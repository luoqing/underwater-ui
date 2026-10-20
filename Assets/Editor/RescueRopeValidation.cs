using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class RescueRopeValidation
{
    public static void Run()
    {
        Mesh mesh = RescueRopeGeometry.CreateKnot();
        try
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector4[] tangents = mesh.tangents;
            int[] triangles = mesh.triangles;
            Require(triangles.Length / 3 * 3 < 25000, "Three rescue knots exceed their 25k triangle budget");
            Require(mesh.bounds.size.z > 0.4f && mesh.bounds.size.x > 1f, "Rope knot is a flat silhouette");
            Require(Mathf.Abs(RescueRopeGeometry.FilamentDiameter * 0.045f - 0.0016f) < 0.000001f,
                "Knot filament is thicker than the shell fishing line");
            Require(normals.Length == vertices.Length && tangents.Length == vertices.Length,
                "Rope knot is missing lit surface normals or tangents");
            for (int i = 0; i < vertices.Length; i++)
            {
                Require(float.IsFinite(vertices[i].sqrMagnitude), "Rope knot has invalid vertices");
                Require(Mathf.Abs(normals[i].magnitude - 1f) < 0.001f, "Rope knot has invalid normals");
                Require(Mathf.Abs(Vector3.Dot(normals[i], (Vector3)tangents[i])) < 0.001f,
                    "Rope knot has non-orthogonal tangents");
            }
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
                Require(a >= 0 && b >= 0 && c >= 0 && a < vertices.Length && b < vertices.Length && c < vertices.Length,
                    "Rope knot has invalid triangle indices");
                Vector3 face = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                Require(face.sqrMagnitude > 0.00000000001f, "Rope knot contains a degenerate triangle");
                Require(Vector3.Dot(face.normalized, (normals[a] + normals[b] + normals[c]).normalized) > 0f,
                    "Rope knot has inward-facing tube triangles");
            }
            Debug.Log($"UNDERWATER_RESCUE_ROPE_GEOMETRY_VALIDATED vertices={vertices.Length}; threeKnotTriangles={triangles.Length}; round capped tubes; finite unit normals; orthogonal tangents; outward winding");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(mesh);
        }

        FishingLineRescue rescue = UnityEngine.Object.FindFirstObjectByType<FishingLineRescue>();
        if (rescue != null) ValidateInstance(rescue);
    }

    public static void ValidateInstance(FishingLineRescue rescue)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        List<MissionInteractable> knots = (List<MissionInteractable>)typeof(FishingLineRescue).GetField("knots", flags).GetValue(rescue);
        List<LineRenderer> loops = (List<LineRenderer>)typeof(FishingLineRescue).GetField("loops", flags).GetValue(rescue);
        Require(knots.Count == 3 && loops.Count == 3, "Rope repair changed the three ordered rescue targets");
        int hits = (int)typeof(FishingLineRescue).GetField("shellSampleHits", flags).GetValue(rescue);
        int samples = (int)typeof(FishingLineRescue).GetField("shellSampleCount", flags).GetValue(rescue);
        Require(samples == 288 && hits >= samples * 0.95f, "Fishing line fitting silently fell back to a floating ellipse");
        foreach (MissionInteractable knot in knots)
        {
            Require(knot.GetComponent<MeshFilter>()?.sharedMesh != null && knot.GetComponent<MeshRenderer>() != null,
                "A rescue knot is not rendered as a real lit mesh");
            Require(knot.GetComponentsInChildren<LineRenderer>().Length == 0, "A rescue knot still uses billboard strands");
            Require(knot.transform.parent == rescue.transform, "Rope knot will not follow turtle motion");
            SphereCollider target = knot.GetComponent<SphereCollider>();
            Vector3 scale = knot.transform.lossyScale;
            float clickRadius = target.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            float worldSize = loops[0].startWidth / 0.0016f;
            Require(clickRadius >= worldSize * 0.075f * 1.15f - 0.0001f,
                "The more realistic small rope has reduced the child's clickable target");
            Material material = knot.GetComponent<MeshRenderer>().sharedMaterial;
            Require(material == loops[0].sharedMaterial && material.GetFloat("_EmissionStrength") == 0f
                && material.GetFloat("_LitFilament") == 1f && material.GetColor("_BaseColor").a > 0f
                && material.GetColor("_BaseColor").a < 1f,
                "The knot and fishing line do not share one non-emissive translucent nylon material");
        }
        foreach (LineRenderer loop in loops)
        {
            Require(!loop.useWorldSpace && loop.transform.parent == rescue.transform,
                "Fitted fishing line does not move with the shell");
            Require(loop.sharedMaterial.GetFloat("_EmissionStrength") == 0f, "Fishing line still glows");
            Require(loop.sharedMaterial.GetFloat("_LitFilament") == 1f, "Fishing line does not use non-emissive filament lighting");
            float bodyWorldSize = loop.startWidth / 0.0016f;
            float perimeter = 0f;
            float longestSegment = 0f;
            for (int point = 0; point < loop.positionCount; point++)
            {
                float segment = loop.transform.TransformVector(loop.GetPosition((point + 1) % loop.positionCount) - loop.GetPosition(point)).magnitude;
                perimeter += segment;
                longestSegment = Mathf.Max(longestSegment, segment);
            }
            Require(longestSegment < bodyWorldSize * 0.06f && perimeter < bodyWorldSize * 1.8f,
                "A fishing-line ring snags a flipper and produces long floating spikes");
        }
        Bounds body = rescue.GetComponentInChildren<SkinnedMeshRenderer>().bounds;
        Require(loops[0].startWidth / 0.0016f <= body.size.magnitude * 1.05f,
            "Baking the shell applied renderer scale twice");
        foreach (MeshCollider collider in rescue.GetComponentsInChildren<MeshCollider>())
            Require(!collider.name.Contains("Temporary shell"), "Temporary rope fitting collider leaked into gameplay raycasts");
        Debug.Log($"UNDERWATER_RESCUE_ROPE_INSTANCE_VALIDATED shellHits={hits}/{samples}; three mesh knots; no knot LineRenderers; non-emissive materials; local fitted paths; no active sampling colliders");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
