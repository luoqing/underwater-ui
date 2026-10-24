using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Production Play Mode scene footage with a choreographed camera, not an FPS
// benchmark or a manual walkthrough. No gameplay asset or setting is saved.
[InitializeOnLoad]
public static class ReadmeShowcaseCapture
{
    private const string Key = "Underwater.ReadmeCapture.";
    private const string Output = "Logs/readme-showcase-20261025/frames-final";
    private const int Fps = 12;
    private const int Frames = Fps * 8;
    private static int lastFrame = -1;
    private static Camera captureCamera;
    private static RenderTexture target, resolved;
    private static Texture2D pixels;

    static ReadmeShowcaseCapture()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Start from Edit Mode.");
        Directory.CreateDirectory(Output);
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetInt(Key + "Index", -1);
        SessionState.SetInt(Key + "First", 0);
        SessionState.SetInt(Key + "Result", 1);
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddMinutes(15).Ticks.ToString());
        EditorSceneManager.OpenScene("Assets/Scenes/UnderwaterJourney.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    public static void RunRescue()
    {
        Run();
        SessionState.SetInt(Key + "Index", Fps * 6);
        SessionState.SetInt(Key + "First", Fps * 6);
    }

    private static void OnPlayMode(PlayModeStateChange change)
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        if (change == PlayModeStateChange.EnteredPlayMode) Time.captureFramerate = Fps;
        else if (change == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Key + "Active", false);
            EditorApplication.Exit(SessionState.GetInt(Key + "Result", 1));
        }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Active", false) || !EditorApplication.isPlaying
            || Time.frameCount == lastFrame || Time.time < 2f) return;
        lastFrame = Time.frameCount;
        try
        {
            if (DateTime.UtcNow.Ticks > long.Parse(SessionState.GetString(Key + "Deadline", "0"))) throw new TimeoutException("Scene footage capture timed out.");
            if (captureCamera == null) captureCamera = Camera.main;
            Camera camera = captureCamera;
            camera.GetComponent<CinematicDive>().enabled = false;
            camera.aspect = 1280f / 720f;
            camera.fieldOfView = 54f;
            int index = SessionState.GetInt(Key + "Index", -1);
            int chapter = Mathf.Max(0, index) / (Fps * 2);
            float progress = Mathf.Max(0, index) % (Fps * 2) / (float)(Fps * 2 - 1);
            Vector3 position, look;
            if (chapter == 0)
            {
                position = Vector3.Lerp(new Vector3(0f, 4.3f, -7f), new Vector3(0f, 4.1f, 5f), progress);
                look = new Vector3(0f, 2.4f, 24f);
            }
            else if (chapter == 1)
            {
                Transform fish = GameObject.Find("Hero Blue Tang").transform;
                position = fish.position + Vector3.Cross(fish.forward, Vector3.up).normalized * 5.8f + Vector3.up * 1.2f + fish.forward * 1.6f;
                look = fish.position;
            }
            else if (chapter == 2)
            {
                Transform whale = UnityEngine.Object.FindFirstObjectByType<HeroWhaleDrift>().transform;
                position = whale.position + whale.right * 18f + whale.forward * 4f + Vector3.up * 2f;
                look = whale.position;
            }
            else
            {
                OceanMissionDirector director = OceanMissionDirector.Instance;
                if (index == Fps * 6)
                {
                    camera.enabled = true;
                    typeof(OceanMissionDirector).GetMethod("StartRescue", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(director, null);
                }
                Transform turtle = ((GameObject)typeof(OceanMissionDirector).GetField("rescueTurtle", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director)).transform;
                FishingLineRescue net = turtle.GetComponent<FishingLineRescue>();
                if (!net.TryGetNextKnotPosition(out Vector3 knot)) throw new InvalidOperationException("Missing rescue net target.");
                Renderer[] renderers = turtle.GetComponentsInChildren<Renderer>();
                Bounds bounds = renderers[0].bounds;
                foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
                Vector3 outward = (knot - bounds.center).normalized;
                position = bounds.center + (outward + Vector3.up * 0.25f).normalized * bounds.extents.magnitude * 2.3f;
                look = bounds.center;
            }
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(look - position, Vector3.up));
            // SubmitRenderRequest renders this camera explicitly; avoid also
            // rendering the same scene into the unused editor Game View.
            camera.enabled = false;
            Capture(camera, Path.Combine(Output, index < 0 ? "warmup.jpg" : $"frame-{index:D5}.jpg"));
            if (index >= 0) File.AppendAllText(Path.Combine(Output, "timing.csv"), $"{index},{Time.frameCount},{Time.time:R}\n");
            if (index >= 0 && index % Fps == 0) Debug.Log($"UNDERWATER_README_FRAME {index}/{Frames} simulationTime={Time.time:F3}");
            if (++index == Frames)
            {
                int captured = Frames - SessionState.GetInt(Key + "First", 0);
                Debug.Log($"UNDERWATER_README_CAPTURED {captured} production frames; 1280x720; {Fps} fps simulation capture; {captured / Fps}s; camera choreography; silent; not runtime FPS evidence");
                SessionState.SetInt(Key + "Result", 0);
                Time.captureFramerate = 0;
                Release();
                EditorApplication.isPlaying = false;
            }
            SessionState.SetInt(Key + "Index", index);
        }
        catch (Exception error)
        {
            Debug.LogError("UNDERWATER_README_CAPTURE_FAILED " + error);
            Time.captureFramerate = 0;
            Release();
            EditorApplication.isPlaying = false;
        }
    }

    private static void Capture(Camera camera, string path)
    {
        if (target == null)
        {
            target = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            target.antiAliasing = camera.allowMSAA && pipeline != null ? pipeline.msaaSampleCount : 1;
            resolved = new RenderTexture(1280, 720, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        }
        RenderTexture previous = RenderTexture.active;
        try
        {
            RenderPipeline.StandardRequest request = new() { destination = target, face = CubemapFace.Unknown };
            RenderPipeline.SubmitRenderRequest(camera, request);
            Graphics.Blit(target, resolved);
            RenderTexture.active = resolved;
            pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToJPG(95));
        }
        finally { RenderTexture.active = previous; }
    }

    private static void Release()
    {
        if (captureCamera != null) captureCamera.enabled = true;
        if (target != null) UnityEngine.Object.DestroyImmediate(target);
        if (resolved != null) UnityEngine.Object.DestroyImmediate(resolved);
        if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
    }
}
