using System.Collections.Generic;
using UnityEngine;

public class CoconutDecals : MonoBehaviour
{
    [Header("Sprites (1er golpe, 2do, 3ro...)")]
    public Sprite[] sprites;
    public Renderer decalTarget;

    [Header("Apariencia")]
    public float size = 0.5f;
    public float sizeStep = 0.2f;
    public float surfaceOffset = 0.01f;
    public Color color = Color.white;
    public int sortingOrder = 100;

    [Header("Colocación")]
    [Range(0f, 1f)] public float spread = 0.4f;
    public bool keepSamePlace = true;
    [Range(4, 24)] public int curveResolution = 12;

    const float RayDistanceFactor = 3f;

    static Shader decalShader;
    readonly SurfaceRaycaster surface = new SurfaceRaycaster();
    readonly List<GameObject> spawned = new List<GameObject>();

    Renderer defaultTarget;
    int shown;
    bool placed;
    Vector3 anchorLocalPoint, anchorLocalNormal;
    float anchorSpin;

    public void Init(Renderer fallbackTarget) => defaultTarget = fallbackTarget;

    public void ResetDecals()
    {
        shown = 0;
        placed = false;
        Clear();
    }

    void OnDestroy() => Clear();

    void Clear()
    {
        foreach (var d in spawned)
        {
            if (d == null) continue;
            var mf = d.GetComponent<MeshFilter>();
            var mr = d.GetComponent<MeshRenderer>();
            if (mf != null && mf.sharedMesh != null) Destroy(mf.sharedMesh);
            if (mr != null && mr.sharedMaterial != null) Destroy(mr.sharedMaterial);
            Destroy(d);
        }
        spawned.Clear();
    }

    public void ShowNext()
    {
        if (sprites == null || sprites.Length == 0) return;
        Renderer target = decalTarget != null ? decalTarget : defaultTarget;
        if (target == null) return;

        int hitIndex = shown;
        Sprite sprite = sprites[Mathf.Min(hitIndex, sprites.Length - 1)];
        if (sprite == null) { shown++; return; }

        Clear();
        surface.Prepare(target, this);

        if ((!placed || !keepSamePlace) && !TryPlaceAnchor(target))
        {
            shown++;
            return;
        }

        Vector3 wPoint = target.transform.TransformPoint(anchorLocalPoint);
        Vector3 wNormal = target.transform.TransformDirection(anchorLocalNormal).normalized;
        float worldSize = size + sizeStep * hitIndex;

        Mesh mesh = BuildMesh(target.transform, wPoint, wNormal, anchorSpin, worldSize, sprite);
        spawned.Add(CreateDecalObject(target.transform, mesh, sprite, hitIndex));
        shown++;
    }

    bool TryPlaceAnchor(Renderer target)
    {
        Camera cam = Camera.main;
        Vector3 baseDir = cam != null ? (cam.transform.position - surface.Center).normalized : Vector3.up;
        Vector3 dir = (baseDir + Random.insideUnitSphere * spread).normalized;
        float far = surface.Radius * RayDistanceFactor;

        if (!surface.Raycast(new Ray(surface.Center + dir * far, -dir), out Vector3 point, out Vector3 normal) &&
            !surface.Raycast(new Ray(surface.Center + baseDir * far, -baseDir), out point, out normal))
            return false;

        anchorLocalPoint = target.transform.InverseTransformPoint(point);
        anchorLocalNormal = target.transform.InverseTransformDirection(normal).normalized;
        anchorSpin = Random.Range(0f, 360f);
        placed = true;
        return true;
    }

    GameObject CreateDecalObject(Transform parent, Mesh mesh, Sprite sprite, int index)
    {
        var go = new GameObject("DamageDecal_" + (index + 1));
        go.transform.SetParent(parent, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        if (decalShader == null) decalShader = Shader.Find("Sprites/Default");
        var mat = new Material(decalShader) { mainTexture = sprite.texture, color = color };

        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = mat;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.sortingOrder = sortingOrder;
        return go;
    }

    Mesh BuildMesh(Transform parent, Vector3 wPoint, Vector3 wNormal, float spin, float worldSize, Sprite sprite)
    {
        int n = Mathf.Max(2, curveResolution);

        Vector3 refUp = Mathf.Abs(Vector3.Dot(wNormal, Vector3.up)) > 0.95f ? Vector3.right : Vector3.up;
        Vector3 right = Vector3.Cross(refUp, wNormal).normalized;
        Vector3 up = Vector3.Cross(wNormal, right).normalized;
        Quaternion q = Quaternion.AngleAxis(spin, wNormal);
        right = q * right;
        up = q * up;

        Rect r = sprite.rect;
        Texture2D tex = sprite.texture;
        float aspect = r.height > 0f ? r.width / r.height : 1f;
        float w = aspect >= 1f ? worldSize : worldSize * aspect;
        float h = aspect >= 1f ? worldSize / aspect : worldSize;

        float far = surface.Radius * RayDistanceFactor;
        int side = n + 1;
        var verts = new Vector3[side * side];
        var uvs = new Vector2[side * side];

        for (int j = 0; j <= n; j++)
            for (int i = 0; i <= n; i++)
            {
                float fx = i / (float)n, fy = j / (float)n;
                Vector3 g = wPoint + right * ((fx - 0.5f) * w) + up * ((fy - 0.5f) * h);
                Vector3 origin = g + wNormal * far;
                Vector3 dir = (surface.Center - origin).normalized;

                Vector3 world = g;
                if (surface.Raycast(new Ray(origin, dir), out Vector3 hp, out Vector3 hn))
                    world = hp + hn * surfaceOffset;

                verts[j * side + i] = parent.InverseTransformPoint(world);
                uvs[j * side + i] = new Vector2((r.x + fx * r.width) / tex.width, (r.y + fy * r.height) / tex.height);
            }

        var tris = new int[n * n * 6];
        int t = 0;
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                int a = j * side + i, b = a + 1, c = a + side, d = c + 1;
                tris[t++] = a; tris[t++] = c; tris[t++] = b;
                tris[t++] = b; tris[t++] = c; tris[t++] = d;
            }

        var m = new Mesh { vertices = verts, uv = uvs, triangles = tris };
        m.RecalculateBounds();
        return m;
    }
}