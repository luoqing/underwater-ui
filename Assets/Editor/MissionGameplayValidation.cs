using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class MissionGameplayValidation
{
    private const string Key = "Underwater.MissionValidation.";
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly string[] Roles = { "Turtle", "Manta", "Clownfish", "Whale" };
    private static Dictionary<MonoBehaviour, Vector3> motionSamples;

    static MissionGameplayValidation()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.update += Tick;
        Application.logMessageReceived += OnLog;
    }

    [MenuItem("Underwater/Validate Mission Gameplay And Exit")]
    public static void RunAll()
    {
        try
        {
            MovementSeafloorValidation.Run();
            CreatureGuidanceValidation.Run();
            Run();
        }
        catch (Exception error)
        {
            Debug.LogError("UNDERWATER_GAMEPLAY_FAILED " + error);
            EditorApplication.Exit(1);
        }
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start mission validation from Edit Mode.");
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetInt(Key + "Case", 0);
        SessionState.SetInt(Key + "Result", 1);
        SessionState.SetString(Key + "Error", string.Empty);
        SessionState.SetString(Key + "Phase", "Entering");
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddSeconds(110).Ticks.ToString());
        try
        {
            UnderwaterBuild.CreateScene();
            EditorApplication.isPlaying = true;
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        if (change == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetString(Key + "Phase", "Cases");
            WaitForFrames(1f);
        }
        else if (change == PlayModeStateChange.EnteredEditMode)
        {
            if (SessionState.GetString(Key + "Phase", "") != "Stopping")
            {
                SessionState.SetInt(Key + "Result", 1);
                Debug.LogError("UNDERWATER_MISSIONS_FAILED Play Mode ended before validation completed.");
            }
            ExitEditor();
        }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        try
        {
            string phase = SessionState.GetString(Key + "Phase", "");
            if (phase == "Stopping")
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode) ExitEditor();
                return;
            }
            long deadline = long.Parse(SessionState.GetString(Key + "Deadline", "0"));
            Require(DateTime.UtcNow.Ticks < deadline, "Timed out waiting for real Play Mode validation.");
            string runtimeError = SessionState.GetString(Key + "Error", "");
            Require(string.IsNullOrEmpty(runtimeError), runtimeError);
            if (phase == "Entering")
            {
                if (EditorApplication.isPlaying)
                {
                    SessionState.SetString(Key + "Phase", "Cases");
                    WaitForFrames(1f);
                }
                return;
            }
            if (!EditorApplication.isPlaying) return;
            if (Time.frameCount < SessionState.GetInt(Key + "Frame", 0)
                || EditorApplication.timeSinceStartup < SessionState.GetFloat(Key + "Wait", 0f)) return;

            OceanMissionDirector director = OceanMissionDirector.Instance;
            Require(director != null && Camera.main != null, "Production world did not create its mission director and camera.");
            if (phase == "Cases")
            {
                int index = SessionState.GetInt(Key + "Case", 0);
                if (index == 0) AdventureReachabilityValidation.ValidateInPlayMode();
                string role = Roles[Mathf.Min(index / 2, 3)];
                string mission = index % 2 == 0 ? "Adventure" : "Rescue";
                if (index == 8) { role = "Whale"; mission = "Rescue"; }
                ValidateCase(director, mission, role);
                Debug.Log($"UNDERWATER_MISSION_CASE_VALIDATED {mission}/{role}{(index == 8 ? " replay after reset" : "")}");
                SessionState.SetInt(Key + "Case", index + 1);
                if (index == 8)
                {
                    ValidateImportedArch();
                    SessionState.SetString(Key + "Phase", "MotionStart");
                }
                WaitForFrames(0.1f);
            }
            else if (phase == "MotionStart")
            {
                motionSamples = new Dictionary<MonoBehaviour, Vector3>();
                foreach (MonoBehaviour animal in Animals()) motionSamples.Add(animal, animal.transform.position);
                Require(motionSamples.Count >= 4, "World animal motion components are missing.");
                SessionState.SetString(Key + "Phase", "MotionEnd");
                WaitForFrames(1f);
            }
            else if (phase == "MotionEnd")
            {
                if (motionSamples == null)
                {
                    SessionState.SetString(Key + "Phase", "MotionStart");
                    return;
                }
                HashSet<Type> movedTypes = new();
                foreach (KeyValuePair<MonoBehaviour, Vector3> sample in motionSamples)
                {
                    Require(sample.Key != null && sample.Key.isActiveAndEnabled, "Leaving a mission disabled or destroyed an animal.");
                    if (Vector3.Distance(sample.Key.transform.position, sample.Value) > 0.001f)
                        movedTypes.Add(sample.Key.GetType());
                }
                Require(movedTypes.Contains(typeof(ReefFishDrift)) && movedTypes.Contains(typeof(OceanCreatureDrift))
                    && movedTypes.Contains(typeof(HeroWhaleDrift)), "Fish, turtle/manta and whale must keep moving after a mission.");
                Require(Camera.main.GetComponent<UniversalAdditionalCameraData>().requiresColorTexture,
                    "Water refraction has no opaque color input.");
                Light sun = GameObject.Find("Filtered Sunlight").GetComponent<Light>();
                Require(!sun.GetUniversalAdditionalLightData().usePipelineSettings && Mathf.Abs(sun.shadowBias - 0.04f) < 0.0001f,
                    "Sun shadow bias is still overridden by pipeline settings.");
                Rect[] controls = UnderwaterHud.GetTravelControlRects();
                Require(controls.Length == 6, "A swim arrow is missing.");
                foreach (Rect control in controls)
                    Require(control.width >= 44f && control.width <= 48f && control.xMin >= 0f && control.yMin >= 0f
                        && control.xMax <= Screen.width && control.yMax <= Screen.height, "A swim arrow is oversized or outside the viewport.");
                UnderwaterHud hud = UnderwaterHud.Instance;
                foreach (string id in new[] { "clownfish", "turtle", "manta", "whale" }) hud.Discover(id, id, string.Empty);
                Require(Read<HashSet<string>>(hud, "discoveries").Count == 4 && ((string)Call(hud, "BuildDiscoveryLine")).Contains("座头鲸"),
                    "Whale is missing from the four-species field guide.");
                Debug.Log("UNDERWATER_PRESENTATION_VALIDATED standalone font; six compact arrows; opaque color input; per-light shadow bias; four-species guide");
                Debug.Log($"UNDERWATER_MISSIONS_VALIDATED 2 missions x 4 roles; rescue replay; narration cleanup; material shaders; {motionSamples.Count} enabled animals, all motion groups active");
                Finish(0);
            }
        }
        catch (Exception error)
        {
            Fail(error);
        }
    }

    private static void ValidateCase(OceanMissionDirector director, string mission, string role)
    {
        Call(director, "ChooseMission", Choice("MissionMode", mission));
        Call(director, "StartAsRole", Choice("PlayerRole", role));
        Require(Read<object>(director, "mode").ToString() == mission && director.IsMissionActive,
            $"{mission}/{role}: normal role selection did not start the mission.");
        Require(Read<object>(director, "selectedRole").ToString() == role, "Selected role changed during mission setup.");
        if (mission == "Adventure")
        {
            List<GameObject> beacons = Read<List<GameObject>>(director, "adventureBeacons");
            Require(beacons.Count == 3, "Adventure must create three route lanterns.");
            for (int stage = 0; stage < beacons.Count; stage++)
            {
                Require(beacons[stage].activeSelf && Read<int>(director, "stage") == stage, "Wrong adventure lantern is active.");
                ValidateMaterials(beacons[stage].GetComponentsInChildren<Renderer>());
                Camera.main.transform.position = beacons[stage].transform.position + Vector3.back * 2f;
                Call(director, "Update");
                Require(!beacons[stage].activeSelf && Read<int>(director, "stage") == stage + 1,
                    "Approaching a lantern did not advance the adventure.");
            }
        }
        else
        {
            GameObject turtle = Read<GameObject>(director, "rescueTurtle");
            FishingLineRescue line = Read<FishingLineRescue>(director, "fishingLine");
            OceanCreatureDrift drift = turtle.GetComponent<OceanCreatureDrift>();
            Require(drift != null && drift.enabled && line != null, "Rescue initialization froze the turtle or omitted the fishing line.");
            Require(turtle.transform.position.y < UnderwaterWorld.WaterSurfaceHeight - 1f, "Replayed rescue started with the turtle still at the surface.");
            Require(Mathf.Abs(turtle.transform.position.y - (UnderwaterWorld.WaterSurfaceHeight - 6f)) < 0.01f,
                "Rescue starts too deep for the short child-friendly ascent.");
            if (role == "Turtle") AdventureReachabilityValidation.ValidateRescueSightline();
            foreach (LineRenderer loop in Read<List<LineRenderer>>(line, "loops")) ValidateMaterials(new Renderer[] { loop });
            foreach (MissionInteractable knot in Read<List<MissionInteractable>>(line, "knots"))
                ValidateMaterials(knot.GetComponentsInChildren<Renderer>());
            director.TryUntie(2);
            Require(line.NextKnot == 0, "An out-of-order knot incorrectly advanced the rescue.");
            for (int knot = 0; knot < line.KnotCount; knot++)
            {
                director.TryUntie(knot);
                Require(line.NextKnot == knot + 1, "An in-order knot did not release.");
            }
            Require(Read<int>(director, "stage") == 1 && drift.enabled, "Untying three knots did not start the breathing stage.");
            object[] target = { Vector3.zero };
            float surface = GameObject.Find("Moving Water Surface").transform.position.y;
            Require((bool)Call(director, "TryGetCurrentTarget", target) && Mathf.Abs(((Vector3)target[0]).y - (surface - 0.3f)) < 0.001f,
                "Rescue target is not fixed at the real surface height.");
            Vector3 position = turtle.transform.position;
            turtle.transform.position = new Vector3(position.x, 6.25f, position.z);
            Call(director, "Update");
            Require(Read<object>(director, "mode").ToString() == "Rescue", "Rescue completed before reaching the surface.");
            turtle.transform.position = new Vector3(position.x, surface - 0.25f, position.z);
            Call(director, "Update");
        }
        Require(Read<object>(director, "mode").ToString() == "Complete"
            && Read<object>(director, "completedMission").ToString() == mission, $"{mission}/{role}: completion was not reached.");
        AudioSource narration = Read<AudioSource>(director, "dialogueSource");
        Require(Read<AudioClip>(director, "pendingDialogueClip") != null || narration.clip != null,
            "Completion narration was neither queued nor assigned.");
        Call(director, "ResetMission");
        Require(!director.IsMissionActive && Read<object>(director, "mode").ToString() == "None"
            && Read<object>(director, "pendingMission").ToString() == "None", "Reset retained the previous mission.");
        Require(Read<AudioClip>(director, "pendingDialogueClip") == null && narration.clip == null && !narration.isPlaying,
            "Reset retained queued or playing narration.");
        Require(Read<FishingLineRescue>(director, "fishingLine") == null, "Reset retained fishing line ownership.");
        foreach (GameObject beacon in Read<List<GameObject>>(director, "adventureBeacons"))
            Require(!beacon.activeSelf, "Reset left a route lantern active.");
        foreach (MonoBehaviour animal in Animals()) Require(animal.isActiveAndEnabled, "A mission disabled an animal's normal motion.");
    }

    private static IEnumerable<MonoBehaviour> Animals()
    {
        foreach (MonoBehaviour value in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (value is ReefFishDrift || value is OceanCreatureDrift || value is HeroWhaleDrift) yield return value;
    }

    private static void ValidateImportedArch()
    {
        GameObject arch = GameObject.Find("Natural Coral Reef Arch");
        Require(arch != null && arch.GetComponentsInChildren<MeshCollider>().Length > 0, "Imported reef arch mesh colliders are missing.");
        Renderer[] renderers = arch.GetComponentsInChildren<Renderer>();
        Require(renderers.Length > 0, "Imported reef arch has no mesh renderer.");
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
        Transform mover = Camera.main.transform;
        Vector3 original = mover.position;
        CinematicDive dive = mover.GetComponent<CinematicDive>();
        int totalPasses = 0;
        Physics.SyncTransforms();
        foreach (string role in Roles)
        {
            dive.ConfigurePlayerRole(role.ToLowerInvariant());
            int passes = 0;
            Vector3 firstStart = default;
            Vector3 firstEnd = default;
            foreach (Vector3 direction in new[] { arch.transform.forward, arch.transform.right })
            {
                Vector3 side = Vector3.Cross(Vector3.up, direction).normalized;
                float depth = Vector3.Dot(new Vector3(Mathf.Abs(direction.x), 0f, Mathf.Abs(direction.z)), bounds.extents);
                float width = Vector3.Dot(new Vector3(Mathf.Abs(side.x), 0f, Mathf.Abs(side.z)), bounds.extents);
                for (int lane = -2; lane <= 2; lane++)
                foreach (float height in new[] { 0.28f, 0.40f, 0.52f, 0.64f })
                {
                    Vector3 center = bounds.center + side * (lane * width * 0.09f);
                    center.y = Mathf.Lerp(bounds.min.y, bounds.max.y, height);
                    Vector3 start = center - direction * (depth + 3f);
                    Vector3 end = center + direction * (depth + 3f);
                    mover.position = start;
                    Vector3 result = UnderwaterCollision.ResolveMove(mover, end, Read<float>(dive, "playerRadius"),
                        Read<float>(dive, "playerFloorClearance"), Read<float>(dive, "playerNoseDistance"), out bool adjusted);
                    if (adjusted || Vector3.Distance(result, end) > 0.01f) continue;
                    if (passes == 0) { firstStart = start; firstEnd = end; }
                    passes++;
                }
            }
            Debug.Log($"UNDERWATER_REAL_ARCH_PATH {role} passed={passes}/40 start={firstStart.ToString("F3")} end={firstEnd.ToString("F3")} actual imported mesh, central opening candidates");
            totalPasses += passes;
        }
        mover.position = original;
        dive.ConfigurePlayerRole("whale");
        Require(totalPasses > 0, "No sampled central path through the imported reef arch was passable.");
    }

    private static void ValidateMaterials(Renderer[] renderers)
    {
        Require(renderers.Length > 0, "A mission prop has no renderer.");
        foreach (Renderer renderer in renderers)
            foreach (Material material in renderer.sharedMaterials)
                Require(material != null && material.shader != null, $"Missing mission shader on {renderer.name}.");
    }

    private static object Choice(string type, string value) => Enum.Parse(typeof(OceanMissionDirector).GetNestedType(type, BindingFlags.NonPublic), value);
    private static T Read<T>(object target, string name) => (T)target.GetType().GetField(name, PrivateInstance).GetValue(target);
    private static object Call(object target, string name, params object[] arguments) => target.GetType().GetMethod(name, PrivateInstance).Invoke(target, arguments);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private static void WaitForFrames(float seconds)
    {
        SessionState.SetInt(Key + "Frame", Time.frameCount + 2);
        SessionState.SetFloat(Key + "Wait", (float)EditorApplication.timeSinceStartup + seconds);
    }

    private static void OnLog(string message, string stack, LogType type)
    {
        // Editor-only search-index failures do not execute in a WebGL player.
        if (stack.Contains("UnityEditor.Search.") && !stack.Contains("Assets/Scripts/")) return;
        if (type == LogType.Exception && SessionState.GetBool(Key + "Active", false))
            SessionState.SetString(Key + "Error", message + "\n" + stack);
    }

    private static void Fail(Exception error)
    {
        if (error is TargetInvocationException invocation && invocation.InnerException != null) error = invocation.InnerException;
        Debug.LogError("UNDERWATER_MISSIONS_FAILED " + error);
        Finish(1);
    }

    private static void Finish(int result)
    {
        SessionState.SetInt(Key + "Result", result);
        SessionState.SetString(Key + "Phase", "Stopping");
        if (EditorApplication.isPlayingOrWillChangePlaymode) EditorApplication.isPlaying = false;
        else ExitEditor();
    }

    private static void ExitEditor()
    {
        int result = SessionState.GetInt(Key + "Result", 1);
        SessionState.SetBool(Key + "Active", false);
        EditorApplication.Exit(result);
    }
}
