using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class WhaleVisualValidation
{
    private const string Key = "Underwater.WhaleVisual.";
    private const string Output = "Logs/bugfix-20261020/whale-visual/recheck";

    static WhaleVisualValidation()
    {
        EditorApplication.update += Tick;
        EditorApplication.playModeStateChanged += OnPlayMode;
    }

    [MenuItem("Underwater/Capture Whale Visual Inspection And Exit")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Start whale visual inspection from Edit Mode.");
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetInt(Key + "Shot", 0);
        SessionState.SetBool(Key + "Warmed", false);
        SessionState.SetInt(Key + "Result", 1);
        SessionState.SetString(Key + "Deadline", DateTime.UtcNow.AddMinutes(3).Ticks.ToString());
        SessionState.SetString(Key + "Phase", "Entering");
        Directory.CreateDirectory(Output);
        // Open the existing production scene without regenerating or saving assets.
        EditorSceneManager.OpenScene("Assets/Scenes/UnderwaterJourney.unity", OpenSceneMode.Single);
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            SessionState.SetInt(Key + "Frame", Time.frameCount + 30);
            SessionState.SetFloat(Key + "CaptureTime", Time.time + 3f);
            SessionState.SetString(Key + "Phase", "Capturing");
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            int result = SessionState.GetInt(Key + "Result", 1);
            SessionState.SetBool(Key + "Active", false);
            EditorApplication.Exit(result);
        }
    }

    private static void Tick()
    {
        if (!SessionState.GetBool(Key + "Active", false) || !EditorApplication.isPlaying
            || SessionState.GetString(Key + "Phase", "") != "Capturing") return;
        try
        {
            if (DateTime.UtcNow.Ticks > long.Parse(SessionState.GetString(Key + "Deadline", "0")))
                throw new TimeoutException("Timed out capturing the moving whale.");
            HeroWhaleDrift whale = UnityEngine.Object.FindFirstObjectByType<HeroWhaleDrift>();
            Camera camera = Camera.main;
            if (whale == null || camera == null) throw new InvalidOperationException("Production whale/camera is missing.");
            if (!whale.isActiveAndEnabled || !whale.GetComponent<WhaleBodyAnimator>().isActiveAndEnabled)
                throw new InvalidOperationException("Whale movement or body animation is disabled.");
            // Only the camera controller is disabled for this inspection;
            // actual animal movement, materials, lighting and post effects run.
            CinematicDive controller = camera.GetComponent<CinematicDive>();
            if (controller != null) controller.enabled = false;
            Renderer[] renderers = whale.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers) bounds.Encapsulate(renderer.bounds);
            Vector3 offset = (whale.transform.right + whale.transform.forward * 0.25f + Vector3.up * 0.10f).normalized;
            float distance = bounds.extents.magnitude * 1.65f;
            camera.transform.SetPositionAndRotation(bounds.center + offset * distance, Quaternion.LookRotation(-offset, Vector3.up));
            if (Time.frameCount < SessionState.GetInt(Key + "Frame", 0)
                || Time.time < SessionState.GetFloat(Key + "CaptureTime", 0f)) return;

            int index = SessionState.GetInt(Key + "Shot", 0);
            bool warmup = !SessionState.GetBool(Key + "Warmed", false);
            if (index == 1 && !warmup)
            {
                Vector3 previous = JsonUtility.FromJson<Vector3>(SessionState.GetString(Key + "Position", "{}"));
                if (Vector3.Distance(previous, whale.transform.position) < 0.05f)
                    throw new InvalidOperationException("Whale did not move between visual inspection frames.");
            }
            string path = Path.Combine(Output, warmup ? "whale-warmup.png" : $"whale-side-{index + 1:00}.png");
            bool compilingBefore = ShaderUtil.anythingCompiling;
            bool passReadyBefore = ShaderUtil.IsPassCompiled(renderers[0].sharedMaterial, 0);
            Capture(camera, path);
            Transform tail = Array.Find(whale.GetComponentsInChildren<Transform>(), child => child.name == "WhaleTailTip");
            File.WriteAllText(Path.ChangeExtension(path, ".json"), JsonUtility.ToJson(new Shot
            {
                frame = Time.frameCount, time = Time.time, whalePosition = whale.transform.position,
                cameraPosition = camera.transform.position, tailRotation = tail == null ? Quaternion.identity : tail.localRotation,
                fieldOfView = camera.fieldOfView, width = camera.pixelWidth, height = camera.pixelHeight,
                hdr = camera.allowHDR, msaa = camera.allowMSAA,
                applicationURL = Application.absoluteURL,
                warmup = warmup, shadersCompilingBefore = compilingBefore, shadersCompilingAfter = ShaderUtil.anythingCompiling,
                whalePassReadyBefore = passReadyBefore, whalePassReadyAfter = ShaderUtil.IsPassCompiled(renderers[0].sharedMaterial, 0),
                postProcessing = camera.GetUniversalAdditionalCameraData().renderPostProcessing
            }, true));
            Debug.Log($"UNDERWATER_WHALE_VISUAL_FRAME {Path.GetFullPath(path)} frame={Time.frameCount} whale={whale.transform.position}");
            if (warmup)
            {
                SessionState.SetBool(Key + "Warmed", true);
                SessionState.SetInt(Key + "Frame", Time.frameCount + 2);
                SessionState.SetFloat(Key + "CaptureTime", Time.time + 0.3f);
                return;
            }
            if (index == 0)
            {
                SessionState.SetString(Key + "Position", JsonUtility.ToJson(whale.transform.position));
                SessionState.SetInt(Key + "Shot", 1);
                SessionState.SetInt(Key + "Frame", Time.frameCount + 2);
                SessionState.SetFloat(Key + "CaptureTime", Time.time + 0.3f);
            }
            else
            {
                Debug.Log("UNDERWATER_WHALE_VISUAL_CAPTURED two real Play Mode frames; fixed whale-relative side/front-side camera; not manual mouse gameplay");
                SessionState.SetInt(Key + "Result", 0);
                SessionState.SetString(Key + "Phase", "Stopping");
                EditorApplication.isPlaying = false;
            }
        }
        catch (Exception error)
        {
            Debug.LogError("UNDERWATER_WHALE_VISUAL_FAILED " + error);
            SessionState.SetString(Key + "Phase", "Stopping");
            EditorApplication.isPlaying = false;
        }
    }

    private static void Capture(Camera camera, string path)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = new(camera.pixelWidth, camera.pixelHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        UniversalRenderPipelineAsset pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        target.antiAliasing = camera.allowMSAA && pipeline != null ? pipeline.msaaSampleCount : 1;
        Texture2D image = new(target.width, target.height, TextureFormat.RGB24, false);
        RenderTexture resolved = RenderTexture.GetTemporary(target.width, target.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        try
        {
            RenderPipeline.StandardRequest request = new() { destination = target, face = CubemapFace.Unknown };
            if (!RenderPipeline.SupportsRenderRequest(camera, request))
                throw new InvalidOperationException("Active pipeline does not support the production camera render request.");
            RenderPipeline.SubmitRenderRequest(camera, request);
            Graphics.Blit(target, resolved);
            RenderTexture.active = resolved;
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
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
        public string mode = "Editor Play Mode fixed-camera visual inspection; not manual mouse gameplay; production URP camera without IMGUI overlay";
        public string applicationURL;
        public int frame, width, height;
        public float time, fieldOfView;
        public Vector3 whalePosition, cameraPosition;
        public Quaternion tailRotation;
        public bool hdr, msaa, postProcessing;
        public bool warmup, shadersCompilingBefore, shadersCompilingAfter, whalePassReadyBefore, whalePassReadyAfter;
    }
}
