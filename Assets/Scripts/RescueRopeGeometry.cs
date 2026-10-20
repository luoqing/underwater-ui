using System.Collections.Generic;
using UnityEngine;

public static class RescueRopeGeometry
{
    public const float FilamentDiameter = 0.03555556f;

    // An open overhand knot made from the same single nylon filament as the
    // shell wraps. Crossings move in depth and both loose ends remain visible.
    public static Mesh CreateKnot()
    {
        const int knotSamples = 120;
        const int tailSamples = 12;
        Vector3[] path = new Vector3[knotSamples + tailSamples * 2];
        for (int i = 0; i < knotSamples; i++)
        {
            float t = Mathf.Lerp(0.55f, Mathf.PI * 2f - 0.55f, i / (knotSamples - 1f));
            path[i + tailSamples] = new Vector3(Mathf.Sin(t) + 2f * Mathf.Sin(2f * t),
                Mathf.Cos(t) - 2f * Mathf.Cos(2f * t), -Mathf.Sin(3f * t)) * 0.25f;
        }
        Vector3 first = path[tailSamples];
        Vector3 last = path[path.Length - tailSamples - 1];
        Vector3 startTangent = (path[tailSamples + 1] - first).normalized;
        Vector3 endTangent = (last - path[path.Length - tailSamples - 2]).normalized;
        for (int i = 0; i < tailSamples; i++)
        {
            float distance = (tailSamples - i) / (float)tailSamples;
            path[i] = first - startTangent * distance * 0.55f + Vector3.back * distance * distance * 0.08f;
            distance = (i + 1) / (float)tailSamples;
            path[path.Length - tailSamples + i] = last + endTangent * distance * 0.55f + Vector3.back * distance * distance * 0.08f;
        }

        List<Vector3> vertices = new();
        List<Vector3> normals = new();
        List<Vector4> tangents = new();
        List<Vector2> uvs = new();
        List<int> triangles = new();
        AddTube(path, FilamentDiameter * 0.5f, 8, vertices, normals, tangents, uvs, triangles);
        Mesh mesh = new() { name = "Nylon fishing-line overhand knot" };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetTangents(tangents);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Vector3 Tangent(Vector3[] path, int index)
    {
        return (path[Mathf.Min(index + 1, path.Length - 1)] - path[Mathf.Max(index - 1, 0)]).normalized;
    }

    private static Vector3[] BuildFrames(Vector3[] path)
    {
        Vector3[] normals = new Vector3[path.Length];
        Vector3 tangent = Tangent(path, 0);
        Vector3 previous = Vector3.Cross(tangent, Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
        for (int i = 0; i < path.Length; i++)
        {
            tangent = Tangent(path, i);
            previous = (previous - tangent * Vector3.Dot(previous, tangent)).normalized;
            normals[i] = previous;
        }
        return normals;
    }

    private static void AddTube(Vector3[] path, float radius, int sides, List<Vector3> vertices,
        List<Vector3> normals, List<Vector4> tangents, List<Vector2> uvs, List<int> triangles)
    {
        int firstVertex = vertices.Count;
        Vector3[] frameNormals = BuildFrames(path);
        float travelled = 0f;
        for (int i = 0; i < path.Length; i++)
        {
            if (i > 0) travelled += Vector3.Distance(path[i - 1], path[i]);
            Vector3 tangent = Tangent(path, i);
            Vector3 binormal = Vector3.Cross(tangent, frameNormals[i]);
            for (int side = 0; side <= sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                Vector3 normal = frameNormals[i] * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                vertices.Add(path[i] + normal * radius);
                normals.Add(normal);
                tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, 1f));
                uvs.Add(new Vector2(side / (float)sides, travelled));
                if (i == 0 || side == sides) continue;
                int current = firstVertex + i * (sides + 1) + side;
                int previous = current - sides - 1;
                triangles.Add(previous); triangles.Add(current + 1); triangles.Add(current);
                triangles.Add(previous); triangles.Add(previous + 1); triangles.Add(current + 1);
            }
        }
        for (int end = 0; end < 2; end++)
        {
            int index = end == 0 ? 0 : path.Length - 1;
            Vector3 normal = Tangent(path, index) * (end == 0 ? -1f : 1f);
            Vector3 tangent = frameNormals[index];
            Vector3 binormal = Vector3.Cross(Tangent(path, index), tangent);
            int cap = vertices.Count;
            vertices.Add(path[index]); normals.Add(normal);
            tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, 1f)); uvs.Add(Vector2.one * 0.5f);
            for (int side = 0; side <= sides; side++)
            {
                float angle = side * Mathf.PI * 2f / sides;
                vertices.Add(path[index] + (tangent * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)) * radius);
                normals.Add(normal); tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, 1f));
                uvs.Add(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.5f + Vector2.one * 0.5f);
                if (side == sides) continue;
                triangles.Add(cap);
                triangles.Add(cap + side + (end == 0 ? 2 : 1));
                triangles.Add(cap + side + (end == 0 ? 1 : 2));
            }
        }
    }
}
