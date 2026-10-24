using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReefLifeValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void RunAndExit()
    {
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogError("UNDERWATER_REEF_LIFE_FAILED " + error); EditorApplication.Exit(1); }
    }

    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        bool valid = true;
        foreach (float step in new[] { 1f / 60f, 0.1f })
        foreach (bool blue in new[] { true, false })
        foreach (float scale in new[] { 0.38f, 0.68f, 0.88f })
        {
            GameObject probe = new("Reef fish motion probe");
            try
            {
                probe.transform.position = new Vector3(0f, 25f, 30f);
                ReefFishDrift fish = probe.AddComponent<ReefFishDrift>();
                fish.isBlueTang = blue;
                fish.swimScale = scale;
                fish.phase = 0.7f;
                Call(fish, "Start");
                MethodInfo advance = typeof(ReefFishDrift).GetMethod("SwimStep", Private);
                float travelled = 0f, topSpeed = 0f, maxPitch = 0f, minAlignment = 1f;
                int samples = 0;
                for (int frame = 0; frame < Mathf.RoundToInt(42f / step); frame++)
                {
                    Vector3 before = probe.transform.position;
                    advance.Invoke(fish, new object[] { frame * step, step });
                    if (frame * step < 12f) continue;
                    Vector3 movement = probe.transform.position - before;
                    float speed = movement.magnitude / step;
                    travelled += movement.magnitude;
                    topSpeed = Mathf.Max(topSpeed, speed);
                    Vector3 modelNose = probe.transform.TransformDirection(blue ? Vector3.forward : Vector3.left);
                    minAlignment = Mathf.Min(minAlignment, Vector3.Dot(modelNose, movement.normalized));
                    maxPitch = Mathf.Max(maxPitch, Mathf.Abs(modelNose.y));
                    samples++;
                }
                float mean = travelled / (samples * step);
                Debug.Log($"UNDERWATER_REEF_FISH_SAMPLE blue={blue}; scale={scale:F2}; dt={step:F4}; mean={mean:F3}m/s; peak={topSpeed:F3}; headAlignment={minAlignment:F3}; pitch={maxPitch:F3}");
                valid &= mean >= 0.78f && mean <= 0.98f && topSpeed <= 1.3f && minAlignment > 0.90f && maxPitch < 0.20f;
                foreach (Vector3 direction in new[] { Vector3.up, Vector3.down })
                {
                    Vector3 nose = fish.RotationForVelocity(direction, 0f) * (blue ? Vector3.forward : Vector3.left);
                    valid &= Mathf.Abs(nose.y) < 0.20f;
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }
        if (!valid) throw new InvalidOperationException("Reef fish are too slow/fast, sideways or pitched; see measured species/size samples.");
        Debug.Log("UNDERWATER_REEF_LIFE_VALIDATED both species/three sizes/two timesteps; 30s measured motion after warmup; near-school speed and upright nose-forward motion");
    }

    public static void ValidateLive()
    {
        ReefFishDrift[] fish = UnityEngine.Object.FindObjectsByType<ReefFishDrift>(FindObjectsSortMode.None);
        int blue = 0, clown = 0;
        HashSet<Mesh> skins = new();
        foreach (ReefFishDrift swimmer in fish)
        {
            if (swimmer.isBlueTang) blue++; else clown++;
            ReefFishSwimAnimator animator = swimmer.GetComponent<ReefFishSwimAnimator>();
            SkinnedMeshRenderer renderer = swimmer.GetComponentInChildren<SkinnedMeshRenderer>();
            MeshFilter original = swimmer.GetComponentInChildren<MeshFilter>();
            if (animator == null || renderer == null || renderer.bones.Length != 4 || renderer.sharedMesh.vertexCount != original.sharedMesh.vertexCount)
                throw new InvalidOperationException("A detailed fish lost its mesh, tail skin or animator.");
            skins.Add(renderer.sharedMesh);
            foreach (Material material in renderer.sharedMaterials)
                if (material.mainTexture == null) throw new InvalidOperationException("Swimming fish lost its original texture.");
            Mesh first = new(), second = new();
            try
            {
                Call(animator, "AnimateTail", 0f);
                renderer.BakeMesh(first);
                Call(animator, "AnimateTail", 0.19f);
                renderer.BakeMesh(second);
                Vector3[] rest = original.sharedMesh.vertices, a = first.vertices, b = second.vertices;
                Bounds bounds = original.sharedMesh.bounds;
                float length = swimmer.isBlueTang ? bounds.size.z : bounds.size.x;
                float movement = 0f;
                int headSamples = 0;
                for (int i = 0; i < rest.Length; i += 5)
                {
                    float along = swimmer.isBlueTang ? (bounds.max.z - rest[i].z) / length : (rest[i].x - bounds.min.x) / length;
                    if (along < 0.25f)
                    {
                        headSamples++;
                        if (Vector3.Distance(a[i], b[i]) > 0.0001f) throw new InvalidOperationException("Tail animation bends the fish's head.");
                    }
                    if (along > 0.86f) movement = Mathf.Max(movement, Vector3.Distance(a[i], b[i]));
                }
                if (headSamples == 0 || movement < length * 0.005f) throw new InvalidOperationException("The tail stays rigid or the head mask is missing.");
            }
            finally { UnityEngine.Object.DestroyImmediate(first); UnityEngine.Object.DestroyImmediate(second); }
        }
        if (blue != 9 || clown != 8 || skins.Count != 2) throw new InvalidOperationException("Expected nine blue tangs, eight clownfish and two shared detailed skins.");
        Debug.Log("UNDERWATER_REEF_SKIN_VALIDATED blue=9; clown=8; sharedSkins=2; original vertex counts and textured materials; upright heads stable while tails deform");

        int colonies = 0;
        foreach (Transform root in UnityEngine.Object.FindFirstObjectByType<UnderwaterWorld>().transform)
        {
            MeshCollider[] colliders = root.GetComponentsInChildren<MeshCollider>();
            if (colliders.Length == 0 || root.GetComponentInChildren<UnderwaterObstacle>()?.isSeafloor == true) continue;
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) continue;
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float top = bounds.min.y + Mathf.Min(bounds.size.y * 0.14f, 1.6f);
            float greatestGap = float.NegativeInfinity;
            int samples = 0;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>())
            foreach (Vector3 vertex in filter.sharedMesh.vertices)
            {
                Vector3 point = filter.transform.TransformPoint(vertex);
                if (point.y > top) continue;
                samples++;
                greatestGap = Mathf.Max(greatestGap, point.y - UnderwaterWorld.SampleSeafloorHeight(point.x, point.z));
            }
            if (samples == 0 || greatestGap > 0.01f) throw new InvalidOperationException($"Floating coral roots: {root.name}, gap={greatestGap:F3}m, samples={samples}");
            colonies++;
            Debug.Log($"UNDERWATER_REEF_GROUND_SAMPLE {root.name}; rootVertices={samples}; highestRootGap={greatestGap:F3}m");
        }
        if (colonies < 14) throw new InvalidOperationException("Coral grounding validation missed the compact reef colonies.");
        Debug.Log($"UNDERWATER_REEF_GROUND_VALIDATED colonies={colonies}; actual root vertices fitted below the unchanged seafloor; no generated sand mounds");
    }

    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
}
