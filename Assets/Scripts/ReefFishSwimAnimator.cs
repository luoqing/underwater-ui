using System.Collections.Generic;
using UnityEngine;

// Keep the original detailed mesh/materials. Only the rear body and tail bend.
public sealed class ReefFishSwimAnimator : MonoBehaviour
{
    public float phase;
    public bool isBlueTang;
    private sealed class SharedSkin { public Mesh mesh; public int users; }
    private static readonly Dictionary<Mesh, SharedSkin> Skins = new();
    private readonly Transform[] bones = new Transform[4];
    private Mesh sourceMesh;
    private SharedSkin skin;

    private void Start()
    {
        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        MeshRenderer original = filter == null ? null : filter.GetComponent<MeshRenderer>();
        if (original == null || filter.sharedMesh == null || !filter.sharedMesh.isReadable)
        {
            enabled = false;
            return;
        }
        sourceMesh = filter.sharedMesh;
        Bounds bounds = sourceMesh.bounds;
        Vector3 head = isBlueTang ? new Vector3(0f, 0f, bounds.max.z) : new Vector3(bounds.min.x, 0f, 0f);
        Vector3 tail = isBlueTang ? new Vector3(0f, 0f, bounds.min.z) : new Vector3(bounds.max.x, 0f, 0f);
        float[] positions = { 0f, 0.46f, 0.68f, 0.86f };
        for (int i = 0; i < bones.Length; i++)
        {
            bones[i] = new GameObject(i == 0 ? "Fish head and body" : $"Fish tail joint {i}").transform;
            bones[i].SetParent(i == 0 ? filter.transform : bones[i - 1], false);
            bones[i].position = filter.transform.TransformPoint(i == 0 ? Vector3.zero : Vector3.Lerp(head, tail, positions[i]));
        }
        if (!Skins.TryGetValue(sourceMesh, out skin))
        {
            Mesh mesh = Instantiate(sourceMesh);
            mesh.name = sourceMesh.name + " shared swimming skin";
            Matrix4x4[] bindposes = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++) bindposes[i] = bones[i].worldToLocalMatrix * filter.transform.localToWorldMatrix;
            Vector3[] vertices = mesh.vertices;
            BoneWeight[] weights = new BoneWeight[vertices.Length];
            float length = Mathf.Max(0.001f, Vector3.Distance(head, tail));
            for (int i = 0; i < vertices.Length; i++)
            {
                float along = isBlueTang ? (head.z - vertices[i].z) / length : (vertices[i].x - head.x) / length;
                int joint = along <= positions[1] ? 0 : along <= positions[2] ? 1 : 2;
                float blend = Mathf.InverseLerp(positions[joint], positions[joint + 1], along);
                if (joint == 0) blend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.28f, positions[1], along));
                weights[i] = new BoneWeight { boneIndex0 = joint, weight0 = 1f - blend, boneIndex1 = joint + 1, weight1 = blend };
            }
            mesh.bindposes = bindposes;
            mesh.boneWeights = weights;
            skin = new SharedSkin { mesh = mesh };
            Skins.Add(sourceMesh, skin);
        }
        skin.users++;
        GameObject meshObject = new("Swimming mesh");
        meshObject.transform.SetParent(filter.transform, false);
        SkinnedMeshRenderer animated = meshObject.AddComponent<SkinnedMeshRenderer>();
        animated.sharedMesh = skin.mesh;
        animated.sharedMaterials = original.sharedMaterials;
        animated.bones = bones;
        animated.rootBone = bones[0];
        animated.quality = SkinQuality.Bone2;
        bounds.Expand(bounds.size.magnitude * 0.25f);
        animated.localBounds = bounds;
        animated.shadowCastingMode = original.shadowCastingMode;
        animated.receiveShadows = original.receiveShadows;
        original.enabled = false;
    }

    private void LateUpdate() => AnimateTail(Time.time);

    private void AnimateTail(float time)
    {
        if (skin == null) return;
        float cycle = time * (isBlueTang ? 10.5f : 11.5f) + phase;
        for (int i = 1; i < bones.Length; i++)
            bones[i].localRotation = Quaternion.Euler(0f, Mathf.Sin(cycle - i * 0.62f) * (4f + i * 4f), 0f);
    }

    private void OnDestroy()
    {
        if (skin == null || --skin.users > 0) return;
        Skins.Remove(sourceMesh);
        Destroy(skin.mesh);
    }
}
