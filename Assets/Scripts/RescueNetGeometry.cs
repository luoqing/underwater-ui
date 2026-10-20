using System;
using System.Collections.Generic;
using UnityEngine;

public static class RescueNetGeometry
{
    public readonly struct SurfacePoint
    {
        public readonly Vector3 Position;
        public readonly Vector3 Normal;
        public SurfacePoint(Vector3 position, Vector3 normal) { Position = position; Normal = normal; }
    }

    public sealed class Patch
    {
        public Mesh Mesh;
        public Mesh[] CutEnds;
        public Vector3 CutPosition;
        public Vector3 CutNormal;
        public Vector3[] CutDirections;
        public int TriangleCount;
    }

    private readonly struct Node
    {
        public readonly float Angle;
        public readonly float Z;
        public readonly SurfacePoint Surface;
        public Node(float angle, float z, SurfacePoint surface) { Angle = angle; Z = z; Surface = surface; }
    }

    // A staggered lattice has four diagonal neighbours at every interior node:
    // its open cells are diamonds, not opaque quads or a projected grid texture.
    public static Patch[] Create(float size, float bodyLength, Func<float, float, SurfacePoint> sample)
    {
        const int rows = 13;
        const int columns = 12;
        const int segments = 6;
        float radius = size * 0.00065f;
        Node[,] nodes = new Node[rows, columns];
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            float irregular = Mathf.Sin(row * 2.71f + column * 4.13f);
            float angle = Mathf.Lerp(0.12f, Mathf.PI - 0.12f,
                (column + ((row & 1) == 1 ? 0.5f : 0f)) / (columns - 0.5f)) + irregular * 0.012f;
            float z = Mathf.Lerp(-0.24f, 0.17f, row / (rows - 1f)) * bodyLength + irregular * size * 0.002f;
            nodes[row, column] = new Node(angle, z, sample(angle, z));
        }

