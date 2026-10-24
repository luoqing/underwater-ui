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
    private static RescueNetFragment releaseSample;
    private static FishingLineRescue releaseNet;
    private static Vector3 releaseOrigin;

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
            FishSchoolMotionValidation.Run();
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
                    SessionState.SetString(Key + "Phase", "NetReleaseStart");
                }
                WaitForFrames(0.1f);
            }
            else if (phase == "NetReleaseStart")
            {
                Call(director, "StartRescue");
                GameObject turtle = Read<GameObject>(director, "rescueTurtle");
                FishingLineRescue net = Read<FishingLineRescue>(director, "fishingLine");
                PositionNearNetTarget(net, turtle);
                director.EquipNetCutter();
                releaseSample = net.Fragments[0];
                releaseNet = net;
                releaseOrigin = releaseSample.transform.position;
                for (int cut = 0; cut < 3; cut++)
                {
                    PositionNearNetTarget(net, turtle);
                    director.TryCutNet(cut);
                }
                Require(releaseSample.IsReleased && !releaseSample.IsFinished && releaseSample.gameObject.activeInHierarchy,
                    "A cut net disappears instead of beginning its release animation.");
                SessionState.SetString(Key + "Phase", "NetReleaseMoving");
                WaitForFrames(0.12f);
            }
            else if (phase == "NetReleaseMoving")
            {
                Require(releaseSample != null && releaseSample.IsReleased && !releaseSample.IsFinished
                    && releaseSample.ReleaseProgress > 0f && releaseSample.gameObject.activeInHierarchy
                    && Vector3.Distance(releaseOrigin, releaseSample.transform.position) > 0.00001f,
                    "Real Play Mode frames did not visibly detach and drift the cut net.");
                SessionState.SetString(Key + "Phase", "NetReleaseFinished");
                SessionState.SetFloat(Key + "ReleaseFinishTime", Time.time + RescueNetFragment.ReleaseDuration + 0.1f);
                WaitForFrames(RescueNetFragment.ReleaseDuration + 0.1f);
            }
            else if (phase == "NetReleaseFinished")
            {
                if (Time.time < SessionState.GetFloat(Key + "ReleaseFinishTime", 0f)) return;
                Require(releaseSample == null || releaseSample.IsFinished && !releaseSample.gameObject.activeInHierarchy,
                    "A detached net fragment never finished its fade-out.");
                Require(releaseNet != null && releaseNet.Fragments.Count == 0 && releaseNet.SharedNetMaterial == null,
                    "All three finished net patches retain their meshes/material ownership.");
                Call(director, "ResetMission");
                RequireNoActiveRescueProps();
                Debug.Log("UNDERWATER_NET_RELEASE_VALIDATED actual Play Mode Update frames; detached drift before completion; delayed fade; reset clears detached patches and cutter");
                SessionState.SetString(Key + "Phase", "MotionStart");
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
                RequireNoActiveRescueProps();
                Call(director, "ChooseMission", Choice("MissionMode", mission));
                Call(director, "StartAsRole", Choice("PlayerRole", role));
                line = Read<FishingLineRescue>(director, "fishingLine");
            }
            OceanCreatureDrift drift = turtle.GetComponent<OceanCreatureDrift>();
            Require(drift != null && drift.enabled && line != null, "Rescue initialization froze the turtle or omitted the fishing line.");
            RescueRopeValidation.ValidateInstance(line);
            CinematicDive pointerDive = Camera.main.GetComponent<CinematicDive>();
            Vector2 knotPointer = PositionNearNetTarget(line, turtle);
            Require(!(bool)Call(pointerDive, "PointerHitsEncounter", knotPointer, turtle.GetComponent<MarineEncounter>()),
                "An already selected turtle steals presses from its rescue knots.");
            Require(turtle.transform.position.y < UnderwaterWorld.WaterSurfaceHeight - 1f, "Replayed rescue started with the turtle still at the surface.");
            Require(Mathf.Abs(turtle.transform.position.y - (UnderwaterWorld.WaterSurfaceHeight - 6f)) < 0.01f,
                "Rescue starts too deep for the short child-friendly ascent.");
            if (role == "Turtle") AdventureReachabilityValidation.ValidateRescueSightline();
            foreach (RescueNetFragment fragment in line.Fragments)
                ValidateMaterials(fragment.GetComponentsInChildren<Renderer>());
            ValidateNetCutting(director, line, turtle, role == "Turtle");
            Require(Read<int>(director, "stage") == 1 && drift.enabled, "Cutting three net patches did not start the breathing stage.");
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
        RequireNoActiveRescueProps();
        foreach (GameObject beacon in Read<List<GameObject>>(director, "adventureBeacons"))
            Require(!beacon.activeSelf, "Reset left a route lantern active.");
        foreach (MonoBehaviour animal in Animals()) Require(animal.isActiveAndEnabled, "A mission disabled an animal's normal motion.");
    }

    private static void ValidateNetCutting(OceanMissionDirector director, FishingLineRescue net, GameObject turtle, bool testGesture)
    {
        Require(director.IsNetCuttingStage && !director.NetCutterEquipped && director.NetCutter == null,
            "A replay started with an old equipped cutter.");
        List<MissionInteractable> targets = Read<List<MissionInteractable>>(net, "knots");
        director.TryCutNet(0);
        Require(net.NextKnot == 0, "A net was cut without equipping the tool.");
        targets[0].Activate();
        Require(net.NextKnot == 0 && director.NetCutterEquipped && director.NetCutter != null,
            "A short target click cuts the net instead of only equipping the cutter.");
        RescueNetCutter cutter = director.NetCutter;
        KnifeModelValidation.Run(cutter);
        ValidateMaterials(cutter.GetComponentsInChildren<Renderer>());
        foreach (Collider collider in cutter.GetComponentsInChildren<Collider>())
            Require(!collider.enabled, "The held cutter blocks animal/net raycasts.");
        CinematicDive dive = Camera.main.GetComponent<CinematicDive>();
        dive.StartMissionAt(turtle.transform.position + Vector3.back * 9f, turtle.transform.position);
        director.TryCutNet(0);
        Require(net.NextKnot == 0, "The child can cut the net from beyond 4.5 m.");
        Vector3 farPointer = Camera.main.WorldToScreenPoint(targets[0].transform.position);
        Require(!director.IsNetWithinReach && !(bool)Call(dive, "TryBeginNetCut", new Vector2(farPointer.x, farPointer.y)),
            "The cutter starts a gesture while the animal is outside the director's reach gate.");
        PositionNearNetTarget(net, turtle);
        director.TryCutNet(2);
        Require(net.NextKnot == 0 && director.IsCurrentNetTarget(targets[0]) && !director.IsCurrentNetTarget(targets[2]),
            "An out-of-order net target incorrectly advanced the rescue.");
        Call(director, "OpenMenu");
        director.TryCutNet(0);
        Require(net.NextKnot == 0, "An open mission menu lets cutting through.");
        typeof(OceanMissionDirector).GetField("showMenu", PrivateInstance).SetValue(director, false);
        if (testGesture)
        {
            Require(net.TryGetNextKnotPosition(out Vector3 farSideTarget), "The reach-boundary cut target is missing.");
            Vector3 nearSide = (farSideTarget - turtle.transform.position).normalized;
            dive.StartMissionAt(turtle.transform.position - nearSide * 4.4f, farSideTarget);
            Physics.SyncTransforms();
            Vector3 projected = Camera.main.WorldToScreenPoint(farSideTarget);
            Require(director.IsNetWithinReach && Vector3.Distance(Camera.main.transform.position, farSideTarget) > 4.5f,
                "The boundary case does not actually put the animal within reach and its far-side strand beyond 4.5 m.");
            Require((bool)Call(dive, "TryBeginNetCut", new Vector2(projected.x, projected.y)),
                "The animal is within reach but its far-side strand cannot begin a stroke.");
            Call(dive, "CancelNetCut");
            Vector2 pointer = PositionNearNetTarget(net, turtle);
            Require((bool)Call(dive, "TryBeginNetCut", pointer), "A real raycast on the current near net cannot begin a cut.");
            Call(dive, "UpdateNetCut", pointer + Vector2.right * 21f, false);
            Require(net.NextKnot == 0, "A stroke shorter than 22 pixels cut the net.");
            Call(dive, "UpdateNetCut", pointer + Vector2.right * 80f, true);
            Require(net.NextKnot == 0, "Mouse-up incorrectly completes an unperformed stroke.");
            Require((bool)Call(dive, "TryBeginNetCut", pointer), "Releasing a short stroke did not allow a new hold.");
            Call(dive, "UpdateNetCut", pointer, false);
            Require(net.NextKnot == 0, "A zero-length hold cut the net.");
            Call(dive, "UpdateNetCut", pointer + Vector2.right * 22f, false);
            Require(net.NextKnot == 1, "A valid 22-pixel stroke did not cut exactly one patch.");
            Call(dive, "UpdateNetCut", pointer + Vector2.right * 180f, false);
            Require(net.NextKnot == 1, "One held stroke cut multiple net patches.");
            Call(dive, "UpdateNetCut", pointer + Vector2.right * 180f, true);
            pointer = PositionNearNetTarget(net, turtle);
            Require((bool)Call(dive, "TryBeginNetCut", pointer), "The next ordered patch could not start after release.");
            Call(dive, "CancelNetCut");
            Call(dive, "UpdateNetCut", pointer + Vector2.right * 100f, false);
            Require(net.NextKnot == 1, "A canceled stroke still cuts the next patch.");
            Require(!(bool)Call(dive, "TryBeginNetCut", new Vector2(-100f, -100f)), "Empty off-screen space starts a net stroke.");
            Rect arrow = UnderwaterHud.GetTravelControlRects()[0];
            Vector2 arrowPointer = new(arrow.center.x, Screen.height - arrow.center.y);
            Require(!(bool)Call(dive, "TryBeginNetCut", arrowPointer), "A movement arrow starts a net stroke.");
            Debug.Log("UNDERWATER_NET_POINTER_GESTURE_VALIDATED production raycasts/private input algorithm; short/zero/release/cancel/UI/empty strokes do not cut; 22px cuts once; release required for next hold; not browser manual gameplay");
        }
        for (int target = net.NextKnot; target < net.KnotCount; target++)
        {
            Vector2 pointer = PositionNearNetTarget(net, turtle);
            if (testGesture && target == net.KnotCount - 1)
            {
                Require((bool)Call(dive, "TryBeginNetCut", pointer), "The final net stroke could not begin.");
                Call(dive, "UpdateNetCut", pointer + Vector2.right * 22f, false);
                Call(dive, "UpdateNetCut", pointer + Vector2.right * 80f, false);
                Require(Read<bool>(dive, "cuttingNet"), "The final cut stopped consuming its held press after the mission changed stage.");
                Call(dive, "UpdateNetCut", pointer + Vector2.right * 80f, true);
                Require(!Read<bool>(dive, "cuttingNet") && float.IsPositiveInfinity(Read<float>(dive, "pointerTravel")),
                    "The final net release can click through into animal selection.");
            }
            else director.TryCutNet(target);
            Require(net.NextKnot == target + 1, "An equipped in-range ordered cut did not release its patch.");
        }
        foreach (RescueNetFragment fragment in net.Fragments)
            Require(fragment.IsReleased && !fragment.IsFinished && fragment.gameObject.activeInHierarchy
                && !fragment.transform.IsChildOf(turtle.transform), "A cut net disappears instantly or remains rigidly attached to the turtle.");
        Require(!director.IsNetCuttingStage && !director.NetCutterEquipped && !cutter.gameObject.activeSelf,
            "The breathing stage retains the equipped cutter or cutting target.");
    }

    private static Vector2 PositionNearNetTarget(FishingLineRescue net, GameObject turtle)
    {
        Require(net.TryGetNextKnotPosition(out Vector3 target), "The next ordered cut target is missing.");
        Vector3 direction = (target - turtle.transform.position).normalized;
        if (direction.sqrMagnitude < 0.1f) direction = Vector3.back;
        CinematicDive dive = Camera.main.GetComponent<CinematicDive>();
        dive.StartMissionAt(turtle.transform.position + direction * 3.2f, target);
        Physics.SyncTransforms();
        Require(Vector3.Distance(Camera.main.transform.position, turtle.transform.position) <= 4.5f,
            "The net test camera is outside the real cutting range.");
        Vector3 pointer = Camera.main.WorldToScreenPoint(target);
        Require(pointer.z > 0f, "The cut target is behind the test camera.");
        return new Vector2(pointer.x, pointer.y);
    }

    private static void RequireNoActiveRescueProps()
    {
        foreach (RescueNetFragment fragment in UnityEngine.Object.FindObjectsByType<RescueNetFragment>(FindObjectsSortMode.None))
            Require(!fragment.gameObject.activeInHierarchy, "Replay/reset left an old intact or detached net fragment active.");
        foreach (RescueNetCutter cutter in UnityEngine.Object.FindObjectsByType<RescueNetCutter>(FindObjectsSortMode.None))
            Require(!cutter.IsEquipped && !cutter.gameObject.activeInHierarchy, "Replay/reset left an old cutter equipped or visible.");
    }

    private static IEnumerable<MonoBehaviour> Animals()
    {
        foreach (MonoBehaviour value in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            if (value is ReefFishDrift || value is OceanCreatureDrift || value is HeroWhaleDrift) yield return value;
    }

    private static void ValidateMinimalHud(OceanMissionDirector director, UnderwaterHud hud)
    {
        Require(Mathf.Abs(Read<Texture2D>(director, "panelTexture").GetPixel(0, 0).a - 0.58f) < 0.004f
            && Mathf.Abs(Read<Texture2D>(director, "buttonTexture").GetPixel(0, 0).a - 0.42f) < 0.004f
            && Mathf.Abs(Read<Texture2D>(director, "buttonHoverTexture").GetPixel(0, 0).a - 0.62f) < 0.004f,
            "Mission menu transparency no longer matches the glass-panel preview.");
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
