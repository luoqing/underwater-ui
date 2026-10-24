using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class FishSchoolMotionValidation
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void RunAndExit()
    {
        try { Run(); EditorApplication.Exit(0); }
        catch (Exception error) { Debug.LogError("UNDERWATER_FISH_MOTION_FAILED " + error); EditorApplication.Exit(1); }
    }

    public static void Run()
    {
        foreach (float step in new[] { 1f / 60f, 1f / 30f, 0.1f }) Validate(step);
        Debug.Log("UNDERWATER_FISH_MOTION_VALIDATED production SwimStep; 12s warmup and 18s measurement; three timesteps; sustained travel and bounded formation");
    }

    private static void Validate(float step)
    {
        GameObject probe = new("Fish motion regression probe");
        try
        {
            FishSwimmer swimmer = probe.AddComponent<FishSwimmer>();
            swimmer.schoolCenter = new Vector3(0f, 25f, 30f);
            swimmer.radius = 5.5f;
            swimmer.speed = 2f;
            swimmer.phase = 0.4f;
            probe.transform.position = swimmer.schoolCenter;
            typeof(FishSwimmer).GetMethod("Start", Private).Invoke(swimmer, null);
            MethodInfo advance = typeof(FishSwimmer).GetMethod("SwimStep", Private);
            float travelled = 0f;
            float measuredTime = 0f;
            float furthest = 0f;
            float topSpeed = 0f;
            int frames = Mathf.RoundToInt(30f / step);
            for (int frame = 0; frame < frames; frame++)
            {
                Vector3 previous = probe.transform.position;
                float time = frame * step;
                advance.Invoke(swimmer, new object[] { time, step });
                Vector3 position = probe.transform.position;
                Require(float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z), "A fish produced an invalid position.");
                if (time < 12f) continue;
                float distance = Vector3.Distance(previous, position);
                travelled += distance;
                measuredTime += step;
                furthest = Mathf.Max(furthest, Vector3.Distance(position, swimmer.schoolCenter));
                topSpeed = Mathf.Max(topSpeed, distance / step);
            }
            float meanSpeed = travelled / measuredTime;
            Debug.Log($"UNDERWATER_FISH_MOTION_SAMPLE dt={step:F4}; mean={meanSpeed:F3}m/s; peak={topSpeed:F3}m/s; radius={furthest:F3}m");
            Require(meanSpeed >= 0.65f && meanSpeed <= 2.2f, "School fish still hover or dart: measured mean speed " + meanSpeed);
            Require(topSpeed <= 3.5f && furthest <= 12f, "A school fish escapes its reef or snaps during turns.");
        }
        finally { UnityEngine.Object.DestroyImmediate(probe); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
