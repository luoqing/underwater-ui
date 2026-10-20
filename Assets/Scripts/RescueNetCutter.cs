using UnityEngine;

// A guarded net cutter: the recessed blade faces the net, never the turtle.
public sealed class RescueNetCutter : MonoBehaviour
{
    private Camera owner;
    private Material grip;
    private Material metal;
    private Material guard;
    private Mesh blade;
    public bool IsEquipped { get; private set; }

    public static RescueNetCutter Create(Camera camera)
    {
        if (camera == null) return null;
        GameObject tool = new("Rescue guarded net cutter");
        tool.transform.SetParent(camera.transform, false);
        RescueNetCutter cutter = tool.AddComponent<RescueNetCutter>();
        cutter.owner = camera;
        cutter.BuildModel();
        cutter.SetEquipped(false);
        return cutter;
    }

    private void BuildModel()
    {
        grip = Material("Dark rubber cutter grip", new Color(0.075f, 0.12f, 0.13f), 0f, 0.24f);
        metal = Material("Brushed steel cutting edge", new Color(0.64f, 0.70f, 0.71f), 0.88f, 0.62f);
        guard = Material("Muted rescue orange guard", new Color(0.76f, 0.35f, 0.12f), 0.12f, 0.30f);
        Part("Rubber handle", PrimitiveType.Capsule, new Vector3(0f, -0.045f, 0f), new Vector3(0.055f, 0.085f, 0.04f), grip);
        for (int i = 0; i < 4; i++)
            Part("Grip ridge", PrimitiveType.Cube, new Vector3(0f, -0.096f + i * 0.032f, -0.021f), new Vector3(0.059f, 0.007f, 0.007f), grip);
        Part("Finger stop", PrimitiveType.Cube, new Vector3(0f, 0.04f, 0f), new Vector3(0.11f, 0.022f, 0.052f), guard);
        Part("Steel spine", PrimitiveType.Cube, new Vector3(-0.038f, 0.108f, 0f), new Vector3(0.02f, 0.13f, 0.025f), metal);
        Part("Rounded safety hook", PrimitiveType.Capsule, new Vector3(0.004f, 0.173f, 0f), new Vector3(0.029f, 0.056f, 0.032f), guard, Quaternion.Euler(0f, 0f, 90f));
        Part("Blunt hook tip", PrimitiveType.Capsule, new Vector3(0.045f, 0.152f, 0f), new Vector3(0.024f, 0.028f, 0.028f), guard);
        blade = new Mesh { name = "Recessed net cutter steel wedge" };
        blade.vertices = new[] { new Vector3(-0.03f, 0.067f, -0.004f), new Vector3(-0.03f, 0.151f, -0.004f),
            new Vector3(0.035f, 0.151f, -0.001f), new Vector3(-0.03f, 0.067f, 0.004f),
            new Vector3(-0.03f, 0.151f, 0.004f), new Vector3(0.035f, 0.151f, 0.001f) };
        blade.triangles = new[] { 0, 1, 2, 3, 5, 4, 0, 3, 4, 0, 4, 1, 1, 4, 5, 1, 5, 2, 2, 5, 3, 2, 3, 0 };
        blade.RecalculateNormals();
        blade.RecalculateBounds();
        GameObject edge = new("Shielded metal knife edge");
        edge.transform.SetParent(transform, false);
        edge.AddComponent<MeshFilter>().sharedMesh = blade;
        edge.AddComponent<MeshRenderer>().sharedMaterial = metal;
    }

    private static Material Material(string name, Color color, float metallic, float smoothness)
    {
        Material material = MissionMaterialFactory.CreateLit(name);
        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        return material;
    }

    private void Part(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(transform, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.transform.localRotation = rotation ?? Quaternion.identity;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = part.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(collider);
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
        transform.rotation = owner.transform.rotation * Quaternion.Euler(0f, 0f, angle);
        Vector3 towardCamera = (owner.transform.position - worldPoint).normalized;
        // Keep the whole guarded edge in front of the rope/shell surface.
        transform.position = worldPoint + towardCamera * 0.02f - transform.up * 0.13f;
    }

    public void ResetPose()
    {
        transform.localPosition = new Vector3(0.27f, -0.19f, 0.68f);
        transform.localRotation = Quaternion.Euler(8f, -16f, -28f);
    }

    private void OnDestroy()
    {
        if (grip != null) Destroy(grip);
        if (metal != null) Destroy(metal);
        if (guard != null) Destroy(guard);
        if (blade != null) Destroy(blade);
    }
}
