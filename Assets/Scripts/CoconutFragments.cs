using System.Collections.Generic;
using UnityEngine;

public class CoconutFragments : MonoBehaviour
{
    [Tooltip("Prefabs de pedazos. Se ignoran si el coco ya tiene hijos con FragmentFader.")]
    public GameObject[] fragmentPrefabs;

    [Header("Rotura")]
    public float pushForce = 0.9f;
    public float spreadForce = 0.35f;
    public float upForce = 0.15f;
    public float torque = 1.2f;
    public float gravity = 12f;
    [Range(0f, 1f)] public float randomness = 0.2f;

    [Header("Vida de los pedazos")]
    public float lifetime = 3f;
    public float fadeDuration = 1.2f;
    [Range(0f, 1f)] public float shrinkTo = 0.6f;

    const float PieceLinearDrag = 0.5f;
    const float PieceAngularDrag = 1.5f;
    const float TorqueNoise = 0.25f;
    const float MinSideOffset = 0.01f;

    FragmentFader[] childPieces = new FragmentFader[0];

    public FragmentFader[] ChildPieces => childPieces;

    public void Init()
    {
        childPieces = GetComponentsInChildren<FragmentFader>(true);
        foreach (var p in childPieces)
        {
            foreach (var r in p.GetComponentsInChildren<Renderer>(true))
                if (!(r is ParticleSystemRenderer)) r.enabled = false;
            foreach (var c in p.GetComponentsInChildren<Collider>(true))
                c.enabled = false;
            var rb = p.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
    }
    public bool Break(Vector3 hitFrom, float sizeRatio)
    {
        bool hasChildren = childPieces.Length > 0;
        bool hasPrefabs = fragmentPrefabs != null && fragmentPrefabs.Length > 0;
        if (!hasChildren && !hasPrefabs) return false;

        GetSlashBasis(hitFrom, out Vector3 slashDir, out Vector3 side);
        List<GameObject> pieces = hasChildren ? DetachChildren() : InstantiatePrefabs(sizeRatio);

        IgnoreMutualCollisions(pieces);
        foreach (var piece in pieces)
            LaunchPiece(piece, slashDir, side);
        return true;
    }

    List<GameObject> DetachChildren()
    {
        var list = new List<GameObject>(childPieces.Length);
        foreach (var piece in childPieces)
        {
            GameObject frag = piece.gameObject;
            frag.transform.SetParent(null, true);
            frag.SetActive(true);
            EnableRenderers(frag);

            var cols = frag.GetComponentsInChildren<Collider>(true);
            if (cols.Length == 0) frag.AddComponent<BoxCollider>();
            else foreach (var c in cols) c.enabled = true;

            list.Add(frag);
        }
        return list;
    }

    List<GameObject> InstantiatePrefabs(float sizeRatio)
    {
        var list = new List<GameObject>(fragmentPrefabs.Length);
        foreach (var prefab in fragmentPrefabs)
        {
            if (prefab == null) continue;
            Vector3 pos = transform.TransformPoint(prefab.transform.localPosition);
            Quaternion rot = transform.rotation * prefab.transform.localRotation;

            GameObject frag = Instantiate(prefab, pos, rot);
            frag.SetActive(true);
            EnableRenderers(frag);
            frag.transform.localScale *= sizeRatio;
            list.Add(frag);
        }
        return list;
    }

    static void EnableRenderers(GameObject go)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            if (!(r is ParticleSystemRenderer)) r.enabled = true;
    }

    static void IgnoreMutualCollisions(List<GameObject> frags)
    {
        var cols = new List<Collider>();
        foreach (var f in frags) cols.AddRange(f.GetComponentsInChildren<Collider>());
        for (int i = 0; i < cols.Count; i++)
            for (int j = i + 1; j < cols.Count; j++)
                Physics.IgnoreCollision(cols[i], cols[j]);
    }

    void GetSlashBasis(Vector3 hitFrom, out Vector3 slashDir, out Vector3 side)
    {
        slashDir = transform.position - hitFrom;
        slashDir.y = 0f;
        if (slashDir.sqrMagnitude < 0.0001f)
        {
            Vector2 r = Random.insideUnitCircle.normalized;
            slashDir = new Vector3(r.x, 0f, r.y);
        }
        slashDir.Normalize();
        side = Vector3.Cross(Vector3.up, slashDir);
    }

    void LaunchPiece(GameObject frag, Vector3 slashDir, Vector3 side)
    {
        Rigidbody rb = frag.GetComponent<Rigidbody>();
        if (rb == null) rb = frag.AddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.linearDamping = PieceLinearDrag;
        rb.angularDamping = PieceAngularDrag;
        rb.maxDepenetrationVelocity = 1f;

        var cf = frag.GetComponent<ConstantForce>();
        if (cf == null) cf = frag.AddComponent<ConstantForce>();
        cf.force = Vector3.down * rb.mass * gravity;

        Vector3 off = frag.transform.position - transform.position;
        off.y = 0f;
        float sd = Vector3.Dot(off, side);
        float sign = Mathf.Abs(sd) > MinSideOffset ? Mathf.Sign(sd) : (Random.value < 0.5f ? -1f : 1f);

        float k = 1f + Random.Range(-randomness, randomness);
        Vector3 v = slashDir * pushForce + side * sign * spreadForce + Vector3.up * upForce;
        rb.AddForce(v * k, ForceMode.VelocityChange);
        rb.AddTorque(side * (torque * k) + Random.insideUnitSphere * (torque * TorqueNoise), ForceMode.VelocityChange);

        FragmentFader.Attach(frag, lifetime, fadeDuration, shrinkTo);
    }
}