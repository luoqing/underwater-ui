using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class ClearWaterVisualValidation
{
    private const string Key = "Underwater.ClearWaterVisual.";
    private const string Output = "Logs/compact-reef-20261024/visual";
    private static readonly string[] Names = { "garden", "lantern-close", "rescue-net", "net-contact-close", "whale-side", "net-released", "knife-held", "knife-cut-contact", "reef-entry", "reef-mid", "reef-exit" };

    static ClearWaterVisualValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    [MenuItem("Underwater/Capture Clear Water Props And Exit")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start the visual inspection from Edit Mode.");
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetInt(Key + "Shot", 0);
        SessionState.SetInt(Key + "Result", 1);
        SessionState.SetBool(Key + "Prepared", false);
        SessionState.SetBool(Key + "Warmed", false);
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddMinutes(4).Ticks.ToString());
        Directory.CreateDirectory(Output);
        EditorSceneManager.OpenScene("Assets/Scenes/UnderwaterJourney.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    public static void RunKnife()
    {
        Run();
        SessionState.SetInt(Key + "Shot", 6);
    }

    public static void RunReef()
    {
        Run();
        SessionState.SetInt(Key + "Shot", 8);
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(Key + "Frame", Time.frameCount + 30);
            SessionState.SetFloat(Key + "CaptureTime", Time.time + 3f);
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key + "Active", false);
            EditorApplication.Exit(SessionState.GetInt(Key + "Result", 1));
        }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Active", false) || !EditorApplication.isPlaying) return;
        try
        {
            if (DateTime.UtcNow.Ticks > long.Parse(SessionState.GetString(Key + "Deadline", "0")))
                throw new TimeoutException("Timed out capturing production props.");
            if (Time.frameCount < SessionState.GetInt(Key + "Frame", 0)
                || Time.time < SessionState.GetFloat(Key + "CaptureTime", 0f)) return;
            Camera camera = Camera.main;
            OceanMissionDirector director = OceanMissionDirector.Instance;
            if (camera == null || director == null) throw new InvalidOperationException("Production camera/director is missing.");
            CinematicDive controller = camera.GetComponent<CinematicDive>();
            if (controller != null) controller.enabled = false;
            camera.aspect = 1280f / 720f;
            int index = SessionState.GetInt(Key + "Shot", 0);
            if (!SessionState.GetBool(Key + "Prepared", false))
            {
                if (index == 0 || index == 8) Invoke(director, "StartAdventure");
                if (index == 2 || index == 6) Invoke(director, "StartRescue");
                PoseCamera(index, camera, director);
                if (index == 5)
                {
                    Capture(camera, Path.Combine(Output, "net-released-warmup.png"));
                    director.EquipNetCutter();
                    director.TryCutNet(0);
                    FishingLineRescue rescue = Field<FishingLineRescue>(director, "fishingLine");
                    if (!rescue.Fragments[0].IsReleased || rescue.Fragments[0].IsFinished)
                        throw new InvalidOperationException("The actual cut did not start a visible net release animation.");
                    SessionState.SetBool(Key + "Warmed", true);
                }
                SessionState.SetBool(Key + "Prepared", true);
                Delay(2, index == 5 ? 0.16f : 0.3f);
                return;
            }
            // Follow moving production animals, without disabling their animators.
            if (index != 5) PoseCamera(index, camera, director);
            RescueNetFragment released = index == 5 ? Field<FishingLineRescue>(director, "fishingLine").Fragments[0] : null;
            if (index == 5 && (released == null || !released.IsReleased || released.IsFinished
                || released.ReleaseProgress <= 0f || !released.gameObject.activeInHierarchy))
                throw new InvalidOperationException("A live released-net animation frame was missed; do not substitute a finished/hidden mesh.");
            bool warmup = !SessionState.GetBool(Key + "Warmed", false);
            string path = Path.Combine(Output, Names[index] + (warmup ? "-warmup" : "") + ".png");
            Capture(camera, path);
            File.WriteAllText(Path.ChangeExtension(path, ".json"), JsonUtility.ToJson(new Shot
            {
                subject = Names[index], frame = Time.frameCount, time = Time.time,
                cameraPosition = camera.transform.position, cameraRotation = camera.transform.rotation,
                fieldOfView = camera.fieldOfView, warmup = warmup,
                postProcessing = camera.GetUniversalAdditionalCameraData().renderPostProcessing,
                hdr = camera.allowHDR, msaa = camera.allowMSAA,
                fog = RenderSettings.fog, fogColor = RenderSettings.fogColor, fogDensity = RenderSettings.fogDensity,
                releaseProgress = released == null ? 0f : released.ReleaseProgress,
                releasedNet = released != null && released.IsReleased
            }, true));
            if (warmup)
            {
                SessionState.SetBool(Key + "Warmed", true);
                Delay(2, 0.3f);
                return;
            }
            Debug.Log("UNDERWATER_CLEAR_WATER_VISUAL_FRAME " + Path.GetFullPath(path));
            if (++index == Names.Length)
            {
                Debug.Log("UNDERWATER_CLEAR_WATER_VISUAL_PASS production Play Mode renders; diagnostic camera/mission setup, not manual gameplay");
                SessionState.SetInt(Key + "Result", 0);
                EditorApplication.isPlaying = false;
                return;
            }
            SessionState.SetInt(Key + "Shot", index);
            SessionState.SetBool(Key + "Prepared", false);
            SessionState.SetBool(Key + "Warmed", false);
            Delay(2, 0.3f);
        }
        catch (Exception error)
        {
            Debug.LogError("UNDERWATER_CLEAR_WATER_VISUAL_FAILED " + error);
            EditorApplication.isPlaying = false;
        }
    }

    private static void PoseCamera(int index, Camera camera, OceanMissionDirector director)
    {
        camera.fieldOfView = 54f;
        if (index >= 8)
        {
            Vector3[] positions = { new(0f, 4.3f, -7f), new(0f, 4.3f, 25f), new(0f, 2.5f, 58f) };
            Vector3[] targets = { new(0f, 2.3f, 18f), new(0f, 2f, 44f), new(0f, 0f, 80f) };
            Look(camera, positions[index - 8], targets[index - 8]);
            return;
        }
        if (index == 0)
        {
            Look(camera, new Vector3(0f, 4.3f, -7f), new Vector3(0f, 2.3f, 18f));
        }
        else if (index == 1)
        {
            GameObject beacon = Field<List<GameObject>>(director, "adventureBeacons")[0];
            // Stay outside the real 4.2 m completion radius; only the camera lens zooms in.
            camera.fieldOfView = 24f;
            Look(camera, LanternCameraPosition(beacon.transform.position), beacon.transform.position);
            if (!beacon.activeSelf) throw new InvalidOperationException("First production lantern is not active.");
        }
        else if (index == 2 || index == 3 || index == 5 || index == 6 || index == 7)
        {
            GameObject turtle = Field<GameObject>(director, "rescueTurtle");
            FishingLineRescue lines = Field<FishingLineRescue>(director, "fishingLine");
            if (lines == null || !lines.TryGetNextKnotPosition(out Vector3 knot))
                throw new InvalidOperationException("Production net/cut targets are missing.");
            Bounds bounds = RendererBounds(turtle);
            Vector3 outward = (knot - bounds.center).normalized;
            if (index == 2)
                Look(camera, bounds.center + (outward + Vector3.up * 0.25f).normalized * bounds.extents.magnitude * 2.6f, bounds.center);
            else if (index == 5 || index >= 6)
                Look(camera, turtle.transform.position + (outward + Vector3.up * 0.25f).normalized * 3.2f, bounds.center);
            else
            {
                camera.fieldOfView = 32f;
                Look(camera, knot + outward * Mathf.Max(0.9f, bounds.extents.magnitude * 0.48f), knot);
            }
            if (index >= 6)
            {
                if (!director.NetCutterEquipped) director.EquipNetCutter();
                if (director.NetCutter == null) throw new InvalidOperationException("Missing equipped knife.");
                if (index == 6) director.NetCutter.ResetPose();
                else director.NetCutter.PoseAt(knot, new Vector2(30f, 12f));
            }
        }
        else
        {
            HeroWhaleDrift whale = UnityEngine.Object.FindFirstObjectByType<HeroWhaleDrift>();
            if (whale == null || !whale.isActiveAndEnabled || !whale.GetComponent<WhaleBodyAnimator>().isActiveAndEnabled)
                throw new InvalidOperationException("Production whale animation is not active.");
            Bounds bounds = RendererBounds(whale.gameObject);
            Vector3 offset = (whale.transform.right + whale.transform.forward * 0.25f + Vector3.up * 0.10f).normalized;
            Look(camera, bounds.center + offset * bounds.extents.magnitude * 1.65f, bounds.center);
        }
    }

    private static Bounds RendererBounds(GameObject subject)
    {
        Renderer[] renderers = subject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) throw new InvalidOperationException("Missing production renderers.");
        Bounds bounds = default;
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer is LineRenderer || renderer.GetComponentInParent<MissionInteractable>() != null
                || renderer.GetComponentInParent<RescueNetFragment>() != null
                || renderer.GetComponentInParent<RescueNetCutter>() != null) continue;
            if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
            else bounds.Encapsulate(renderer.bounds);
        }
        if (!hasBounds) throw new InvalidOperationException("Missing production body renderers.");
        return bounds;
    }

    private static Vector3 LanternCameraPosition(Vector3 target)
    {
        Physics.SyncTransforms();
        Vector3[] offsets =
        {
            new(0f, 1f, -5.5f), new(-1f, 1f, -5.5f), new(1f, 1f, -5.5f),
            new(0f, 2f, -5.5f), new(-2f, 1.5f, -5.5f), new(2f, 1.5f, -5.5f),
            new(-5.5f, 1f, 0f), new(5.5f, 1f, 0f), new(0f, 2f, 5.5f)
        };
        foreach (Vector3 offset in offsets)
        {
            Vector3 position = target + offset;
            if (offset.magnitude <= 4.2f
                || position.y <= UnderwaterWorld.SampleSeafloorHeight(position.x, position.z) + 0.2f) continue;
            bool blocked = false;
            foreach (Collider collider in Physics.OverlapSphere(position, 0.2f, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                if (collider.GetComponentInParent<UnderwaterObstacle>() != null) blocked = true;
            Vector3 side = Vector3.Cross(offset.normalized, Vector3.up).normalized * 0.48f;
            Vector3[] aimPoints = { target, target + Vector3.up * 0.9f, target - Vector3.up * 0.75f, target + side, target - side };
            foreach (Vector3 aim in aimPoints)
            {
                Vector3 ray = aim - position;
                foreach (RaycastHit hit in Physics.RaycastAll(position, ray.normalized, ray.magnitude, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                    if (hit.collider.GetComponentInParent<UnderwaterObstacle>() != null) blocked = true;
                for (int sample = 1; sample < 24; sample++)
                {
                    Vector3 point = Vector3.Lerp(position, aim, sample / 24f);
                    if (point.y <= UnderwaterWorld.SampleSeafloorHeight(point.x, point.z) + 0.03f) blocked = true;
                }
            }
            if (!blocked) return position;
        }
        throw new InvalidOperationException("No unobstructed lantern camera: center and cage edges must have clear production obstacle/terrain sight lines.");
    }

    private static void Look(Camera camera, Vector3 position, Vector3 target) =>
        camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, Vector3.up));

    private static void Delay(int frames, float seconds)
    {
        SessionState.SetInt(Key + "Frame", Time.frameCount + frames);
        SessionState.SetFloat(Key + "CaptureTime", Time.time + seconds);
    }

    private static T Field<T>(object subject, string name) =>
        (T)subject.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(subject);

    private static void Invoke(object subject, string name) =>
        subject.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(subject, null);

    private static void Capture(Camera camera, string path)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = new(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        target.antiAliasing = camera.allowMSAA && pipeline != null ? pipeline.msaaSampleCount : 1;
        Texture2D image = new(1280, 720, TextureFormat.RGB24, false);
        RenderTexture resolved = RenderTexture.GetTemporary(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        try
        {
            RenderPipeline.StandardRequest request = new() { destination = target, face = CubemapFace.Unknown };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                throw new InvalidOperationException("Production pipeline does not support render requests.");
            RenderPipeline.SubmitRenderRequest(camera, request);
            Graphics.Blit(target, resolved);
            RenderTexture.active = resolved;
            image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(image);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            RenderTexture.ReleaseTemporary(resolved);
        }
    }

    [Serializable]
    private sealed class Shot
    {
        public string mode = "Production URP Play Mode rendering; diagnostic mission/camera setup; not manual gameplay; no IMGUI overlay";
        public string style = "clear-water production camera; fitted three-dimensional gray-green nylon net; original lantern and animal assets";
        public string subject;
        public int width = 1280, height = 720, frame;
        public float time, fieldOfView, fogDensity, releaseProgress;
        public Vector3 cameraPosition;
        public Quaternion cameraRotation;
        public Color fogColor;
        public bool warmup, postProcessing, hdr, msaa, fog, releasedNet;
    }
}
