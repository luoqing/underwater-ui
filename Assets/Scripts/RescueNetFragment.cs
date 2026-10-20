using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class RescueNetFragment : MonoBehaviour
{
    public const float ReleaseDuration = 0.85f;
    private readonly List<Renderer> renderers = new();
    private readonly List<Mesh> ownedMeshes = new();
    private readonly Transform[] cutEnds = new Transform[2];
    private Vector3[] cutDirections;
    private Vector3 cutPosition;
    private Vector3 cutNormal;
    private Vector3 worldOrigin;
    private Vector3 outward;
    private Vector3 releaseAxis;
    private Quaternion rotation;
    private Vector3 scale;
    private Color baseColor;
    private float bodyWorldSize;
    private float bodyLocalSize;
    private MaterialPropertyBlock propertyBlock;
    private Action finished;

    public bool IsReleased { get; private set; }
    public bool IsFinished { get; private set; }
    public float ReleaseProgress { get; private set; }
    public int TriangleCount { get; private set; }
    public IReadOnlyList<Renderer> Renderers => renderers;

    public void Initialize(RescueNetGeometry.Patch patch, Material material, float worldSize, float localSize)
    {
        bodyWorldSize = worldSize;
        bodyLocalSize = localSize;
        baseColor = material.GetColor("_BaseColor");
        cutPosition = patch.CutPosition;
        cutNormal = patch.CutNormal;
        cutDirections = patch.CutDirections;
        TriangleCount = patch.TriangleCount;
        AddMesh(gameObject, patch.Mesh, material);
        for (int end = 0; end < 2; end++)
        {
            GameObject tail = new($"Nylon cut end {end + 1}");
            tail.transform.SetParent(transform, false);
            tail.transform.localPosition = cutPosition;
            cutEnds[end] = tail.transform;
            AddMesh(tail, patch.CutEnds[end], material);
        }
        propertyBlock = new MaterialPropertyBlock();
    }

    private void AddMesh(GameObject target, Mesh mesh, Material material)
    {
        target.AddComponent<MeshFilter>().sharedMesh = mesh;
        Renderer renderer = target.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderers.Add(renderer);
        ownedMeshes.Add(mesh);
    }

    public void Release(Action onFinished)
    {
        if (IsReleased) return;
        IsReleased = true;
        finished = onFinished;
        outward = transform.TransformDirection(cutNormal).normalized;
        releaseAxis = Vector3.Cross(outward, Vector3.up);
        releaseAxis = releaseAxis.sqrMagnitude > 0.001f ? releaseAxis.normalized : transform.right;
        transform.SetParent(null, true);
        worldOrigin = transform.position;
        rotation = transform.rotation;
        scale = transform.localScale;
    }

    private void Update()
    {
        if (IsReleased) AdvanceRelease(Time.deltaTime);
    }

    public void AdvanceRelease(float deltaTime)
    {
        if (!IsReleased || IsFinished) return;
        ReleaseProgress = Mathf.Clamp01(ReleaseProgress + Mathf.Max(0f, deltaTime) / ReleaseDuration);
        float t = Mathf.SmoothStep(0f, 1f, ReleaseProgress);
        float open = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(ReleaseProgress / 0.3f));
        for (int end = 0; end < 2; end++)
        {
            cutEnds[end].localPosition = cutPosition + cutDirections[end] * bodyLocalSize * 0.014f * open
                + cutNormal * bodyLocalSize * 0.003f * open;
            cutEnds[end].localRotation = Quaternion.AngleAxis((end == 0 ? -1f : 1f) * 24f * open, cutNormal);
        }
        // First lift free of the shell; the weaker downward drift must not
        // pull a dorsal panel back through the turtle while it is still visible.
        transform.position = worldOrigin + outward * bodyWorldSize * 0.12f * t
            + Vector3.down * bodyWorldSize * 0.035f * t * t;
        transform.rotation = Quaternion.AngleAxis(7f * t, releaseAxis) * rotation;
        transform.localScale = scale * (1f + 0.035f * t);
        float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1f, ReleaseProgress));
        Color color = baseColor;
        color.a *= fade;
        propertyBlock.SetColor("_BaseColor", color);
        foreach (Renderer renderer in renderers) renderer.SetPropertyBlock(propertyBlock);
        if (ReleaseProgress < 1f) return;
        IsFinished = true;
        gameObject.SetActive(false);
        Action callback = finished;
        finished = null;
        callback?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (Mesh mesh in ownedMeshes) if (mesh != null) Destroy(mesh);
        ownedMeshes.Clear();
    }
}
