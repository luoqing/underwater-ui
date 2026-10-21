using System;
using System.Collections.Generic;
using UnityEngine;

public static class KnifeModelValidation
{
    public static void Run(RescueNetCutter cutter)
    {
        MeshFilter[] parts = cutter.GetComponentsInChildren<MeshFilter>();
        Require(parts.Length == 6, "Expected blade, handle and four flush rivets, with no old guard.");
        HashSet<Mesh> meshes = new();
        HashSet<Material> materials = new();
        int triangles = 0;
        foreach (MeshFilter part in parts)
        {
            meshes.Add(part.sharedMesh);
            materials.Add(part.GetComponent<Renderer>().sharedMaterial);
            triangles += part.sharedMesh.triangles.Length / 3;
        }
        Require(meshes.Count == 3 && materials.Count == 2 && triangles == 520,
            "Knife unexpectedly duplicates meshes/materials or exceeds the 520 triangle geometry budget.");
        foreach (Mesh mesh in meshes) ValidateMesh(mesh);
        Require(cutter.GetComponentsInChildren<Collider>().Length == 0, "Knife must not intercept net raycasts.");

        Camera camera = cutter.GetComponentInParent<Camera>();
        cutter.ResetPose();
        float minY = 1f;
        float maxY = 0f;
        foreach (MeshFilter part in parts)
        {
            foreach (Vector3 vertex in part.sharedMesh.vertices)
            {
                Vector3 point = camera.WorldToViewportPoint(part.transform.TransformPoint(vertex));
                Require(point.z > camera.nearClipPlane && point.x > 0.5f && point.x < 0.95f
                    && point.y > 0.02f && point.y < 0.65f, "Held knife is clipped or no longer tucked at lower right.");
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }
        }
        Require(maxY - minY < 0.36f, "Held knife occupies too much of the view.");
        Vector3 target = camera.transform.TransformPoint(new Vector3(0.15f, 0.1f, 3f));
        Vector3 expected = target + (camera.transform.position - target).normalized * 0.02f;
        foreach (Vector2 delta in new[] { Vector2.zero, Vector2.right * 30f, Vector2.up * 30f, new Vector2(-30f, -10f) })
        {
            cutter.PoseAt(target, delta);
            Vector3 contact = cutter.transform.TransformPoint(new Vector3(0.006f, 0.180f, 0f));
            Require(Vector3.Distance(contact, expected) < 0.0001f, "Knife edge drifts off the cut point when stroke direction changes.");
            Vector3 handle = cutter.transform.TransformPoint(new Vector3(0f, -0.03f, 0f));
            Require(camera.transform.InverseTransformPoint(handle).z < camera.transform.InverseTransformPoint(contact).z - 0.07f,
                "The knife handle is not lifted clear of the net/shell during a stroke.");
        }
        cutter.ResetPose();
    }

    private static void ValidateMesh(Mesh mesh)
    {
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;
        Dictionary<(Vector3, Vector3), int> edges = new();
        float volume = 0f;
        for (int i = 0; i < indices.Length; i += 3)
        {
            Vector3 a = vertices[indices[i]];
            Vector3 b = vertices[indices[i + 1]];
            Vector3 c = vertices[indices[i + 2]];
            Vector3 face = Vector3.Cross(b - a, c - a);
            Require(face.sqrMagnitude > 1e-20f, mesh.name + " has a degenerate triangle.");
            Require(Vector3.Dot(face, (a + b + c) / 3f - mesh.bounds.center) > 0f,
                mesh.name + " has an inward-facing triangle.");
            volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
            Edge(a, b); Edge(b, c); Edge(c, a);
        }
        Require(volume > 0f, mesh.name + " is not an outward solid.");
        foreach (KeyValuePair<(Vector3, Vector3), int> edge in edges)
            Require(edge.Value == 1 && edges.TryGetValue((edge.Key.Item2, edge.Key.Item1), out int reverse) && reverse == 1,
                mesh.name + " has an open or duplicate edge.");

        void Edge(Vector3 a, Vector3 b)
        {
            (Vector3, Vector3) key = (a, b);
            edges.TryGetValue(key, out int count);
            edges[key] = count + 1;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
