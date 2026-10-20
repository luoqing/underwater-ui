using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// The legacy entry point now checks real diamond tube nets, never retired knots.
public static class RescueRopeValidation
{
    public static void Run()
    {
        RescueNetGeometry.Patch[] patches = RescueNetGeometry.Create(4f, 4f, (angle, z) =>
        {
            Vector3 point = new(Mathf.Cos(angle) * 1.15f, Mathf.Sin(angle) * 0.72f, z);
            Vector3 normal = new Vector3(point.x / (1.15f * 1.15f), point.y / (0.72f * 0.72f), 0f).normalized;
            return new RescueNetGeometry.SurfacePoint(point, normal);
        });
        int triangles = 0;
        try
        {
            Require(patches.Length == 3, "The net generator did not produce three releasable patches.");
            foreach (RescueNetGeometry.Patch patch in patches)
            {
                int patchTriangles = ValidateMesh(patch.Mesh);
                Require(patch.Mesh.bounds.size.x > 0.3f && patch.Mesh.bounds.size.y > 0.02f && patch.Mesh.bounds.size.z > 0.3f,
                    "The generated net is a flat projected texture.");
                Require(patch.CutEnds.Length == 2 && patch.CutDirections.Length == 2,
                    "The net has no two physical cut ends.");
                foreach (Mesh end in patch.CutEnds) patchTriangles += ValidateMesh(end);
                Require(patchTriangles == patch.TriangleCount, "A net patch triangle budget omitted its cut ends.");
                triangles += patchTriangles;
            }
            Require(triangles > 1000 && triangles <= 60000, "The complete tube net exceeds its 60k triangle budget.");
            Debug.Log($"UNDERWATER_RESCUE_NET_GEOMETRY_VALIDATED three diamond tube meshes with cut ends; triangles={triangles}; finite unit normals/tangents; outward nondegenerate triangles");
        }
        finally
        {
            foreach (RescueNetGeometry.Patch patch in patches)
            {
                UnityEngine.Object.DestroyImmediate(patch.Mesh);
                foreach (Mesh end in patch.CutEnds) UnityEngine.Object.DestroyImmediate(end);
            }
        }
    }

