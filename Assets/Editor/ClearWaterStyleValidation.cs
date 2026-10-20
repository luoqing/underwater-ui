using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ClearWaterStyleValidation
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    public static void Run()
    {
        foreach (bool original in new[] { false, true })
        {
            GameObject prototype = new("Water configuration probe");
            prototype.SetActive(false);
            try
            {
                UnderwaterWorld world = prototype.AddComponent<UnderwaterWorld>();
                typeof(UnderwaterWorld).GetMethod("ConfigureWaterLook", Private).Invoke(world,
                    new object[] { original ? "water=layered&look=original" : "water=layered" });
                Require(Read<bool>(world, "clearWaterLook") != original && Read<bool>(world, "useLayeredFog"), "Water comparison mode is incorrect.");
                Color top = Read<Color>(world, "layeredTop");
                Color horizon = Read<Color>(world, "layeredHorizon");
                Color bottom = Read<Color>(world, "layeredBottom");
                Require(top.grayscale > horizon.grayscale && horizon.grayscale > bottom.grayscale,
                    "Water must retain its light-to-deep gradient.");
                Require(Mathf.Abs(top.r - (original ? 0.10f : 0.24f)) < 0.0001f
                    && Mathf.Abs(Read<float>(world, "postSaturation") - (original ? 12f : 2f)) < 0.0001f,
                    "Original comparison or clear-water grading changed unexpectedly.");
            }
            finally { UnityEngine.Object.DestroyImmediate(prototype); }
        }
        Debug.Log("UNDERWATER_CLEAR_WATER_CONFIG_VALIDATED clear default; original layered comparison; ordered water luminance");
    }

    public static void ValidateLive()
    {
        UnderwaterWorld world = UnityEngine.Object.FindFirstObjectByType<UnderwaterWorld>();
        Require(Read<bool>(world, "clearWaterLook"), "Editor acceptance is not exercising the clear-water look.");
        Volume volume = Read<Volume>(world, "postVolume");
        Require(volume.profile.TryGet(out DepthOfField depth) && !depth.active, "Clear-water detail is still blurred by depth of field.");
        Light sun = GameObject.Find("Filtered Sunlight").GetComponent<Light>();
        foreach (GameObject ray in GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (ray.name != "Volumetric Sun Ray") continue;
            Require(Vector3.Dot(ray.transform.up, -sun.transform.forward) > 0.999f, "Light shafts disagree with the sun direction.");
            Require(ray.transform.localScale.x <= 7.281f && ray.transform.localScale.z <= 7.281f, "Light shafts are still wide columns.");
        }
        Rect[] arrows = UnderwaterHud.GetTravelControlRects();
        Require(arrows[3].xMax < Screen.width * 0.5f && arrows[4].xMin > Screen.width * 0.5f,
            "Movement and depth arrows must remain at opposite bottom corners.");
        FishSwimmer[] fish = UnityEngine.Object.FindObjectsByType<FishSwimmer>(FindObjectsSortMode.None);
        HashSet<Vector3> schools = new();
        HashSet<Mesh> meshes = new();
        HashSet<Material> materials = new();
        foreach (FishSwimmer swimmer in fish)
        {
            schools.Add(swimmer.schoolCenter);
            foreach (MeshFilter filter in swimmer.GetComponentsInChildren<MeshFilter>()) meshes.Add(filter.sharedMesh);
            foreach (Renderer renderer in swimmer.GetComponentsInChildren<Renderer>())
            {
                materials.Add(renderer.sharedMaterial);
                Require(renderer.sharedMaterial.enableInstancing, "A school fish lost its shared instancing-enabled material.");
            }
        }
        Require(fish.Length == 250 && schools.Count == 7, "The reef does not contain 250 fish in seven distributed schools.");
        Require(meshes.Count == 4 && materials.Count <= 25, "Extra schools created per-fish mesh or material copies.");
        foreach (string name in new[] { "Warm red coral accent", "Violet coral accent", "Golden coral accent", "Blue coral accent", "Rust orange coral accent" })
        {
            GameObject coral = GameObject.Find(name);
            Require(coral != null && Mathf.Abs(coral.transform.position.x) >= 7f,
                "A coloured coral colony is missing or blocks the central swim lane.");
        }
        Debug.Log($"UNDERWATER_REEF_DISTRIBUTION_VALIDATED fish={fish.Length}; schools={schools.Count}; sharedMeshes={meshes.Count}; sharedMaterials={materials.Count}; five side-bank coral accents; GPU frame rate not measured");
        Debug.Log("UNDERWATER_CLEAR_WATER_LIVE_VALIDATED sharp depth; sun-aligned narrow shafts; split compact arrows");
    }

    private static T Read<T>(object subject, string name) => (T)subject.GetType().GetField(name, Private).GetValue(subject);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
