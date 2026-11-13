using System.Collections.Generic;
using UnityEngine;

public sealed class SurfaceRaycaster
{
    readonly List<Vector3> localVerts = new List<Vector3>();
    Vector3[] worldVerts;
    int[] tris;

    public Vector3 Center { get; private set; }
    public float Radius { get; private set; }

    bool warnedUnreadable;

    public void Prepare(Renderer target, Object logContext)
    {
        worldVerts = null;
        tris = null;

        Bounds b = target.bounds;
        Center = b.center;
        Radius = Mathf.Max(b.extents.x, Mathf.Max(b.extents.y, b.extents.z));

        Mesh mesh;
        Matrix4x4 l2w;
        bool temporary = false;

        if (target is SkinnedMeshRenderer smr)
        {
            mesh = new Mesh();
            smr.BakeMesh(mesh);
            temporary = true;
            Transform t = smr.transform;
            l2w = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);
        }
        else
        {
            MeshFilter mf = target.GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) return;
            mesh = mf.sharedMesh;
            if (!mesh.isReadable)
            {
                if (!warnedUnreadable)
                {
                    warnedUnreadable = true;
                    Debug.LogWarning("[SurfaceRaycaster] El mesh '" + mesh.name + "' no es legible. Activa Read/Write; mientras tanto se usa una esfera.", logContext);
                }
                return;
            }
            l2w = target.transform.localToWorldMatrix;
        }

        mesh.GetVertices(localVerts);
        tris = mesh.triangles;
        if (worldVerts == null || worldVerts.Length != localVerts.Count)
            worldVerts = new Vector3[localVerts.Count];
        for (int i = 0; i < localVerts.Count; i++)
            worldVerts[i] = l2w.MultiplyPoint3x4(localVerts[i]);

        if (temporary) Object.Destroy(mesh);
    }

    public bool Raycast(Ray ray, out Vector3 point, out Vector3 normal)
    {
        return worldVerts == null ? RaycastSphere(ray, out point, out normal)
                                  : RaycastMesh(ray, out point, out normal);
    }

    bool RaycastSphere(Ray ray, out Vector3 point, out Vector3 normal)
    {
        point = Vector3.zero;
        normal = Vector3.up;
        Vector3 oc = ray.origin - Center;
        float bq = Vector3.Dot(oc, ray.direction);
        float c = Vector3.Dot(oc, oc) - Radius * Radius;
        float disc = bq * bq - c;
        if (disc < 0f) return false;
        float t = -bq - Mathf.Sqrt(disc);
        if (t < 0f) return false;
        point = ray.origin + ray.direction * t;
        normal = (point - Center).normalized;
        return true;
    }

    bool RaycastMesh(Ray ray, out Vector3 point, out Vector3 normal)
    {
        point = Vector3.zero;
        normal = Vector3.up;
        float bestT = float.MaxValue;
        bool hit = false;

        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = worldVerts[tris[i]];
            Vector3 e1 = worldVerts[tris[i + 1]] - v0;
            Vector3 e2 = worldVerts[tris[i + 2]] - v0;

            Vector3 p = Vector3.Cross(ray.direction, e2);
            float det = Vector3.Dot(e1, p);
            if (Mathf.Abs(det) < 1e-8f) continue;

            float inv = 1f / det;
            Vector3 s = ray.origin - v0;
            float u = Vector3.Dot(s, p) * inv;
            if (u < 0f || u > 1f) continue;

            Vector3 q = Vector3.Cross(s, e1);
            float v = Vector3.Dot(ray.direction, q) * inv;
            if (v < 0f || u + v > 1f) continue;

            float t = Vector3.Dot(e2, q) * inv;
            if (t <= 0f || t >= bestT) continue;

            bestT = t;
            hit = true;
            Vector3 n = Vector3.Cross(e1, e2).normalized;
            normal = Vector3.Dot(n, ray.direction) > 0f ? -n : n;
        }

        if (hit) point = ray.origin + ray.direction * bestT;
        return hit;
    }
}