    public static void ValidateInstance(FishingLineRescue rescue)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        List<MissionInteractable> targets = (List<MissionInteractable>)typeof(FishingLineRescue).GetField("knots", flags).GetValue(rescue);
        Require(targets.Count == 3 && rescue.KnotCount == 3 && rescue.Fragments.Count == 3,
            "The rescue must retain three ordered cut targets and three real net patches.");
        Require(rescue.GetComponentsInChildren<LineRenderer>(true).Length == 0,
            "The fitted net still uses billboard rings or line-renderer strands.");
        Require(rescue.ShellSampleCount > 500 && rescue.ShellSampleHits >= rescue.ShellSampleCount * 0.95f,
            "Net fitting fell back to an unfitted floating cage.");
        Require(rescue.ShellSamples.Count == rescue.ShellSampleCount && rescue.ShellNormals.Count == rescue.ShellSampleCount,
            "Shell fitting diagnostics do not cover all generated samples.");
        Require(rescue.BodyWorldSize > 0f && rescue.ShellLocalSize > 0f, "Net fitting applied a zero body scale.");
        for (int i = 0; i < rescue.ShellSamples.Count; i++)
            Require(float.IsFinite(rescue.ShellSamples[i].sqrMagnitude)
                && Mathf.Abs(rescue.ShellNormals[i].magnitude - 1f) < 0.002f,
                "A shell-fitted net sample or outward normal is invalid.");
        Material material = rescue.SharedNetMaterial;
        Color color = material.GetColor("_BaseColor");
        Require(material.GetFloat("_EmissionStrength") == 0f && material.GetFloat("_LitFilament") == 1f
            && color.a > 0f && color.a < 1f && color.g >= color.r * 0.95f && color.b < color.g * 1.1f,
            "The net is not muted gray-green, translucent and non-emissive nylon.");
        int triangles = 0;
        foreach (RescueNetFragment fragment in rescue.Fragments)
        {
            Require(fragment != null && !fragment.IsReleased && !fragment.IsFinished
                && fragment.transform.IsChildOf(rescue.transform), "An intact net patch is detached or finished.");
            int fragmentTriangles = 0;
            foreach (MeshFilter filter in fragment.GetComponentsInChildren<MeshFilter>())
            {
                fragmentTriangles += ValidateMesh(filter.sharedMesh);
                Require(filter.GetComponent<MeshRenderer>()?.sharedMaterial == material,
                    "A net mesh does not use the shared non-emissive nylon material.");
            }
            Require(fragmentTriangles == fragment.TriangleCount && fragment.Renderers.Count >= 3,
                "A physical net patch or one of its two cut ends is missing.");
            triangles += fragmentTriangles;
        }
        Require(triangles == rescue.NetTriangleCount && triangles <= 60000,
            "The production net budget does not include every patch and both cut ends.");
        foreach (MissionInteractable target in targets)
        {
            Require(target.transform.IsChildOf(rescue.transform), "A cut target does not follow the turtle.");
            SphereCollider collider = target.GetComponent<SphereCollider>();
            Vector3 scale = target.transform.lossyScale;
            Require(collider != null && collider.enabled && collider.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z))
                >= rescue.BodyWorldSize * 0.06f, "The small nylon strands reduced the child's hit target.");
        }
        Bounds body = rescue.GetComponentInChildren<SkinnedMeshRenderer>().bounds;
        Require(rescue.BodyWorldSize <= body.size.magnitude * 1.05f, "Net fitting applied renderer scale twice.");
        body.Expand(rescue.BodyWorldSize * 0.04f);
        foreach (Vector3 sample in rescue.ShellSamples)
            Require(body.Contains(rescue.transform.TransformPoint(sample)), "A fitted nylon sample floats outside the turtle body envelope.");
        foreach (MeshCollider collider in rescue.GetComponentsInChildren<MeshCollider>(true))
            Require(!collider.gameObject.activeInHierarchy || !collider.enabled || !collider.name.Contains("Temporary"),
                "A temporary shell-sampling collider leaked into interaction raycasts.");
        Debug.Log($"UNDERWATER_RESCUE_NET_INSTANCE_VALIDATED shellHits={rescue.ShellSampleHits}/{rescue.ShellSampleCount}; triangles={triangles}; three lit tube patches; gray-green nylon; no billboard rings or active sampling colliders");
    }

    private static int ValidateMesh(Mesh mesh)
    {
        Require(mesh != null, "A nylon tube mesh is missing.");
        Vector3[] vertices = mesh.vertices, normals = mesh.normals;
        Vector4[] tangents = mesh.tangents;
        int[] triangles = mesh.triangles;
        Require(vertices.Length > 12 && normals.Length == vertices.Length && tangents.Length == vertices.Length,
            "A nylon tube has no real lit surface normals/tangents.");
        for (int i = 0; i < vertices.Length; i++)
            Require(float.IsFinite(vertices[i].sqrMagnitude) && Mathf.Abs(normals[i].magnitude - 1f) < 0.002f
                && float.IsFinite(tangents[i].sqrMagnitude) && Mathf.Abs(Vector3.Dot(normals[i], (Vector3)tangents[i])) < 0.002f,
                "A nylon tube has invalid vertices, normals or tangents.");
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            Require(a >= 0 && b >= 0 && c >= 0 && a < vertices.Length && b < vertices.Length && c < vertices.Length,
                "A nylon tube has invalid triangle indices.");
            Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            float areaSquared = face.sqrMagnitude;
            Require(areaSquared > 1e-20f, $"{mesh.name} triangle {i / 3} is degenerate.");
            // Vector3.normalized zeros vectors below 1e-5: valid millimetre
            // filament faces are smaller, so normalize their area explicitly.
            Require(Vector3.Dot(face / Mathf.Sqrt(areaSquared), (normals[a] + normals[b] + normals[c]).normalized) > 0f,
                $"{mesh.name} triangle {i / 3} faces inward.");
        }
        return triangles.Length / 3;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
