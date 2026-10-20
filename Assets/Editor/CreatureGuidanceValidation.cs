using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CreatureGuidanceValidation
{
    private static readonly MethodInfo Start = typeof(OceanCreatureDrift).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo Step = typeof(OceanCreatureDrift).GetMethod("ContinueFromGuidance", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly MethodInfo Struggle = typeof(OceanCreatureDrift).GetMethod("UpdateEntangled", BindingFlags.Instance | BindingFlags.NonPublic);

    [MenuItem("Underwater/Validate Creature Guidance")]
    public static void Run()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        OceanCreatureDrift turtle = CreateCreature(new Vector3(80f, 25f, 90f), Vector3.forward);
        Vector3 origin = turtle.transform.position;
        Advance(turtle, 12f);
        Require(turtle.transform.position.z > origin.z + 27f && Mathf.Abs(turtle.transform.position.x - origin.x) < 0.01f,
            "Turtle returns to an orbit instead of retaining its release direction");

        turtle.transform.position = new Vector3(80f, 2.7f, 90f);
        turtle.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.3f, 1f));
        turtle.ResumeFromGuidance(Vector3.forward * 3f);
        Advance(turtle, 70f);
        Require(turtle.transform.position.y >= UnderwaterWorld.WaterSurfaceHeight - 0.3f
            && turtle.transform.position.y <= UnderwaterWorld.WaterSurfaceHeight - 0.099f,
            "Released turtle cannot ascend through the complete water column to breathe");

        turtle.transform.position = new Vector3(80f, UnderwaterWorld.WaterSurfaceHeight - 0.7f, 90f);
        turtle.transform.rotation = Quaternion.LookRotation(new Vector3(0f, 0.2f, 1f));
        turtle.ResumeFromGuidance(Vector3.forward * 3f);
        Advance(turtle, 2f);
        Require(turtle.transform.position.y >= UnderwaterWorld.WaterSurfaceHeight - 0.3f
            && turtle.transform.position.y <= UnderwaterWorld.WaterSurfaceHeight - 0.099f,
            "Release discards upward heading, cannot reach the actual surface, or flies above it");
        Require(turtle.transform.forward.y > 0f && turtle.transform.forward.y <= 0.301f,
            "Released turtle loses its upward posture or tips too far");

        GameObject barrier = new("Creature validation wall");
        barrier.transform.position = new Vector3(80f, 25f, 93f);
        barrier.AddComponent<UnderwaterObstacle>();
        BoxCollider wall = barrier.AddComponent<BoxCollider>();
        wall.size = new Vector3(10f, 10f, 1f);
        Physics.SyncTransforms();
        turtle.transform.position = origin;
        turtle.transform.rotation = Quaternion.identity;
        turtle.ResumeFromGuidance(Vector3.forward * 3f);
        for (int i = 0; i < 720; i++)
        {
            Step.Invoke(turtle, new object[] { 1f / 60f });
            float distance = Vector3.Distance(wall.ClosestPoint(turtle.transform.position), turtle.transform.position);
            Require(distance >= 0.619f, "Released turtle penetrates a wall while choosing an escape course");
        }
        Require(Mathf.Abs(turtle.transform.position.x - origin.x) > 5f && Vector3.Distance(turtle.transform.position, origin) > 12f,
            "Released turtle remains stuck facing a wall");

        turtle.transform.position = new Vector3(400f, 25f, 64f);
        turtle.transform.rotation = Quaternion.LookRotation(Vector3.right);
        turtle.ResumeFromGuidance(Vector3.right * 3f);
        Advance(turtle, 4f);
        Require(turtle.transform.position.x <= 400f && Mathf.Abs(turtle.transform.position.z - 64f) > 4f,
            "Released turtle freezes at or passes through the exploration boundary");
        turtle.transform.position = new Vector3(80f, 25f, 80f);
        turtle.transform.rotation = Quaternion.identity;
        turtle.SetEntangled(true);
        Vector3 tiedPosition = turtle.transform.position;
        float greatestStruggle = 0f;
        for (int i = 0; i < 1200; i++)
        {
            Struggle.Invoke(turtle, new object[] { 1f / 60f });
            greatestStruggle = Mathf.Max(greatestStruggle, Vector3.Distance(tiedPosition, turtle.transform.position));
            Require(turtle.enabled && turtle.GetComponent<TurtleStrokeAnimator>().enabled,
                "Entanglement disables drift or flipper animation");
        }
        Require(greatestStruggle > 0.05f && greatestStruggle < 0.20f,
            "Entangled turtle either freezes or swims out of the rescue view");
        turtle.SetEntangled(false);
        Vector3 untiedPosition = turtle.transform.position;
        Advance(turtle, 2f);
        Require(turtle.transform.position.z > untiedPosition.z + 4f,
            "Untied turtle does not resume its last forward heading");
        Debug.Log("UNDERWATER_CREATURE_GUIDANCE_VALIDATED straight 12s; ascent to actual 48m surface and ceiling; head-on wall escape without penetration; exploration boundary escape; entangled 20s bounded nonzero struggle; untied forward swim");
    }

    private static OceanCreatureDrift CreateCreature(Vector3 position, Vector3 heading)
    {
        GameObject creature = new("Guidance validation turtle");
        creature.transform.SetPositionAndRotation(position, Quaternion.LookRotation(heading));
        creature.AddComponent<TurtleStrokeAnimator>();
        OceanCreatureDrift drift = creature.AddComponent<OceanCreatureDrift>();
        Start.Invoke(drift, null);
        drift.ResumeFromGuidance(heading * 3f);
        return drift;
    }

    private static void Advance(OceanCreatureDrift creature, float seconds)
    {
        for (int i = 0; i < Mathf.RoundToInt(seconds * 60f); i++) Step.Invoke(creature, new object[] { 1f / 60f });
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