        Builder[] builders = { new(), new(), new() };
        Patch[] patches = { new(), new(), new() };
        for (int row = 0; row < rows - 1; row++)
        for (int column = 0; column < columns; column++)
        for (int direction = 0; direction < 2; direction++)
        {
            int nextColumn = column + (direction == 0 ? 0 : (row & 1) == 0 ? -1 : 1);
            if (nextColumn < 0 || nextColumn >= columns) continue;
            int patch = column / 4;
            Node a = nodes[row, column], b = nodes[row + 1, nextColumn];
            Vector3[] path = new Vector3[segments + 1];
            for (int step = 0; step <= segments; step++)
            {
                if (step == 0) path[step] = a.Surface.Position;
                else if (step == segments) path[step] = b.Surface.Position;
                else path[step] = sample(Mathf.Lerp(a.Angle, b.Angle, step / (float)segments),
                    Mathf.Lerp(a.Z, b.Z, step / (float)segments)).Position;
            }
            float thickness = radius * (0.94f + Mathf.Abs(Mathf.Sin(row * 3.1f + column)) * 0.12f);
            if (row == 2 + patch * 3 && column == 1 + patch * 4 && direction == 0)
            {
                SurfacePoint cut = sample((a.Angle + b.Angle) * 0.5f, (a.Z + b.Z) * 0.5f);
                patches[patch].CutPosition = cut.Position;
                patches[patch].CutNormal = cut.Normal;
                patches[patch].CutDirections = new[] { (path[0] - cut.Position).normalized, (path[segments] - cut.Position).normalized };
                patches[patch].CutEnds = new Mesh[2];
                for (int end = 0; end < 2; end++)
                {
                    Builder tail = new();
                    Vector3[] half = new Vector3[segments / 2 + 1];
                    for (int step = 0; step < half.Length; step++) half[step] = path[step + end * segments / 2] - cut.Position;
                    tail.AddTube(half, thickness, 6);
                    patches[patch].CutEnds[end] = tail.Build($"Nylon net {patch + 1} cut end {end + 1}");
                }
            }
            else builders[patch].AddTube(path, thickness, 6);
        }

        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            int patch = column / 4;
            SurfacePoint surface = nodes[row, column].Surface;
            Vector3 normal = surface.Normal;
            Vector3 tangent = Vector3.Cross(normal, Vector3.forward).normalized;
            if (tangent.sqrMagnitude < 0.5f) tangent = Vector3.right;
            Vector3 across = Vector3.Cross(normal, tangent);
            Vector3[] binding = new Vector3[11];
            for (int step = 0; step < binding.Length; step++)
            {
                float angle = step * Mathf.PI * 2f / (binding.Length - 1);
                binding[step] = surface.Position + (tangent * Mathf.Cos(angle) + across * Mathf.Sin(angle)) * radius * 2.1f;
            }
            builders[patch].AddTube(binding, radius * 0.8f, 5);
            if ((row == 0 || row == rows - 1 || column == 0 || column == columns - 1) && (row + column) % 3 == 0)
            {
                // Broken edge strands have short loose tips, not a perfect
                // rectangular border or large loops floating above the animal.
                Vector3 direction = tangent * ((column & 1) == 0 ? 1f : -1f);
                builders[patch].AddTube(new[] { surface.Position,
                    surface.Position + direction * size * 0.008f + normal * size * 0.002f,
                    surface.Position + direction * size * 0.016f + normal * size * 0.006f }, radius * 0.75f, 5);
            }
        }
        for (int patch = 0; patch < patches.Length; patch++)
        {
            patches[patch].Mesh = builders[patch].Build($"Fitted diamond nylon net patch {patch + 1}");
            patches[patch].TriangleCount = patches[patch].Mesh.triangles.Length / 3;
            foreach (Mesh end in patches[patch].CutEnds) patches[patch].TriangleCount += end.triangles.Length / 3;
        }
        return patches;
    }

    private sealed class Builder
    {
        private readonly List<Vector3> vertices = new();
        private readonly List<Vector3> normals = new();
        private readonly List<Vector4> tangents = new();
        private readonly List<Vector2> uvs = new();
        private readonly List<int> triangles = new();

        public void AddTube(Vector3[] path, float radius, int sides)
        {
            int first = vertices.Count;
            Vector3 previousNormal = Vector3.zero;
            Vector3 firstNormal = Vector3.zero;
            float travelled = 0f;
            for (int point = 0; point < path.Length; point++)
            {
                Vector3 tangent = (path[Mathf.Min(point + 1, path.Length - 1)] - path[Mathf.Max(point - 1, 0)]).normalized;
                if (point == 0) previousNormal = Vector3.Cross(tangent, Mathf.Abs(tangent.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                else travelled += Vector3.Distance(path[point - 1], path[point]);
                previousNormal = (previousNormal - tangent * Vector3.Dot(previousNormal, tangent)).normalized;
                if (point == 0) firstNormal = previousNormal;
                Vector3 binormal = Vector3.Cross(tangent, previousNormal);
                for (int side = 0; side <= sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    Vector3 normal = previousNormal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle);
                    vertices.Add(path[point] + normal * radius);
                    normals.Add(normal);
                    tangents.Add(new Vector4(tangent.x, tangent.y, tangent.z, 1f));
                    uvs.Add(new Vector2(side / (float)sides, travelled));
                    if (point == 0 || side == sides) continue;
                    int current = first + point * (sides + 1) + side;
                    int previous = current - sides - 1;
                    triangles.Add(previous); triangles.Add(current + 1); triangles.Add(current);
                    triangles.Add(previous); triangles.Add(previous + 1); triangles.Add(current + 1);
                }
            }
            // Separate cap vertices preserve outward normals on both cut ends.
            for (int end = 0; end < 2; end++)
            {
                int point = end == 0 ? 0 : path.Length - 1;
                Vector3 tangent = (path[Mathf.Min(point + 1, path.Length - 1)] - path[Mathf.Max(point - 1, 0)]).normalized;
                Vector3 normal = tangent * (end == 0 ? -1f : 1f);
                Vector3 across = end == 0 ? firstNormal : previousNormal;
                Vector3 up = Vector3.Cross(tangent, across);
                int cap = vertices.Count;
                vertices.Add(path[point]); normals.Add(normal);
                tangents.Add(new Vector4(across.x, across.y, across.z, 1f)); uvs.Add(Vector2.one * 0.5f);
                for (int side = 0; side <= sides; side++)
                {
                    float angle = side * Mathf.PI * 2f / sides;
                    vertices.Add(path[point] + (across * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius);
                    normals.Add(normal); tangents.Add(new Vector4(across.x, across.y, across.z, 1f));
                    uvs.Add(Vector2.one * 0.5f);
                    if (side == sides) continue;
                    triangles.Add(cap);
                    triangles.Add(cap + side + (end == 0 ? 2 : 1));
                    triangles.Add(cap + side + (end == 0 ? 1 : 2));
                }
            }
        }

        public Mesh Build(string name)
        {
            Mesh mesh = new() { name = name };
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTangents(tangents);
            mesh.SetUVs(0, uvs); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
