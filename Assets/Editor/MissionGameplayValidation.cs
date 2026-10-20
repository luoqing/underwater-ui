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
            ClearWaterStyleValidation.Run();
            RescueRopeValidation.Run();
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
                ClearWaterStyleValidation.ValidateLive();
                ValidateMinimalHud(director, hud);
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
        string prompt = Read<string>(director, "objective");
        Require(!string.IsNullOrEmpty(prompt) && !prompt.Contains("\n") && prompt.Length <= 40,
            "The current objective is missing or has expanded into a multi-line instruction block.");
        Call(director, "OpenMenu");
        Require(director.IsMenuOpen && director.IsMissionActive && Read<int>(director, "stage") == 0,
            "Opening the menu reset the current mission.");
        Require(director.IsPointerOverUi(Vector2.zero), "An open menu lets world interactions through outside its panel.");
        typeof(OceanMissionDirector).GetField("showMenu", PrivateInstance).SetValue(director, false);
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
            if (role == "Turtle")
            {
                Call(director, "ResetMission");
                Require(turtle.GetComponentsInChildren<MissionInteractable>().Length == 0,
                    "Abandoned rescue knots remain active during immediate replay.");
                Call(director, "ChooseMission", Choice("MissionMode", mission));
                Call(director, "StartAsRole", Choice("PlayerRole", role));
                line = Read<FishingLineRescue>(director, "fishingLine");
            }
            OceanCreatureDrift drift = turtle.GetComponent<OceanCreatureDrift>();
            Require(drift != null && drift.enabled && line != null, "Rescue initialization froze the turtle or omitted the fishing line.");
            RescueRopeValidation.ValidateInstance(line);
            CinematicDive pointerDive = Camera.main.GetComponent<CinematicDive>();
            Require(line.TryGetNextKnotPosition(out Vector3 firstKnot), "Rescue has no next knot position.");
            Vector3 knotPointer = Camera.main.WorldToScreenPoint(firstKnot);
            Require(!(bool)Call(pointerDive, "PointerHitsEncounter", new Vector2(knotPointer.x, knotPointer.y), turtle.GetComponent<MarineEncounter>()),
                "An already selected turtle steals presses from its rescue knots.");
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
            CinematicDive dive = Camera.main.GetComponent<CinematicDive>();
            Require(director.ActionHint == "点一下豆豆，选中它", "The rescue introduced dragging before selecting the turtle.");
            Require(Read<bool>(drift, "awaitingGuidance"), "The freed turtle swims out of reach while the child reads.");
            Vector3 turtlePointer = Camera.main.WorldToScreenPoint(turtle.transform.position);
            Call(dive, "TryDiscover", new Vector2(turtlePointer.x, turtlePointer.y));
            Require(dive.GuidedEncounter == turtle.GetComponent<MarineEncounter>(), "One real hit on the rescue turtle did not select it.");
            Require(director.ActionHint == "按住豆豆，向上拖", "Selecting the turtle did not advance its action hint.");
            typeof(CinematicDive).GetField("guidingAnimal", PrivateInstance).SetValue(dive, true);
            Require(director.ActionHint == "向上拖，带豆豆靠近水面", "Dragging the turtle did not advance its action hint.");
            Vector3 beforeReleaseHint = turtle.transform.position;
            turtle.transform.position = new Vector3(beforeReleaseHint.x, UnderwaterWorld.WaterSurfaceHeight - 1.1f, beforeReleaseHint.z);
            Require(director.ActionHint == "松开，豆豆会继续往上游", "The final ascent omits the release instruction.");
            turtle.transform.position = beforeReleaseHint;
            typeof(CinematicDive).GetField("guidedEncounter", PrivateInstance).SetValue(dive, null);
            typeof(CinematicDive).GetField("guidingAnimal", PrivateInstance).SetValue(dive, false);
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

    private static void ValidateMinimalHud(OceanMissionDirector director, UnderwaterHud hud)
    {
        Require(!Read<bool>(hud, "discoveryOpen") && !Read<bool>(director, "showSubtitles"),
            "The field guide or story subtitles must not open over the scenery by default.");
        Rect guideButton = UnderwaterHud.GetDiscoveryRect();
        Require(guideButton.height <= 46f && guideButton.width <= 98f, "The closed guide still occupies a large panel.");
        Vector2 guidePointer = new(guideButton.center.x, Screen.height - guideButton.center.y);
        Require(UnderwaterHud.IsPointerOverTravelControls(guidePointer), "The visible field-guide button is not protected from world clicks.");
        float scale = Mathf.Clamp(Screen.width / 1440f, 0.72f, 1.08f);
        GUIStyle guideTextStyle = Read<GUIStyle>(hud, "panelTextStyle");
        GUIStyle guideTitleStyle = Read<GUIStyle>(hud, "panelTitleStyle");
        // Batch Play Mode does not dispatch OnGUI. Measure with the game's label
        // skin and production font/settings, then verify in a browser.
        if (guideTextStyle == null)
        {
            Font font = Resources.Load<Font>("Fonts/BlueLanternUI");
            GUIStyle label = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Game).label;
            guideTextStyle = new GUIStyle(label) { font = font, fontSize = Mathf.Max(11, Mathf.RoundToInt(12f * scale)), richText = true, wordWrap = true };
            guideTitleStyle = new GUIStyle(label) { font = font, fontSize = Mathf.Max(12, Mathf.RoundToInt(14f * scale)), fontStyle = FontStyle.Bold };
            typeof(UnderwaterHud).GetField("panelTextStyle", PrivateInstance).SetValue(hud, guideTextStyle);
            typeof(UnderwaterHud).GetField("panelTitleStyle", PrivateInstance).SetValue(hud, guideTitleStyle);
        }
        typeof(UnderwaterHud).GetField("discoveryOpen", PrivateInstance).SetValue(hud, true);
        Rect guidePanel = UnderwaterHud.GetDiscoveryRect();
        float headerHeight = guideTitleStyle.CalcHeight(new GUIContent("海洋图鉴   0 / 4"), guidePanel.width - 32f * scale);
        float bodyHeight = guideTextStyle.CalcHeight(new GUIContent((string)Call(hud, "BuildDiscoveryLine")), guidePanel.width - 32f * scale);
        float requiredHeight = headerHeight + bodyHeight + 48f * scale;
        Require(guidePanel.height + 0.01f >= requiredHeight,
            $"The four-species guide clips its second line: panel={guidePanel.height}, required={requiredHeight}, header={headerHeight}, body={bodyHeight}, scale={scale}.");
        Vector2 panelPointer = new(guidePanel.center.x, Screen.height - guidePanel.center.y);
        Require(UnderwaterHud.IsPointerOverTravelControls(panelPointer), "The expanded guide lets world clicks through.");
        typeof(UnderwaterHud).GetField("discoveryOpen", PrivateInstance).SetValue(hud, false);
        Require(!UnderwaterHud.IsPointerOverTravelControls(panelPointer), "The hidden field-guide panel still blocks the scenery.");
        Rect objective = (Rect)Call(director, "GetObjectiveRect");
        Require(objective.x < Screen.width * 0.1f && objective.y < Screen.height * 0.1f,
            "The current objective is not in the upper-left corner.");
        Require(!director.IsPointerOverUi(new Vector2(objective.center.x, Screen.height - objective.center.y)),
            "Non-interactive objective text still creates an invisible input blocker.");
        CinematicDive dive = Camera.main.GetComponent<CinematicDive>();
        typeof(UnderwaterHud).GetField("manualTravelDistance", PrivateInstance).SetValue(hud, 0f);
        typeof(UnderwaterHud).GetField("manualLookPixels", PrivateInstance).SetValue(hud, 0f);
        hud.SetHover(string.Empty, false, Vector2.zero);
        Require(((string)Call(hud, "BuildControlsHint", dive)).Contains("往前游"), "Movement must be introduced first.");
        Call(director, "OpenMenu");
        hud.RecordManualTravel(2f);
        Require(Read<float>(hud, "manualTravelDistance") == 0f, "Opening a menu advanced the movement lesson.");
        typeof(OceanMissionDirector).GetField("showMenu", PrivateInstance).SetValue(director, false);
        hud.RecordManualTravel(0f);
        Require(((string)Call(hud, "BuildControlsHint", dive)).Contains("往前游"), "Blocked movement advanced the lesson.");
        hud.RecordManualTravel(0.7f);
        Require(((string)Call(hud, "BuildControlsHint", dive)).Contains("看看四周"), "Real movement did not introduce looking around.");
        hud.RecordManualLook(20f);
        Require(((string)Call(hud, "BuildControlsHint", dive)).Contains("看看四周"), "A tiny drag dismissed the looking lesson.");
        hud.RecordManualLook(20f);
        Require(string.IsNullOrEmpty((string)Call(hud, "BuildControlsHint", dive)), "Completed basic instructions remained on screen.");
        hud.SetHover("海龟", false, Vector2.zero);
        Require(((string)Call(hud, "BuildControlsHint", dive)).Contains("点一下海龟"), "A new animal does not get its contextual action.");
        hud.SetHover("海龟", true, Vector2.zero);
        Require(((string)Call(hud, "BuildControlsHint", dive)).Contains("双击海龟"), "Discovery did not advance the animal interaction hint.");
        hud.SetHover(string.Empty, false, Vector2.zero);
        Debug.Log("UNDERWATER_PROGRESSIVE_HINTS_VALIDATED movement then look; no menu/blocked-motion advancement; context-aware animal actions; completed basics hidden");
        Debug.Log("UNDERWATER_MINIMAL_HUD_VALIDATED short objectives; on-demand guide/subtitles; menu preserves mission; visible input regions");
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
