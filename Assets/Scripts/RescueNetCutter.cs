using System.Collections.Generic;
using UnityEngine;

// Simple knife silhouette; the side edge meets the net during a cutting stroke.
public sealed class RescueNetCutter : MonoBehaviour
{
    private static readonly Vector3 CuttingContact = new(0.006f, 0.180f, 0f);
    private Camera owner;
    private Material grip;
    private Material metal;
    private Mesh blade;
    private Mesh handle;
    private Mesh rivet;
    public bool IsEquipped { get; private set; }

    public static RescueNetCutter Create(Camera camera)
    {
        if (camera == null) return null;
        GameObject tool = new("Rescue net knife");
        tool.transform.SetParent(camera.transform, false);
        RescueNetCutter cutter = tool.AddComponent<RescueNetCutter>();
        cutter.owner = camera;
        cutter.BuildModel();
        cutter.SetEquipped(false);
        return cutter;
    }

    private void BuildModel()
    {
        grip = Material("Matte charcoal knife handle", new Color(0.045f, 0.052f, 0.055f), 0f, 0.20f);
        metal = Material("Satin stainless knife steel", new Color(0.67f, 0.70f, 0.72f), 0.70f, 0.46f);
        blade = BeveledPrism("Thin beveled knife blade", new[]
        {
            new Vector2(-0.016f, 0.035f), new Vector2(0.026f, 0.035f),
            new Vector2(0.028f, 0.090f), new Vector2(0.022f, 0.139f),
            new Vector2(0.006f, 0.180f), new Vector2(-0.013f, 0.212f),
            new Vector2(-0.016f, 0.200f)
        }, 0.003f, 0.0004f, new Vector2(0.78f, 0.97f));
        handle = BeveledPrism("Rounded knife handle", new[]
        {
            new Vector2(-0.012f, -0.092f), new Vector2(0.004f, -0.096f),
            new Vector2(0.015f, -0.087f), new Vector2(0.018f, -0.067f),
            new Vector2(0.018f, 0.020f), new Vector2(0.012f, 0.035f),
            new Vector2(-0.013f, 0.035f), new Vector2(-0.020f, 0.026f),
            new Vector2(-0.022f, -0.063f), new Vector2(-0.020f, -0.080f)
        }, 0.012f, 0.005f, new Vector2(0.77f, 0.94f));
        MeshPart("Silver blade", blade, metal);
        MeshPart("Dark rounded handle", handle, grip);
        Vector2[] rivetOutline = new Vector2[12];
        for (int i = 0; i < rivetOutline.Length; i++)
        {
            float angle = i * Mathf.PI * 2f / rivetOutline.Length;
            rivetOutline[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 0.0035f;
        }
        rivet = BeveledPrism("Flush handle rivet", rivetOutline, 0.00075f, 0.00025f, Vector2.one * 0.80f);
        for (int i = 0; i < 2; i++)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                GameObject pin = MeshPart("Flush steel handle rivet", rivet, metal);
                pin.transform.localPosition = new Vector3(-0.002f, -0.062f + i * 0.067f, side * 0.0117f);
            }
        }
    }

    // Convex outline, counterclockwise: thin perimeter plus inset face rings.
    // Separate triangle vertices keep the blade faces and bevel normals crisp.
    private static Mesh BeveledPrism(string name, Vector2[] outline, float faceDepth, float rimDepth, Vector2 inset)
    {
        List<Vector3> vertices = new();
        List<int> triangles = new();
        Vector2 center = Vector2.zero;
        foreach (Vector2 point in outline) center += point;
        center /= outline.Length;
        for (int side = -1; side <= 1; side += 2)
        {
            for (int i = 0; i < outline.Length; i++)
            {
                Vector2 a = outline[i];
                Vector2 b = outline[(i + 1) % outline.Length];
                Vector2 innerA = center + Vector2.Scale(a - center, inset);
                Vector2 innerB = center + Vector2.Scale(b - center, inset);
                Vector3 outerA = new(a.x, a.y, side * rimDepth);
                Vector3 outerB = new(b.x, b.y, side * rimDepth);
                Vector3 faceA = new(innerA.x, innerA.y, side * faceDepth);
                Vector3 faceB = new(innerB.x, innerB.y, side * faceDepth);
                AddTriangle(new Vector3(center.x, center.y, side * faceDepth), faceA, faceB, side < 0);
                AddTriangle(outerA, outerB, faceB, side < 0);
                AddTriangle(outerA, faceB, faceA, side < 0);
                if (side == 1)
                {
                    Vector3 backA = new(a.x, a.y, -rimDepth);
                    Vector3 backB = new(b.x, b.y, -rimDepth);
                    AddTriangle(backA, backB, outerB, false);
                    AddTriangle(backA, outerB, outerA, false);
                }
            }
        }
        Mesh mesh = new() { name = name };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;

        void AddTriangle(Vector3 a, Vector3 b, Vector3 c, bool reverse)
        {
            int start = vertices.Count;
            vertices.Add(a);
            vertices.Add(reverse ? c : b);
            vertices.Add(reverse ? b : c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }
    }

    private static Material Material(string name, Color color, float metallic, float smoothness)
    {
        Material material = MissionMaterialFactory.CreateLit(name);
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private GameObject MeshPart(string name, Mesh mesh, Material material)
    {
        GameObject part = new(name);
        part.transform.SetParent(transform, false);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
        return part;
    }

    public void SetEquipped(bool equipped)
    {
        IsEquipped = equipped;
        gameObject.SetActive(equipped);
        if (equipped) ResetPose();
    }

    public void PoseAt(Vector3 worldPoint, Vector2 screenDelta)
    {
        if (!IsEquipped || owner == null) return;
        float angle = screenDelta.sqrMagnitude > 0.01f ? Mathf.Atan2(screenDelta.y, screenDelta.x) * Mathf.Rad2Deg - 90f : 0f;
        // Keep the handle lifted toward the viewer while the curved edge touches
        // the strand, rather than laying the whole knife inside the curved shell.
        transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward) * Quaternion.Euler(28f, -12f, 0f);
        Vector3 towardCamera = (owner.transform.position - worldPoint).normalized;
        transform.position = worldPoint + towardCamera * 0.02f - transform.TransformVector(CuttingContact);
    }

    public void ResetPose()
    {
        transform.localScale = Vector3.one * 0.86f;
        transform.localPosition = new Vector3(0.29f, -0.22f, 0.76f);
        transform.localRotation = Quaternion.Euler(5f, -12f, -32f);
    }

    private void OnDestroy()
    {
        if (grip != null) Destroy(grip);
        if (metal != null) Destroy(metal);
        if (blade != null) Destroy(blade);
        if (handle != null) Destroy(handle);
        if (rivet != null) Destroy(rivet);
    }
}
