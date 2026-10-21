using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class UnderwaterBuild
{
    private const string ScenePath = "Assets/Scenes/UnderwaterJourney.unity";
    private const string BuildPath = "Build/WebGLSimpleKnife";

    [MenuItem("Underwater/Create Benchmark Scene")]
    public static void CreateScene()
    {
        EnsureShaderResources();
        Directory.CreateDirectory("Assets/Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        GameObject root = new("Underwater Journey");
        root.AddComponent<UnderwaterWorld>();
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
        Debug.Log($"UNDERWATER_SCENE_READY {ScenePath}");
    }

    private static void EnsureShaderResources()
    {
        const string materialDirectory = "Assets/Resources/Materials";
        Directory.CreateDirectory(materialDirectory);
        AssetDatabase.Refresh();
        ConfigureTexture("Assets/Resources/Textures/CoastSandRocks02/coast_sand_rocks_02_diff_2k.jpg", true);
        ConfigureTexture("Assets/Resources/Textures/CoastSandRocks02/coast_sand_rocks_02_nor_gl_2k.jpg", false);
        ConfigureTexture("Assets/Resources/Textures/CoastSandRocks02/coast_sand_rocks_02_rough_2k.jpg", false);
        ConfigureTexture("Assets/Resources/Textures/CoastSandRocks02/coast_sand_rocks_02_ao_2k.jpg", false);
        EnsureMaterial($"{materialDirectory}/TransparentGlow.mat", "Underwater/TransparentGlow");
        EnsureMaterial($"{materialDirectory}/UnderwaterSurface.mat", "Underwater/Surface");
        EnsureMaterial($"{materialDirectory}/VolumetricBeam.mat", "Underwater/VolumetricBeam");
        EnsureMaterial($"{materialDirectory}/CausticSeafloor.mat", "Underwater/CausticSeafloor");
        EnsureMaterial($"{materialDirectory}/LayeredWaterBackdrop.mat", "Underwater/LayeredWaterBackdrop");
        EnsureMaterial($"{materialDirectory}/UnderwaterLit.mat", "Universal Render Pipeline/Lit");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ConfigureTexture(string path, bool sRgb)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new BuildFailedException($"Required texture not found: {path}");
        }

        importer.textureType = TextureImporterType.Default;
        importer.sRGBTexture = sRgb;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 4;
        importer.mipmapEnabled = true;
        importer.maxTextureSize = 2048;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void EnsureMaterial(string path, string shaderName)
    {
        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            throw new BuildFailedException($"Required shader not found: {shaderName}");
        }
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing == null)
        {
            AssetDatabase.CreateAsset(new Material(shader), path);
        }
        else if (existing.shader != shader)
        {
            existing.shader = shader;
            EditorUtility.SetDirty(existing);
        }
    }

    [MenuItem("Underwater/Build WebGL Benchmark")]
    public static void BuildWebGL()
    {
        CreateScene();
        Directory.CreateDirectory(BuildPath);

        PlayerSettings.companyName = "Blue Lantern Studio";
        PlayerSettings.productName = "The Blue Between Worlds";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        PlayerSettings.runInBackground = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, new[] { GraphicsDeviceType.OpenGLES3 });
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

        BuildPlayerOptions options = new()
        {
            scenes = new[] { ScenePath },
            locationPathName = BuildPath,
            target = BuildTarget.WebGL,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new BuildFailedException($"WebGL build failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        }
        PatchWebTemplate();
        Debug.Log($"UNDERWATER_WEBGL_READY {Path.GetFullPath(BuildPath)} {report.summary.totalSize} bytes");
    }

    private static void PatchWebTemplate()
    {
        string indexPath = Path.Combine(BuildPath, "index.html");
        string stylePath = Path.Combine(BuildPath, "TemplateData/style.css");
        string index = File.ReadAllText(indexPath);
        index = index.Replace(
            "canvas.style.width = \"960px\";\n        canvas.style.height = \"600px\";",
            "canvas.style.width = \"100vw\";\n        canvas.style.height = \"100vh\";");
        File.WriteAllText(indexPath, index);

        File.AppendAllText(stylePath, @"
html, body { width: 100%; height: 100%; overflow: hidden; background: #062f3d; }
#unity-container.unity-desktop { position: fixed; inset: 0; width: 100vw; height: 100vh; transform: none; }
#unity-canvas { width: 100vw !important; height: 100vh !important; display: block; }
#unity-footer { display: none !important; }
#unity-warning { z-index: 3; }
");
    }
}
