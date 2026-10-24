using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class UnderwaterWorld : MonoBehaviour
{
    public const float WaterSurfaceHeight = 48f;
    private readonly List<Material> runtimeMaterials = new();
    private Camera journeyCamera;
    private Light sunlight;
    private Volume postVolume;
    private Mesh schoolingFishBodyMesh;
    private Mesh schoolingFishTailMesh;
    private Mesh schoolingFishFinMesh;
    private Mesh schoolingFishEyeMesh;
    private Color shallowWater = new(0.025f, 0.34f, 0.48f);
    private Color midWater = new(0.012f, 0.18f, 0.30f);
    private Color deepWater = new(0.008f, 0.075f, 0.15f);
    private Color shallowSky = new(0.30f, 0.58f, 0.62f);
    private Color midSky = new(0.08f, 0.24f, 0.34f);
    private Color shallowEquator = new(0.11f, 0.31f, 0.34f);
    private Color deepEquator = new(0.025f, 0.11f, 0.18f);
    private Color backgroundDeep = new(0.035f, 0.14f, 0.20f);
    private Color shallowSun = new(0.78f, 0.95f, 1f);
    private Color midSun = new(0.48f, 0.78f, 0.90f);
    private Color deepSun = new(0.25f, 0.48f, 0.72f);
    private float shallowFogDensity = 0.0105f;
    private float midFogDensity = 0.0175f;
    private float deepFogDensity = 0.027f;
    private float shallowSunIntensity = 2.35f;
    private float midSunIntensity = 1.35f;
    private float deepSunIntensity = 0.58f;
    private float postExposure = 0.78f;
    private float postSaturation = 18f;
    private float beamAlpha = 0.030f;
    private float beamIntensity = 0.94f;
    private float causticStrength = 0.15f;
    private Color surfaceShallow = new(0.10f, 0.56f, 0.68f, 0.12f);
    private Color surfaceDeep = new(0.018f, 0.13f, 0.23f, 0.20f);
    private bool useLayeredFog;
    private float shallowFogStart = 10f;
    private float midFogStart = 7f;
    private float deepFogStart = 3f;
    private float shallowFogEnd = 125f;
    private float midFogEnd = 88f;
    private float deepFogEnd = 54f;
    private Color layeredTop = new(0.10f, 0.52f, 0.63f);
    private Color layeredHorizon = new(0.025f, 0.30f, 0.45f);
    private Color layeredBottom = new(0.006f, 0.07f, 0.14f);
    private bool clearWaterLook;

    private void Awake()
    {
        Random.InitState(73021);
        ConfigureWaterLook(Application.absoluteURL);
        BuildWorld();
    }

    private void ConfigureWaterLook(string url)
    {
        if (url.Contains("water=layered"))
        {
            useLayeredFog = true;
            shallowWater = new Color(0.040f, 0.42f, 0.56f);
            midWater = new Color(0.014f, 0.205f, 0.34f);
            deepWater = new Color(0.006f, 0.07f, 0.15f);
            shallowSky = new Color(0.36f, 0.67f, 0.69f);
            midSky = new Color(0.085f, 0.25f, 0.36f);
            shallowEquator = new Color(0.13f, 0.36f, 0.39f);
            deepEquator = new Color(0.025f, 0.105f, 0.19f);
            backgroundDeep = new Color(0.018f, 0.095f, 0.17f);
            shallowSun = new Color(0.84f, 0.98f, 1f);
            midSun = new Color(0.50f, 0.80f, 0.92f);
            deepSun = new Color(0.24f, 0.47f, 0.72f);
            shallowSunIntensity = 2.58f;
            midSunIntensity = 1.40f;
            deepSunIntensity = 0.58f;
            postExposure = 0.86f;
            postSaturation = 12f;
            beamAlpha = 0.036f;
            beamIntensity = 1.02f;
            causticStrength = 0.16f;
            surfaceShallow = new Color(0.12f, 0.63f, 0.73f, 0.12f);
            surfaceDeep = new Color(0.018f, 0.13f, 0.24f, 0.20f);
        }
        else if (url.Contains("water=bright"))
        {
            shallowWater = new Color(0.09f, 0.58f, 0.70f);
            midWater = new Color(0.04f, 0.35f, 0.46f);
            deepWater = new Color(0.016f, 0.15f, 0.25f);
            shallowSky = new Color(0.44f, 0.76f, 0.76f);
            midSky = new Color(0.15f, 0.38f, 0.47f);
            shallowEquator = new Color(0.18f, 0.46f, 0.46f);
            deepEquator = new Color(0.05f, 0.18f, 0.27f);
            backgroundDeep = new Color(0.06f, 0.23f, 0.29f);
            shallowSun = new Color(0.86f, 0.98f, 1f);
            midSun = new Color(0.58f, 0.86f, 0.94f);
            deepSun = new Color(0.32f, 0.57f, 0.78f);
            shallowFogDensity = 0.0075f;
            midFogDensity = 0.0125f;
            deepFogDensity = 0.020f;
            shallowSunIntensity = 2.85f;
            midSunIntensity = 1.68f;
            deepSunIntensity = 0.78f;
            postExposure = 1.08f;
            postSaturation = 6f;
            beamAlpha = 0.034f;
            beamIntensity = 1.00f;
            causticStrength = 0.18f;
            surfaceShallow = new Color(0.16f, 0.72f, 0.80f, 0.12f);
            surfaceDeep = new Color(0.03f, 0.21f, 0.32f, 0.20f);
        }
        else if (url.Contains("water=balanced"))
        {
            shallowWater = new Color(0.045f, 0.44f, 0.58f);
            midWater = new Color(0.020f, 0.26f, 0.38f);
            deepWater = new Color(0.010f, 0.105f, 0.19f);
            shallowSky = new Color(0.36f, 0.66f, 0.68f);
            midSky = new Color(0.11f, 0.30f, 0.40f);
            shallowEquator = new Color(0.14f, 0.37f, 0.39f);
            deepEquator = new Color(0.032f, 0.13f, 0.21f);
            backgroundDeep = new Color(0.04f, 0.165f, 0.23f);
            shallowSun = new Color(0.82f, 0.97f, 1f);
            midSun = new Color(0.53f, 0.82f, 0.92f);
            deepSun = new Color(0.28f, 0.52f, 0.75f);
            shallowFogDensity = 0.0096f;
            midFogDensity = 0.0158f;
            deepFogDensity = 0.024f;
            shallowSunIntensity = 2.52f;
            midSunIntensity = 1.48f;
            deepSunIntensity = 0.65f;
            postExposure = 0.88f;
            postSaturation = 14f;
            beamAlpha = 0.033f;
            beamIntensity = 0.98f;
            causticStrength = 0.16f;
            surfaceShallow = new Color(0.11f, 0.60f, 0.70f, 0.12f);
            surfaceDeep = new Color(0.022f, 0.15f, 0.25f, 0.20f);
        }
        else if (url.Contains("water=cinematic"))
        {
            shallowWater = new Color(0.018f, 0.27f, 0.42f);
            midWater = new Color(0.009f, 0.14f, 0.25f);
            deepWater = new Color(0.005f, 0.05f, 0.11f);
            shallowSky = new Color(0.24f, 0.49f, 0.56f);
            midSky = new Color(0.055f, 0.18f, 0.29f);
            shallowEquator = new Color(0.075f, 0.25f, 0.30f);
            deepEquator = new Color(0.018f, 0.075f, 0.14f);
            backgroundDeep = new Color(0.02f, 0.10f, 0.17f);
            shallowSun = new Color(0.70f, 0.91f, 1f);
            midSun = new Color(0.40f, 0.69f, 0.86f);
            deepSun = new Color(0.19f, 0.38f, 0.65f);
            shallowFogDensity = 0.012f;
            midFogDensity = 0.020f;
            deepFogDensity = 0.032f;
            shallowSunIntensity = 2.08f;
            midSunIntensity = 1.12f;
            deepSunIntensity = 0.48f;
            postExposure = 0.55f;
            postSaturation = 20f;
            beamAlpha = 0.040f;
            beamIntensity = 1.04f;
            causticStrength = 0.12f;
            surfaceShallow = new Color(0.075f, 0.47f, 0.61f, 0.12f);
            surfaceDeep = new Color(0.012f, 0.095f, 0.19f, 0.20f);
        }

        // This isolated build defaults to the new reference look. The original
        // layered-water treatment remains available for same-build comparison.
        clearWaterLook = !url.Contains("look=original");
        if (!clearWaterLook) return;
        useLayeredFog = true;
        shallowWater = new Color(0.16f, 0.49f, 0.59f);
        midWater = new Color(0.065f, 0.30f, 0.42f);
        deepWater = new Color(0.024f, 0.14f, 0.24f);
        shallowSky = new Color(0.31f, 0.54f, 0.58f);
        midSky = new Color(0.15f, 0.34f, 0.41f);
        shallowEquator = new Color(0.16f, 0.32f, 0.33f);
        deepEquator = new Color(0.07f, 0.16f, 0.22f);
        backgroundDeep = new Color(0.035f, 0.17f, 0.26f);
        shallowSun = new Color(0.96f, 0.98f, 0.94f);
        midSun = new Color(0.70f, 0.86f, 0.91f);
        deepSun = new Color(0.43f, 0.65f, 0.79f);
        shallowSunIntensity = 2.42f;
        midSunIntensity = 1.50f;
        deepSunIntensity = 0.80f;
        shallowFogStart = 16f;
        midFogStart = 10f;
        deepFogStart = 6f;
        shallowFogEnd = 145f;
        midFogEnd = 110f;
        deepFogEnd = 76f;
        layeredTop = new Color(0.24f, 0.62f, 0.69f);
        layeredHorizon = new Color(0.105f, 0.39f, 0.51f);
        layeredBottom = new Color(0.025f, 0.15f, 0.24f);
        postExposure = 0.72f;
        postSaturation = 2f;
        beamAlpha = 0.026f;
        beamIntensity = 0.94f;
        causticStrength = 0.19f;
        surfaceShallow = new Color(0.24f, 0.68f, 0.74f, 0.10f);
        surfaceDeep = new Color(0.055f, 0.23f, 0.32f, 0.16f);
    }

    private void Update()
    {
        if (journeyCamera == null)
        {
            return;
        }

        float verticalDepth = Mathf.InverseLerp(5f, -16f, journeyCamera.transform.position.y);
        float routeMid = Mathf.InverseLerp(38f, 142f, journeyCamera.transform.position.z);
        float routeDeep = Mathf.InverseLerp(132f, 204f, journeyCamera.transform.position.z);
        float midBlend = Mathf.Clamp01(Mathf.Max(verticalDepth * 0.72f, routeMid));
        float deepBlend = Mathf.Clamp01(Mathf.Max(Mathf.InverseLerp(0.58f, 1f, verticalDepth), routeDeep));
        Color atmosphere = Color.Lerp(Color.Lerp(shallowWater, midWater, midBlend), deepWater, deepBlend);
        RenderSettings.fogColor = atmosphere;
        if (useLayeredFog)
        {
            RenderSettings.fogStartDistance = Mathf.Lerp(Mathf.Lerp(shallowFogStart, midFogStart, midBlend), deepFogStart, deepBlend);
            RenderSettings.fogEndDistance = Mathf.Lerp(Mathf.Lerp(shallowFogEnd, midFogEnd, midBlend), deepFogEnd, deepBlend);
        }
        else
        {
            RenderSettings.fogDensity = Mathf.Lerp(Mathf.Lerp(shallowFogDensity, midFogDensity, midBlend), deepFogDensity, deepBlend);
        }
        RenderSettings.ambientSkyColor = Color.Lerp(shallowSky, midSky, midBlend);
        RenderSettings.ambientEquatorColor = Color.Lerp(shallowEquator, deepEquator, deepBlend);
        journeyCamera.backgroundColor = Color.Lerp(atmosphere, backgroundDeep, 0.12f + deepBlend * 0.18f);
        if (sunlight != null)
        {
            sunlight.intensity = Mathf.Lerp(Mathf.Lerp(shallowSunIntensity, midSunIntensity, midBlend), deepSunIntensity, deepBlend);
            sunlight.color = Color.Lerp(Color.Lerp(shallowSun, midSun, midBlend), deepSun, deepBlend);
        }
    }

    private void OnDestroy()
    {
        foreach (Material material in runtimeMaterials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }

    private void BuildWorld()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = shallowSky;
        RenderSettings.ambientEquatorColor = shallowEquator;
        RenderSettings.ambientGroundColor = clearWaterLook ? new Color(0.10f, 0.17f, 0.17f) : new Color(0.055f, 0.17f, 0.17f);
        RenderSettings.reflectionIntensity = 0.72f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = useLayeredFog ? FogMode.Linear : FogMode.ExponentialSquared;
        RenderSettings.fogColor = shallowWater;
        RenderSettings.fogDensity = shallowFogDensity;
        RenderSettings.fogStartDistance = shallowFogStart;
        RenderSettings.fogEndDistance = shallowFogEnd;

        CreateLighting();
        CreateCamera();
        if (useLayeredFog) CreateLayeredBackdrop();
        CreatePostProcessing();
        CreateWaterSurface();
        CreateSeafloor();
        CreateLightShafts();
        CreateShallowReef();
        CreateKelpGarden();
        CreateOpenOcean();
        CreateFishSchools();
        CreateDeepSea();
        CreateRuins();
        CreateAtmosphericParticles();
        gameObject.AddComponent<UnderwaterHud>();
        gameObject.AddComponent<UnderwaterAudio>();
        gameObject.AddComponent<OceanMissionDirector>();
    }

    private void CreateLighting()
    {
        GameObject sun = new("Filtered Sunlight");
        sun.transform.SetParent(transform);
        sun.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        sunlight = sun.AddComponent<Light>();
        sunlight.type = LightType.Directional;
        sunlight.color = shallowSun;
        sunlight.intensity = shallowSunIntensity;
        sunlight.shadows = LightShadows.Soft;
        sunlight.shadowStrength = clearWaterLook ? 0.72f : 0.64f;
        sunlight.shadowBias = 0.04f;
        sunlight.shadowNormalBias = 0.4f;
        sunlight.GetUniversalAdditionalLightData().usePipelineSettings = false;

        CreatePointLight("Shallow water bounce", new Vector3(-4f, 5f, 18f), new Color(0.70f, 0.90f, 0.90f), 1.35f, 34f);
        CreatePointLight("Warm reef bounce", new Vector3(6f, 2f, 29f), new Color(0.92f, 0.76f, 0.58f), 0.82f, 24f);
        CreatePointLight("Reef bounce", new Vector3(8f, 1f, 54f), new Color(0.42f, 0.70f, 0.72f), 0.95f, 30f);
        CreatePointLight("Deep water bounce", new Vector3(-2f, -6f, 146f), new Color(0.16f, 0.36f, 0.62f), 0.58f, 36f);
        CreateSunPool("Sun pool near reef", new Vector3(-8f, 34f, 18f), Quaternion.Euler(92f, -18f, 0f), 2.8f, 72f, 32f);
        CreateSunPool("Sun pool over channel", new Vector3(10f, 36f, 58f), Quaternion.Euler(96f, 14f, 0f), 2.2f, 78f, 38f);
    }

    private void CreatePointLight(string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new(name);
        go.transform.SetParent(transform);
        go.transform.position = position;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity * (clearWaterLook ? 0.65f : 1f);
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private void CreateSunPool(string name, Vector3 position, Quaternion rotation, float intensity, float range, float spotAngle)
    {
        GameObject go = new(name);
        go.transform.SetParent(transform);
        go.transform.position = position;
        go.transform.rotation = rotation;
        Light light = go.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(0.62f, 0.88f, 0.96f);
        light.intensity = intensity;
        light.range = range;
        light.spotAngle = spotAngle;
        light.innerSpotAngle = spotAngle * 0.48f;
        light.shadows = LightShadows.None;
        if (clearWaterLook)
        {
            Vector3 floor = new(position.x, SampleSeafloorHeight(position.x, position.z), position.z);
            Vector3 direction = sunlight.transform.forward;
            go.transform.position = floor - direction * ((position.y - floor.y) / -direction.y);
            go.transform.rotation = sunlight.transform.rotation;
            light.color = new Color(0.81f, 0.92f, 0.94f);
        }
    }

    private void CreateCamera()
    {
        GameObject cameraObject = new("Journey Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(transform);
        journeyCamera = cameraObject.AddComponent<Camera>();
        journeyCamera.fieldOfView = 58f;
        journeyCamera.nearClipPlane = 0.08f;
        journeyCamera.farClipPlane = 260f;
        journeyCamera.clearFlags = CameraClearFlags.SolidColor;
        journeyCamera.backgroundColor = shallowWater;
        journeyCamera.allowHDR = true;
        journeyCamera.allowMSAA = true;
        cameraObject.AddComponent<AudioListener>();

        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.requiresColorTexture = true;
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;

        CinematicDive dive = cameraObject.AddComponent<CinematicDive>();
        dive.duration = 68f;
        dive.path = new[]
        {
            new Vector3(0f, 4.6f, -12f),
            new Vector3(-1.4f, 4.2f, 1f),
            new Vector3(2.2f, 4f, 16f),
            new Vector3(-2.4f, 3.8f, 32f),
            new Vector3(1.6f, 4.1f, 49f),
            new Vector3(4f, 4.4f, 70f),
            new Vector3(1f, -1f, 117f),
            new Vector3(-4f, -4f, 139f),
            new Vector3(3f, -3.6f, 164f),
            new Vector3(0f, 0.5f, 190f),
            new Vector3(-2f, 7.2f, 215f)
        };
    }

    private void CreatePostProcessing()
    {
        GameObject volumeObject = new("Underwater Grade");
        volumeObject.transform.SetParent(transform);
        postVolume = volumeObject.AddComponent<Volume>();
        postVolume.isGlobal = true;
        postVolume.priority = 10f;
        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        postVolume.profile = profile;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(clearWaterLook ? 0.18f : 0.30f);
        bloom.threshold.Override(0.95f);
        bloom.scatter.Override(0.62f);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(postExposure);
        color.contrast.Override(6f);
        color.saturation.Override(postSaturation);
        color.colorFilter.Override(clearWaterLook ? new Color(0.99f, 1f, 0.99f) : new Color(0.96f, 1f, 1f));

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color(0.01f, 0.05f, 0.08f));
        vignette.intensity.Override(clearWaterLook ? 0.05f : 0.08f);
        vignette.smoothness.Override(0.78f);

        WhiteBalance whiteBalance = profile.Add<WhiteBalance>(true);
        whiteBalance.temperature.Override(-2f);
        whiteBalance.tint.Override(-1f);

        LensDistortion lens = profile.Add<LensDistortion>(true);
        lens.intensity.Override(-0.035f);
        lens.scale.Override(0.985f);

        ChromaticAberration chromatic = profile.Add<ChromaticAberration>(true);
        chromatic.intensity.Override(clearWaterLook ? 0.006f : 0.018f);

        FilmGrain grain = profile.Add<FilmGrain>(true);
        grain.intensity.Override(clearWaterLook ? 0.02f : 0.055f);
        grain.response.Override(0.72f);

        DepthOfField depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(11f);
        depthOfField.gaussianEnd.Override(42f);
        depthOfField.gaussianMaxRadius.Override(0.48f);
        depthOfField.highQualitySampling.Override(false);
        depthOfField.active = !clearWaterLook;
    }

    private void CreateLayeredBackdrop()
    {
        Material template = Resources.Load<Material>("Materials/LayeredWaterBackdrop");
        Shader shader = template != null ? template.shader : Shader.Find("Underwater/LayeredWaterBackdrop");
        if (shader == null)
        {
            return;
        }

        Material material = template != null ? new Material(template) : new Material(shader);
        material.name = "Layered water backdrop";
        material.SetColor("_TopColor", layeredTop);
        material.SetColor("_HorizonColor", layeredHorizon);
        material.SetColor("_BottomColor", layeredBottom);
        RenderSettings.skybox = material;
        journeyCamera.clearFlags = CameraClearFlags.Skybox;
        runtimeMaterials.Add(material);
    }

    private void CreateWaterSurface()
    {
        Material surface = MakeWaterSurfaceMaterial();
        GameObject plane = new("Moving Water Surface");
        plane.name = "Moving Water Surface";
        plane.transform.SetParent(transform);
        plane.transform.position = new Vector3(0f, WaterSurfaceHeight, 0f);
        plane.AddComponent<MeshFilter>().sharedMesh = CreateSurfaceMesh(720f, 1200f, 48, 120);
        plane.AddComponent<MeshRenderer>().sharedMaterial = surface;
        plane.AddComponent<SurfaceDrift>();
        plane.AddComponent<FollowCameraXZ>().Initialize(journeyCamera.transform, WaterSurfaceHeight);
    }

    private static Mesh CreateSurfaceMesh(float width, float length, int xSegments, int zSegments)
    {
        Vector3[] vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * zSegments * 6];
        for (int z = 0; z <= zSegments; z++)
        {
            float z01 = z / (float)zSegments;
            for (int x = 0; x <= xSegments; x++)
            {
                float x01 = x / (float)xSegments;
                int vertex = z * (xSegments + 1) + x;
                vertices[vertex] = new Vector3((x01 - 0.5f) * width, 0f, (z01 - 0.5f) * length);
                uvs[vertex] = new Vector2(x01, z01);
            }
        }
        int index = 0;
        for (int z = 0; z < zSegments; z++)
        {
            for (int x = 0; x < xSegments; x++)
            {
                int a = z * (xSegments + 1) + x;
                int b = a + 1;
                int c = a + xSegments + 1;
                int d = c + 1;
                triangles[index++] = a;
                triangles[index++] = c;
                triangles[index++] = b;
                triangles[index++] = b;
                triangles[index++] = c;
                triangles[index++] = d;
            }
        }
        Mesh mesh = new() { name = "Subdivided Water Surface", vertices = vertices, uv = uvs, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateSeafloor()
    {
        GameObject floor = new("Stable Sculpted Seafloor");
        floor.transform.SetParent(transform);
        floor.AddComponent<InfiniteSeafloorGrid>().Initialize(journeyCamera.transform, MakeCausticMaterial());
    }

    public static float SampleSeafloorHeight(float worldX, float worldZ)
    {
        // Use the same two triangles as the fixed 2 metre seafloor mesh.
        const float spacing = InfiniteSeafloorGrid.VertexSpacing;
        float x = Mathf.Floor(worldX / spacing) * spacing;
        float z = Mathf.Floor(worldZ / spacing) * spacing;
        float u = (worldX - x) / spacing;
        float v = (worldZ - z) / spacing;
        if (u == 0f && v == 0f)
        {
            return SampleTerrainHeight(x, z);
        }
        float b = SampleTerrainHeight(x + spacing, z);
        float c = SampleTerrainHeight(x, z + spacing);
        if (u + v <= 1f)
        {
            float a = SampleTerrainHeight(x, z);
            return a + (b - a) * u + (c - a) * v;
        }
        float d = SampleTerrainHeight(x + spacing, z + spacing);
        return d + (c - d) * (1f - u) + (b - d) * (1f - v);
    }

    private static float SampleTerrainHeight(float worldX, float worldZ)
    {
        float routeDepth = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(75f, 150f, worldZ));
        float channelCenter = Mathf.Sin(worldZ * 0.034f) * 2.2f + Mathf.Sin(worldZ * 0.012f + 1.7f) * 1.3f;
        float lateral = worldX - channelCenter;
        float channelWidth = 8.2f + Mathf.Sin(worldZ * 0.045f + 0.8f) * 1.6f;
        float corridorBlend = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.5f, 16f, Mathf.Abs(lateral)));
        float broad = (Mathf.PerlinNoise(worldX * 0.028f + 4.2f, worldZ * 0.025f + 9.1f) - 0.5f) * 5.2f
            * Mathf.Lerp(0.32f, 1f, corridorBlend);
        float detail = (Mathf.PerlinNoise(worldX * 0.13f + 17f, worldZ * 0.12f + 2f) - 0.5f) * 1.2f
            * Mathf.Lerp(0.25f, 1f, corridorBlend);
        float banks = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(channelWidth, 34f, Mathf.Abs(lateral))) * 9.8f;
        float rockyRidges = Mathf.Pow(Mathf.PerlinNoise(worldX * 0.074f + 21f, worldZ * 0.066f + 31f), 3f)
            * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(5f, 24f, Mathf.Abs(lateral))) * 3.2f;
        float sandRipples = Mathf.Sin(worldZ * 1.45f + Mathf.Sin(worldX * 0.43f) * 1.3f) * 0.14f;
        float outcrops = EllipticalMound(worldX, worldZ, -12f, 58f, 5f, 10f, 2.2f)
            + EllipticalMound(worldX, worldZ, 12f, 71f, 4.8f, 9f, 2f)
            + EllipticalMound(worldX, worldZ, -12.5f, 86f, 5.5f, 11f, 1.8f);
        float trench = -routeDepth * 9f;
        return broad + detail + banks + rockyRidges + sandRipples + outcrops - 3.3f + trench;
    }

    public static Vector3 SampleSeafloorNormal(float worldX, float worldZ)
    {
        const float step = 0.35f;
        float left = SampleTerrainHeight(worldX - step, worldZ);
        float right = SampleTerrainHeight(worldX + step, worldZ);
        float back = SampleTerrainHeight(worldX, worldZ - step);
        float forward = SampleTerrainHeight(worldX, worldZ + step);
        return new Vector3(left - right, step * 2f, back - forward).normalized;
    }

    private static float EllipticalMound(float x, float z, float centerX, float centerZ, float radiusX, float radiusZ, float height)
    {
        float nx = (x - centerX) / radiusX;
        float nz = (z - centerZ) / radiusZ;
        float falloff = Mathf.Clamp01(1f - nx * nx - nz * nz);
        falloff = falloff * falloff * (3f - 2f * falloff);
        return falloff * height;
    }

    private void CreateLightShafts()
    {
        Material shaftMaterial = MakeBeamMaterial();
        Mesh beamMesh = CreateCrossBeamMesh();
        Vector3[] shaftPositions =
        {
            new(-22f, 12f, -6f), new(-11f, 13f, 3f), new(2f, 12f, 12f), new(17f, 14f, 22f),
            new(-17f, 13f, 36f), new(-3f, 12f, 49f), new(13f, 13f, 61f), new(24f, 14f, 76f),
            new(-12f, 12f, 90f), new(8f, 13f, 106f)
        };
        for (int i = 0; i < shaftPositions.Length; i++)
        {
            GameObject shaft = new("Volumetric Sun Ray");
            shaft.name = "Volumetric Sun Ray";
            shaft.transform.SetParent(transform);
            shaft.transform.position = shaftPositions[i];
            shaft.transform.rotation = Quaternion.Euler(Random.Range(-1.0f, 1.0f), -28f + Random.Range(-8f, 8f), Random.Range(5f, 11f));
            shaft.transform.localScale = new Vector3(Random.Range(8f, 14f), Random.Range(72f, 98f), Random.Range(8f, 14f));
            if (clearWaterLook)
            {
                float azimuth = shaft.transform.eulerAngles.y;
                shaft.transform.rotation = Quaternion.FromToRotation(Vector3.up, -sunlight.transform.forward) * Quaternion.Euler(0f, azimuth, 0f);
                Vector3 scale = shaft.transform.localScale;
                shaft.transform.localScale = new Vector3(scale.x * 0.52f, scale.y, scale.z * 0.52f);
            }
            shaft.AddComponent<MeshFilter>().sharedMesh = beamMesh;
            shaft.AddComponent<MeshRenderer>().sharedMaterial = shaftMaterial;
            shaft.AddComponent<SoftPulse>().speed = Random.Range(0.15f, 0.35f);
        }
    }

    private static Mesh CreateCrossBeamMesh()
    {
        Vector3[] vertices =
        {
            new(-0.5f, -0.5f, 0f), new(0.5f, -0.5f, 0f), new(0.18f, 0.5f, 0f), new(-0.18f, 0.5f, 0f),
            new(0f, -0.5f, -0.5f), new(0f, -0.5f, 0.5f), new(0f, 0.5f, 0.18f), new(0f, 0.5f, -0.18f)
        };
        Vector2[] uvs =
        {
            new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f),
            new(0f, 0f), new(1f, 0f), new(1f, 1f), new(0f, 1f)
        };
        int[] triangles = { 0, 2, 1, 0, 3, 2, 4, 6, 5, 4, 7, 6 };
        Mesh mesh = new() { name = "Crossed Volumetric Beam", vertices = vertices, uv = uvs, triangles = triangles };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void CreateShallowReef()
    {
        SpawnResourceModel("Models/Reef", "Left foreground reef", new Vector3(-8.5f, -2.9f, -1f), Quaternion.Euler(0f, 26f, -4f), 27f, CreateFallbackReef);
        SpawnResourceModel("Models/ReefShelf", "Right foreground reef shelf", new Vector3(7.3f, -3.1f, 5f), Quaternion.Euler(0f, 218f, 1f), 15f, null, new Color(1.03f, 0.97f, 0.94f));
        SpawnResourceModel("Models/Reef", "Right reef wall", new Vector3(10f, -3.7f, 22f), Quaternion.Euler(0f, 202f, 2f), 30f, CreateFallbackReef);
        SpawnResourceModel("Models/ReefShelf", "Left coral garden", new Vector3(-7.4f, -3.4f, 25f), Quaternion.Euler(0f, 34f, -2f), 14f, null, new Color(0.96f, 1.02f, 1.03f));
        SpawnResourceModel("Models/Reef", "Left mid reef wall", new Vector3(-10.5f, -4.2f, 45f), Quaternion.Euler(0f, 34f, -3f), 29f, CreateFallbackReef);
        SpawnResourceModel("Models/ReefShelf", "Right coral garden", new Vector3(8.4f, -4.2f, 47f), Quaternion.Euler(0f, 194f, 2f), 13.5f, null, new Color(1.02f, 0.94f, 0.91f));
        SpawnResourceModel("Models/CoralCluster", "Warm red coral accent", new Vector3(4.8f, -2.5f, 12f), Quaternion.Euler(0f, 148f, 0f), 8.5f, null, new Color(1.18f, 0.48f, 0.38f));
        SpawnResourceModel("Models/CoralCluster", "Violet coral accent", new Vector3(-5.4f, -2.9f, 20f), Quaternion.Euler(0f, 42f, 1f), 7.5f, null, new Color(0.72f, 0.55f, 1.16f));
        SpawnResourceModel("Models/CoralCluster", "Golden coral accent", new Vector3(5.8f, -3.4f, 35f), Quaternion.Euler(0f, 220f, -1f), 8f, null, new Color(1.18f, 0.92f, 0.48f));
        SpawnResourceModel("Models/ReefShelf", "Near pink reef shelf", new Vector3(-6.2f, -3.4f, 7f), Quaternion.Euler(0f, 12f, -2f), 11f, null, new Color(1.06f, 0.88f, 0.91f));
        SpawnResourceModel("Models/ReefShelf", "Mid channel reef shelf", new Vector3(-9.8f, -3.7f, 59f), Quaternion.Euler(0f, 58f, -2f), 7.5f, null, new Color(0.98f, 1.02f, 1.00f));
        SpawnResourceModel("Models/ReefShelf", "Far channel reef shelf", new Vector3(10.2f, -4f, 72f), Quaternion.Euler(0f, 226f, 1f), 7.2f, null, new Color(1.03f, 0.97f, 0.92f));
        SpawnResourceModel("Models/CoralCluster", "Blue coral accent", new Vector3(7.8f, -3.6f, 73f), Quaternion.Euler(0f, 78f, 0f), 4.2f, null, new Color(0.55f, 0.71f, 0.97f));
        SpawnResourceModel("Models/CoralCluster", "Rust orange coral accent", new Vector3(-7.5f, -3.8f, 91f), Quaternion.Euler(0f, 186f, -1f), 4.4f, null, new Color(1.04f, 0.63f, 0.40f));
    }

    private void CreateKelpGarden()
    {
        Vector3[] positions =
        {
            new(-10f, -3.2f, 43f), new(9f, -3.6f, 50f), new(-14f, -4.1f, 61f), new(13f, -4.4f, 68f)
        };
        for (int i = 0; i < positions.Length; i++)
        {
            Color kelpTint = i % 2 == 0 ? new Color(0.48f, 0.66f, 0.42f) : new Color(0.42f, 0.60f, 0.50f);
            GameObject kelp = SpawnResourceModel("Models/KelpCluster", $"Kelp Cluster {i + 1}", positions[i], Quaternion.Euler(0f, i * 83f + 18f, 0f), Random.Range(7f, 10f), null, kelpTint);
            if (kelp != null)
            {
                SwayingPlant sway = kelp.AddComponent<SwayingPlant>();
                sway.speed = Random.Range(0.22f, 0.42f);
                sway.amount = Random.Range(1.2f, 2.8f);
                sway.phase = Random.value * 6.28f;
            }
        }
    }

    private void CreateFishSchools()
    {
        Color[] fishColors =
        {
            new Color(0.46f, 0.64f, 0.68f),
            new Color(0.09f, 0.31f, 0.62f),
            new Color(0.78f, 0.55f, 0.12f),
            new Color(0.82f, 0.27f, 0.13f),
            new Color(0.12f, 0.52f, 0.48f),
            new Color(0.14f, 0.20f, 0.26f)
        };

        Material[] bodyMaterials = new Material[fishColors.Length];
        Material[] finMaterials = new Material[fishColors.Length];
        Material[] deepBodyMaterials = new Material[fishColors.Length];
        Material[] deepFinMaterials = new Material[fishColors.Length];
        for (int i = 0; i < fishColors.Length; i++)
        {
            Color bodyColor = fishColors[i];
            Color finColor = Color.Lerp(bodyColor, new Color(0.035f, 0.08f, 0.12f), 0.34f);
            Color dorsalColor = Color.Lerp(bodyColor, new Color(0.018f, 0.075f, 0.12f), 0.58f);
            Color bellyColor = Color.Lerp(bodyColor, new Color(0.78f, 0.90f, 0.88f), 0.66f);
            Color headColor = Color.Lerp(bodyColor, new Color(0.66f, 0.86f, 0.88f), 0.38f);
            bodyMaterials[i] = MakeSchoolFishGradient($"School fish body {i}", dorsalColor, bodyColor, bellyColor, headColor, 0.62f);
            finMaterials[i] = MakeLit($"School fish fins {i}", finColor, 0.01f, 0.48f);
            deepBodyMaterials[i] = MakeSchoolFishGradient(
                $"Deep school fish body {i}",
                dorsalColor * 0.46f,
                bodyColor * 0.54f,
                bellyColor * 0.58f,
                headColor * 0.60f,
                0.56f);
            deepFinMaterials[i] = MakeLit($"Deep school fish fins {i}", finColor * 0.45f, 0f, 0.46f, bodyColor * 0.18f);
            bodyMaterials[i].enableInstancing = true;
            finMaterials[i].enableInstancing = true;
            deepBodyMaterials[i].enableInstancing = true;
            deepFinMaterials[i].enableInstancing = true;
        }

        Material eyeMaterial = MakeLit("School fish eyes", new Color(0.008f, 0.012f, 0.015f), 0f, 0.86f);
        eyeMaterial.enableInstancing = true;

        // Compact shallow schools frame the r5 reef; later groups keep the route alive.
        // All schools reuse the same four meshes and existing material palette.
        CreateFishSchool(new Vector3(-1f, 2.2f, 16f), 80, 5f, bodyMaterials, finMaterials, eyeMaterial, 0.20f);
        CreateFishSchool(new Vector3(-2f, 2.5f, 31f), 78, 6f, bodyMaterials, finMaterials, eyeMaterial, 0.19f);
        CreateFishSchool(new Vector3(3f, 1.8f, 49f), 72, 6.5f, bodyMaterials, finMaterials, eyeMaterial, 0.19f);
        CreateFishSchool(new Vector3(-3f, 2.2f, 72f), 48, 5.5f, bodyMaterials, finMaterials, eyeMaterial, 0.18f);
        CreateFishSchool(new Vector3(4f, -0.5f, 99f), 28, 5.5f, bodyMaterials, finMaterials, eyeMaterial, 0.16f);
        CreateFishSchool(new Vector3(-3f, -5.8f, 126f), 24, 5.2f, deepBodyMaterials, deepFinMaterials, eyeMaterial, 0.14f);
        CreateFishSchool(new Vector3(2.5f, -7.4f, 150f), 20, 5f, deepBodyMaterials, deepFinMaterials, eyeMaterial, 0.14f);
    }

    private void CreateFishSchool(Vector3 center, int count, float radius, Material[] bodyMaterials, Material[] finMaterials, Material eyeMaterial, float scale)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new(Random.Range(-radius, radius), Random.Range(-radius * 0.30f, radius * 0.30f), Random.Range(-radius, radius));
            int palette = i % bodyMaterials.Length;
            GameObject fish = CreateFish(
                center + offset,
                scale * Random.Range(0.82f, 1.18f),
                bodyMaterials[palette],
                finMaterials[palette],
                eyeMaterial,
                i % 3);
            FishSwimmer swimmer = fish.AddComponent<FishSwimmer>();
            swimmer.schoolCenter = center;
            swimmer.formationOffset = offset;
            swimmer.radius = radius;
            swimmer.speed = Random.Range(1.1f, 2.5f);
            swimmer.phase = Random.value * 6.28f;
        }
    }

    private GameObject CreateFish(Vector3 position, float scale, Material bodyMaterial, Material finMaterial, Material eyeMaterial, int profile)
    {
        GameObject root = new("Swimming fish");
        root.transform.SetParent(transform);
        root.transform.position = position;
        root.transform.localScale = Vector3.one * scale;

        GameObject body = new("Streamlined body");
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = profile switch
        {
            0 => new Vector3(1.06f, 0.78f, 0.86f),
            1 => new Vector3(0.98f, 1.02f, 0.94f),
            _ => new Vector3(1.02f, 0.88f, 0.86f)
        };
        body.AddComponent<MeshFilter>().sharedMesh = GetSchoolingFishBodyMesh();
        body.AddComponent<MeshRenderer>().sharedMaterial = bodyMaterial;

        GameObject tail = new("Tail");
        tail.transform.SetParent(root.transform, false);
        tail.transform.localPosition = new Vector3(-0.78f, 0f, 0f);
        tail.AddComponent<MeshFilter>().sharedMesh = GetSchoolingFishTailMesh();
        tail.AddComponent<MeshRenderer>().sharedMaterial = finMaterial;

        GameObject fins = new("Dorsal and pectoral fins");
        fins.transform.SetParent(root.transform, false);
        fins.AddComponent<MeshFilter>().sharedMesh = GetSchoolingFishFinMesh();
        fins.AddComponent<MeshRenderer>().sharedMaterial = finMaterial;

        GameObject eyes = new("Eyes");
        eyes.transform.SetParent(root.transform, false);
        eyes.AddComponent<MeshFilter>().sharedMesh = GetSchoolingFishEyeMesh();
        eyes.AddComponent<MeshRenderer>().sharedMaterial = eyeMaterial;
        return root;
    }

    private Mesh GetSchoolingFishBodyMesh()
    {
        if (schoolingFishBodyMesh != null)
        {
            return schoolingFishBodyMesh;
        }

        const int sides = 12;
        float[] x = { -0.78f, -0.60f, -0.28f, 0.10f, 0.42f, 0.68f, 0.82f };
        float[] height = { 0.11f, 0.34f, 0.47f, 0.50f, 0.43f, 0.28f, 0.08f };
        float[] width = { 0.09f, 0.23f, 0.31f, 0.33f, 0.30f, 0.21f, 0.07f };
        Vector3[] vertices = new Vector3[x.Length * sides + 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int tailTip = x.Length * sides;
        int noseTip = tailTip + 1;

        for (int ring = 0; ring < x.Length; ring++)
        {
            float longitudinal = ring / (float)(x.Length - 1);
            for (int side = 0; side < sides; side++)
            {
                float angle = side / (float)sides * Mathf.PI * 2f;
                int index = ring * sides + side;
                float vertical = Mathf.Cos(angle);
                vertices[index] = new Vector3(
                    x[ring],
                    vertical * height[ring] - 0.025f * (1f - longitudinal),
                    Mathf.Sin(angle) * width[ring]);
                uvs[index] = new Vector2(longitudinal, side / (float)sides);
            }
        }

        vertices[tailTip] = new Vector3(-0.83f, -0.015f, 0f);
        vertices[noseTip] = new Vector3(0.88f, -0.005f, 0f);
        uvs[tailTip] = new Vector2(0f, 0.5f);
        uvs[noseTip] = new Vector2(1f, 0.5f);

        int[] triangles = new int[(x.Length - 1) * sides * 6 + sides * 6];
        int triangle = 0;
        for (int ring = 0; ring < x.Length - 1; ring++)
        {
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                int a = ring * sides + side;
                int b = ring * sides + next;
                int c = (ring + 1) * sides + side;
                int d = (ring + 1) * sides + next;
                triangles[triangle++] = a;
                triangles[triangle++] = b;
                triangles[triangle++] = c;
                triangles[triangle++] = b;
                triangles[triangle++] = d;
                triangles[triangle++] = c;
            }
        }

        for (int side = 0; side < sides; side++)
        {
            int next = (side + 1) % sides;
            triangles[triangle++] = tailTip;
            triangles[triangle++] = side;
            triangles[triangle++] = next;
            int finalRing = (x.Length - 1) * sides;
            triangles[triangle++] = noseTip;
            triangles[triangle++] = finalRing + next;
            triangles[triangle++] = finalRing + side;
        }

        schoolingFishBodyMesh = new Mesh
        {
            name = "School fish streamlined body",
            vertices = vertices,
            uv = uvs,
            triangles = triangles
        };
        schoolingFishBodyMesh.RecalculateNormals();
        schoolingFishBodyMesh.RecalculateTangents();
        schoolingFishBodyMesh.RecalculateBounds();
        return schoolingFishBodyMesh;
    }

    private Mesh GetSchoolingFishTailMesh()
    {
        if (schoolingFishTailMesh != null)
        {
            return schoolingFishTailMesh;
        }

        schoolingFishTailMesh = new Mesh
        {
            name = "School fish articulated tail",
            vertices = new[]
            {
                new Vector3(0f, 0f, 0.035f), new Vector3(-0.72f, 0.58f, 0.035f), new Vector3(-0.55f, 0f, 0.035f), new Vector3(-0.72f, -0.58f, 0.035f),
                new Vector3(0f, 0f, -0.035f), new Vector3(-0.72f, 0.58f, -0.035f), new Vector3(-0.55f, 0f, -0.035f), new Vector3(-0.72f, -0.58f, -0.035f)
            },
            triangles = new[]
            {
                0, 1, 2, 0, 2, 3, 4, 6, 5, 4, 7, 6,
                0, 4, 5, 0, 5, 1, 1, 5, 6, 1, 6, 2,
                2, 6, 7, 2, 7, 3, 3, 7, 4, 3, 4, 0
            }
        };
        schoolingFishTailMesh.RecalculateNormals();
        schoolingFishTailMesh.RecalculateBounds();
        return schoolingFishTailMesh;
    }

    private Mesh GetSchoolingFishFinMesh()
    {
        if (schoolingFishFinMesh != null)
        {
            return schoolingFishFinMesh;
        }

        schoolingFishFinMesh = new Mesh
        {
            name = "School fish dorsal and pectoral fins",
            vertices = new[]
            {
                new Vector3(0.28f, 0.29f, 0f), new Vector3(-0.52f, 0.28f, 0f), new Vector3(-0.28f, 0.76f, 0f),
                new Vector3(0.28f, 0.29f, 0f), new Vector3(-0.28f, 0.76f, 0f), new Vector3(-0.52f, 0.28f, 0f),
                new Vector3(0.34f, -0.04f, 0.18f), new Vector3(-0.20f, -0.15f, 0.18f), new Vector3(-0.30f, -0.35f, 0.62f),
                new Vector3(0.34f, -0.04f, 0.18f), new Vector3(-0.30f, -0.35f, 0.62f), new Vector3(-0.20f, -0.15f, 0.18f),
                new Vector3(0.34f, -0.04f, -0.18f), new Vector3(-0.30f, -0.35f, -0.62f), new Vector3(-0.20f, -0.15f, -0.18f),
                new Vector3(0.34f, -0.04f, -0.18f), new Vector3(-0.20f, -0.15f, -0.18f), new Vector3(-0.30f, -0.35f, -0.62f)
            },
            triangles = new[]
            {
                0, 1, 2, 3, 4, 5,
                6, 7, 8, 9, 10, 11,
                12, 13, 14, 15, 16, 17
            }
        };
        schoolingFishFinMesh.RecalculateNormals();
        schoolingFishFinMesh.RecalculateBounds();
        return schoolingFishFinMesh;
    }

    private Mesh GetSchoolingFishEyeMesh()
    {
        if (schoolingFishEyeMesh != null)
        {
            return schoolingFishEyeMesh;
        }

        const int segments = 8;
        Vector3[] vertices = new Vector3[(segments + 1) * 2];
        int[] triangles = new int[segments * 6];
        for (int side = 0; side < 2; side++)
        {
            float z = side == 0 ? 0.225f : -0.225f;
            int vertexOffset = side * (segments + 1);
            int triangleOffset = side * segments * 3;
            vertices[vertexOffset] = new Vector3(0.56f, 0.10f, z);
            for (int i = 0; i < segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;
                vertices[vertexOffset + i + 1] = new Vector3(0.56f + Mathf.Cos(angle) * 0.075f, 0.10f + Mathf.Sin(angle) * 0.075f, z);
                int next = (i + 1) % segments;
                if (side == 0)
                {
                    triangles[triangleOffset + i * 3] = vertexOffset;
                    triangles[triangleOffset + i * 3 + 1] = vertexOffset + i + 1;
                    triangles[triangleOffset + i * 3 + 2] = vertexOffset + next + 1;
                }
                else
                {
                    triangles[triangleOffset + i * 3] = vertexOffset;
                    triangles[triangleOffset + i * 3 + 1] = vertexOffset + next + 1;
                    triangles[triangleOffset + i * 3 + 2] = vertexOffset + i + 1;
                }
            }
        }

        schoolingFishEyeMesh = new Mesh { name = "School fish eyes", vertices = vertices, triangles = triangles };
        schoolingFishEyeMesh.RecalculateNormals();
        schoolingFishEyeMesh.RecalculateBounds();
        return schoolingFishEyeMesh;
    }

    private void CreateOpenOcean()
    {
        SpawnReefFish("Models/BlueTang", "Hero Blue Tang", new Vector3(6.2f, 4.2f, 10.5f), Quaternion.Euler(-2f, 0f, 0f), 3.1f, 0.2f, 0.88f);
        SpawnReefFish("Models/BlueTang", "Blue Tang companion 1", new Vector3(-4.2f, 3.4f, 14.5f), Quaternion.Euler(-4f, 178f, 0f), 2.1f, 1.7f, 0.72f);
        SpawnReefFish("Models/BlueTang", "Blue Tang companion 2", new Vector3(4.4f, 2.4f, 24f), Quaternion.Euler(2f, 8f, 0f), 1.7f, 3.1f, 0.58f);
        SpawnReefFish("Models/BlueTang", "Blue Tang companion 3", new Vector3(-3.6f, 0.8f, 31f), Quaternion.Euler(-3f, 184f, 0f), 1.45f, 4.8f, 0.48f);
        GameObject clownfish = SpawnReefFish("Models/Clownfish", "Clownfish close pass", new Vector3(-3.4f, 0.6f, 8.5f), Quaternion.Euler(0f, 12f, -2f), 2.35f, 2.4f, 0.68f);
        SpawnReefFish("Models/Clownfish", "Clownfish garden 1", new Vector3(3.6f, 0.4f, 18f), Quaternion.Euler(0f, 188f, 3f), 1.55f, 4.2f, 0.48f);
        SpawnReefFish("Models/Clownfish", "Clownfish garden 2", new Vector3(-4.8f, 0.1f, 27f), Quaternion.Euler(0f, 8f, -1f), 1.25f, 5.6f, 0.40f);
        GameObject turtle = SpawnResourceModel("Models/Turtle", "Green Sea Turtle", new Vector3(-6.2f, 1.8f, 20f), Quaternion.Euler(0f, 68f, 0f), 4.8f, null);
        GameObject manta = SpawnResourceModel("Models/Manta", "Oceanic Manta Ray", new Vector3(-8f, 7.2f, 47f), Quaternion.Euler(0f, 98f, 0f), 8.8f, null);
        GameObject turtleCompanion = SpawnResourceModel("Models/Turtle", "Green Sea Turtle Companion", new Vector3(8.5f, 2.7f, 43f), Quaternion.Euler(0f, 214f, 0f), 3.7f, null);
        GameObject mantaCompanion = SpawnResourceModel("Models/Manta", "Oceanic Manta Ray Companion", new Vector3(9f, 5.8f, 76f), Quaternion.Euler(0f, 238f, 0f), 6.5f, null);
        SpawnReefFish("Models/BlueTang", "Blue Tang companion 4", new Vector3(6.8f, 1.4f, 46f), Quaternion.Euler(1f, 22f, 0f), 1.35f, 6.2f, 0.44f);
        SpawnReefFish("Models/Clownfish", "Clownfish garden 3", new Vector3(-5.6f, 0.6f, 55f), Quaternion.Euler(-1f, 170f, 2f), 1.15f, 7.1f, 0.38f);
        AddEncounter(clownfish, "clownfish", "小丑鱼", "喜欢躲在海葵触手之间的珊瑚礁居民。");
        AddEncounter(turtle, "turtle", "绿海龟", "性情温和的远洋旅行者，能够穿越整片海域。");
        AddEncounter(manta, "manta", "远洋魔鬼鱼", "展开宽大的胸鳍，借着水流平稳滑翔。");
        AddEncounter(turtleCompanion, "turtle", "绿海龟", "性情温和的远洋旅行者，能够穿越整片海域。");
        AddEncounter(mantaCompanion, "manta", "远洋魔鬼鱼", "展开宽大的胸鳍，借着水流平稳滑翔。");
        GameObject whale = SpawnResourceModel("Models/WhaleRigged", "Tripo Humpback Whale", new Vector3(20f, 5.5f, 100f), Quaternion.identity, 14f, CreateFallbackWhale);
        AddEncounter(whale, "whale", "座头鲸", "用尾鳍上下摆动推进的远洋巨兽。");
    }

    private static void AddEncounter(GameObject target, string id, string displayName, string description)
    {
        if (target == null)
        {
            return;
        }

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 scale = target.transform.lossyScale;
        SphereCollider collider = target.AddComponent<SphereCollider>();
        collider.center = target.transform.InverseTransformPoint(bounds.center);
        float worldRadius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z) * 0.76f;
        float largestScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        collider.radius = worldRadius / Mathf.Max(largestScale, 0.0001f);
        collider.isTrigger = true;

        MarineEncounter encounter = target.AddComponent<MarineEncounter>();
        encounter.id = id;
        encounter.displayName = displayName;
        encounter.description = description;
    }

    private GameObject SpawnReefFish(string resourcePath, string name, Vector3 position, Quaternion rotation, float targetSize, float phase, float swimScale)
    {
        GameObject fish = SpawnResourceModel(resourcePath, name, position, rotation, targetSize, null);
        if (fish != null)
        {
            ReefFishDrift drift = fish.GetComponent<ReefFishDrift>();
            if (drift != null)
            {
                drift.phase = phase;
                drift.swimScale = swimScale;
                drift.isBlueTang = resourcePath.EndsWith("BlueTang");
                drift.collisionRadius = Mathf.Clamp(targetSize * 0.23f, 0.22f, 0.78f);
                drift.floorClearance = Mathf.Clamp(targetSize * 0.22f, 0.34f, 0.82f);
                drift.noseDistance = Mathf.Clamp(targetSize * 0.42f, 0.48f, 1.65f);
            }
            if (resourcePath.EndsWith("BlueTang"))
            {
                BlueTangSwimAnimator animator = fish.AddComponent<BlueTangSwimAnimator>();
                animator.phase = phase;
            }
        }
        return fish;
    }

    private void CreateDeepSea()
    {
        Vector3[] positions =
        {
            new(-1.5f, -4.5f, 125f), new(6f, -6f, 135f), new(-5f, -8f, 143f), new(8f, -6.5f, 151f), new(-8f, -7.5f, 160f)
        };
        float[] sizes = { 4.4f, 3.5f, 2.8f, 3.2f, 3.8f };
        Color[] jellyTints =
        {
            new(0.82f, 0.94f, 1.05f), new(0.96f, 0.78f, 0.94f), new(0.78f, 0.98f, 1.02f),
            new(0.88f, 0.82f, 1.04f), new(0.80f, 0.94f, 1.03f)
        };
        for (int i = 0; i < positions.Length; i++)
        {
            Color jellyGlow = jellyTints[i] * 0.055f;
            GameObject jelly = SpawnResourceModel("Models/Jellyfish", $"Photoreal Moon Jellyfish {i + 1}", positions[i], Quaternion.Euler(0f, i * 71f + 16f, i % 2 == 0 ? -3f : 4f), sizes[i], null, jellyTints[i], jellyGlow);
            if (jelly != null)
            {
                JellyDrift drift = jelly.AddComponent<JellyDrift>();
                drift.phase = i * 1.17f;
                drift.uprightBias = i == 1 || i == 4 ? 0.58f : 0.90f;
                JellyPulseAnimator pulse = jelly.AddComponent<JellyPulseAnimator>();
                pulse.phase = i * 0.16f;
            }
        }
    }

    private void CreateJellyfish(Vector3 position, float scale, Material material, float phase)
    {
        GameObject root = new("Bioluminescent jellyfish");
        root.transform.SetParent(transform);
        root.transform.position = position;
        root.transform.localScale = Vector3.one * scale;

        GameObject bell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bell.transform.SetParent(root.transform, false);
        bell.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        bell.transform.localScale = new Vector3(1.1f, 0.55f, 1.1f);
        bell.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(bell.GetComponent<Collider>());

        for (int t = 0; t < 6; t++)
        {
            GameObject tentacle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tentacle.transform.SetParent(root.transform, false);
            float angle = t / 6f * Mathf.PI * 2f;
            tentacle.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.48f, -0.8f, Mathf.Sin(angle) * 0.48f);
            tentacle.transform.localScale = new Vector3(0.035f, Random.Range(0.55f, 1.1f), 0.035f);
            tentacle.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(tentacle.GetComponent<Collider>());
        }

        JellyDrift drift = root.AddComponent<JellyDrift>();
        drift.phase = phase;
    }

    private void CreateRuins()
    {
        SpawnResourceModel("Models/ReefRockArch", "Natural Coral Reef Arch", new Vector3(-2f, -10.6f, 168f), Quaternion.Euler(0f, 8f, 0f), 28f, CreateFallbackRuins);
    }

    private void CreateAtmosphericParticles()
    {
        Material moteMaterial = MakeGlow("Suspended plankton", new Color(0.62f, 0.90f, 0.92f, 0.20f), 0.46f, 0.11f, 0.08f);
        CreateParticleVolume("Near suspended plankton", journeyCamera.transform, new Vector3(0f, 0f, 8f), new Vector3(34f, 20f, 52f), 175f, 16f, 0.022f, 0.09f, moteMaterial, new Vector3(-0.035f, 0.018f, -0.09f));

        Material distantMoteMaterial = MakeGlow("Distant marine snow", new Color(0.45f, 0.72f, 0.75f, 0.12f), 0.30f, 0.08f, 0.04f);
        CreateParticleVolume("Distant marine snow", journeyCamera.transform, new Vector3(0f, 0f, 26f), new Vector3(68f, 30f, 105f), 90f, 28f, 0.025f, 0.12f, distantMoteMaterial, new Vector3(0.018f, -0.01f, -0.045f));

        Material bubbleMaterial = MakeGlow("Bubbles", new Color(0.70f, 0.94f, 1f, 0.28f), 0.72f, 0.16f, 0.06f);
        CreateParticleVolume("Reef bubbles", transform, new Vector3(-9f, -2f, 27f), new Vector3(8f, 3f, 14f), 9f, 9f, 0.025f, 0.105f, bubbleMaterial, new Vector3(0f, 0.66f, 0f));
        CreateParticleVolume("Ruin bubbles", transform, new Vector3(8f, -10f, 170f), new Vector3(7f, 3f, 10f), 7f, 12f, 0.03f, 0.12f, bubbleMaterial, new Vector3(0f, 0.55f, 0f));
    }

    private void CreateParticleVolume(string name, Transform parent, Vector3 localPosition, Vector3 box, float rate, float lifetime, float minSize, float maxSize, Material material, Vector3 velocity)
    {
        GameObject go = new(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 2400;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;
        ParticleSystem.VelocityOverLifetimeModule velocityModule = particles.velocityOverLifetime;
        velocityModule.enabled = true;
        velocityModule.space = ParticleSystemSimulationSpace.World;
        velocityModule.x = velocity.x;
        velocityModule.y = velocity.y;
        velocityModule.z = velocity.z;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient fade = new();
        fade.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(new Color(0.82f, 0.96f, 1f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.72f, 0.12f),
                new GradientAlphaKey(0.58f, 0.82f),
                new GradientAlphaKey(0f, 1f)
            });
        colorOverLifetime.color = fade;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.16f;
        noise.frequency = 0.18f;
        noise.scrollSpeed = 0.045f;

        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = material;
    }

    private GameObject SpawnResourceModel(string resourcePath, string objectName, Vector3 position, Quaternion rotation, float targetSize, System.Action fallback, Color? tint = null, Color? emission = null)
    {
        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            fallback?.Invoke();
            return null;
        }

        GameObject instance = Instantiate(prefab, position, rotation, transform);
        instance.name = objectName;
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return instance;
        }

        if (clearWaterLook && (resourcePath.Contains("Reef") || resourcePath.Contains("Coral")))
            tint = (tint ?? Color.white) * new Color(1.04f, 0.99f, 0.91f);
        if (tint.HasValue)
        {
            TintModelMaterials(renderers, tint.Value, emission ?? Color.black);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largest > 0.001f)
        {
            instance.transform.localScale *= targetSize / largest;
            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            if (ShouldPlaceOnSeafloor(resourcePath))
            {
                float groundHeight = SamplePlacementHeight(position.x, position.z, bounds.extents.x, bounds.extents.z);
                float shallowEmbed = resourcePath.EndsWith("KelpCluster") ? 0.08f : resourcePath.Contains("Coral") ? 0.12f : 0.22f;
                instance.transform.position += new Vector3(
                    position.x - bounds.center.x,
                    groundHeight - shallowEmbed - bounds.min.y,
                    position.z - bounds.center.z);
            }
            else
            {
                instance.transform.position += position - bounds.center;
            }

            bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
            if (ShouldBlockLargeSwimmers(resourcePath))
            {
                CreateMeshObstacleColliders(instance);
            }
            else if (ShouldBlockCreatureBody(resourcePath))
            {
                CreateCreatureObstacleCollider(instance, resourcePath, CalculateLocalBounds(instance, renderers));
            }
        }

        if (resourcePath.Contains("Whale"))
        {
            HeroWhaleDrift drift = instance.AddComponent<HeroWhaleDrift>();
            drift.basePosition = instance.transform.position;
            if (resourcePath.EndsWith("WhaleRigged"))
            {
                instance.AddComponent<WhaleBodyAnimator>();
            }
        }
        else if (resourcePath.EndsWith("Manta") || resourcePath.EndsWith("Turtle"))
        {
            OceanCreatureDrift drift = instance.AddComponent<OceanCreatureDrift>();
            drift.basePosition = instance.transform.position;
            drift.useMantaAxes = resourcePath.EndsWith("Manta");
            drift.phase = drift.useMantaAxes ? 0.4f : 2.2f;
            if (resourcePath.EndsWith("Turtle"))
            {
                TurtleStrokeAnimator stroke = instance.AddComponent<TurtleStrokeAnimator>();
                stroke.phase = 0.18f;
            }
            else
            {
                MantaWingAnimator wings = instance.AddComponent<MantaWingAnimator>();
                wings.phase = drift.phase;
            }
        }
        else if (resourcePath.EndsWith("BlueTang") || resourcePath.EndsWith("Clownfish"))
        {
            ReefFishDrift drift = instance.AddComponent<ReefFishDrift>();
            drift.basePosition = instance.transform.position;
        }
        return instance;
    }

    private static bool ShouldPlaceOnSeafloor(string resourcePath)
    {
        return resourcePath.Contains("Reef") || resourcePath.Contains("Coral") || resourcePath.EndsWith("KelpCluster");
    }

    private static bool ShouldBlockLargeSwimmers(string resourcePath)
    {
        return resourcePath.Contains("Reef") || resourcePath.Contains("Coral");
    }

    private static bool ShouldBlockCreatureBody(string resourcePath)
    {
        return resourcePath.Contains("Whale") || resourcePath.EndsWith("Manta") || resourcePath.EndsWith("Turtle");
    }

    private static Bounds CalculateLocalBounds(GameObject root, Renderer[] renderers)
    {
        bool initialized = false;
        Bounds localBounds = default;
        foreach (Renderer renderer in renderers)
        {
            Bounds rendererBounds = renderer.localBounds;
            Vector3 center = rendererBounds.center;
            Vector3 extents = rendererBounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 rendererCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        Vector3 localCorner = root.transform.InverseTransformPoint(renderer.transform.TransformPoint(rendererCorner));
                        if (!initialized)
                        {
                            localBounds = new Bounds(localCorner, Vector3.zero);
                            initialized = true;
                        }
                        else
                        {
                            localBounds.Encapsulate(localCorner);
                        }
                    }
                }
            }
        }
        return localBounds;
    }

    private static void CreateCreatureObstacleCollider(GameObject instance, string resourcePath, Bounds localBounds)
    {
        GameObject collisionVolume = new("Creature body collision");
        collisionVolume.transform.SetParent(instance.transform, false);
        collisionVolume.transform.localPosition = localBounds.center;
        collisionVolume.AddComponent<UnderwaterObstacle>();

        Vector3 size = localBounds.size;
        if (resourcePath.Contains("Whale"))
        {
            BoxCollider whaleBody = collisionVolume.AddComponent<BoxCollider>();
            whaleBody.size = ScaleBySortedAxes(size, 0.90f, 0.76f, 0.66f);
            return;
        }

        BoxCollider box = collisionVolume.AddComponent<BoxCollider>();
        if (resourcePath.EndsWith("Manta"))
        {
            box.size = ScaleBySortedAxes(size, 0.80f, 0.72f, 0.46f);
        }
        else
        {
            box.size = ScaleBySortedAxes(size, 0.78f, 0.70f, 0.62f);
        }
    }

    private static Vector3 ScaleBySortedAxes(Vector3 size, float largestScale, float middleScale, float smallestScale)
    {
        float largest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
        float smallest = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
        Vector3 scaled = size;
        for (int axis = 0; axis < 3; axis++)
        {
            float value = size[axis];
            float scale = Mathf.Approximately(value, largest) ? largestScale : Mathf.Approximately(value, smallest) ? smallestScale : middleScale;
            scaled[axis] = value * scale;
        }
        return scaled;
    }

    private static float SamplePlacementHeight(float centerX, float centerZ, float extentX, float extentZ)
    {
        float sampleX = Mathf.Min(extentX * 0.32f, 3.2f);
        float sampleZ = Mathf.Min(extentZ * 0.32f, 3.2f);
        float center = SampleSeafloorHeight(centerX, centerZ);
        float diagonalA = SampleSeafloorHeight(centerX - sampleX, centerZ - sampleZ);
        float diagonalB = SampleSeafloorHeight(centerX + sampleX, centerZ + sampleZ);
        float diagonalC = SampleSeafloorHeight(centerX - sampleX, centerZ + sampleZ);
        float diagonalD = SampleSeafloorHeight(centerX + sampleX, centerZ - sampleZ);
        return Mathf.Max(center, Mathf.Min(Mathf.Max(diagonalA, diagonalB), Mathf.Max(diagonalC, diagonalD)));
    }

    private static void CreateMeshObstacleColliders(GameObject instance)
    {
        foreach (MeshFilter filter in instance.GetComponentsInChildren<MeshFilter>())
        {
            if (filter.sharedMesh == null)
            {
                continue;
            }
            MeshCollider collider = filter.gameObject.GetComponent<MeshCollider>();
            if (collider == null)
            {
                collider = filter.gameObject.AddComponent<MeshCollider>();
            }
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = false;
            if (filter.gameObject.GetComponent<UnderwaterObstacle>() == null)
            {
                filter.gameObject.AddComponent<UnderwaterObstacle>();
            }
        }
    }

    private void TintModelMaterials(Renderer[] renderers, Color tint, Color emission)
    {
        foreach (Renderer renderer in renderers)
        {
            Material[] sourceMaterials = renderer.sharedMaterials;
            Material[] tintedMaterials = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                if (source == null)
                {
                    continue;
                }

                Material material = new(source) { name = $"{source.name} color grade" };
                string colorProperty = material.HasProperty("baseColorFactor") ? "baseColorFactor" : material.HasProperty("_BaseColor") ? "_BaseColor" : material.HasProperty("_Color") ? "_Color" : null;
                if (colorProperty != null)
                {
                    Color baseColor = material.GetColor(colorProperty);
                    material.SetColor(colorProperty, new Color(baseColor.r * tint.r, baseColor.g * tint.g, baseColor.b * tint.b, baseColor.a));
                }
                if (emission.maxColorComponent > 0f)
                {
                    if (material.HasProperty("emissiveFactor"))
                    {
                        material.SetColor("emissiveFactor", emission);
                    }
                    else if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", emission);
                    }
                }
                runtimeMaterials.Add(material);
                tintedMaterials[i] = material;
            }
            renderer.sharedMaterials = tintedMaterials;
        }
    }

    private void CreateFallbackReef()
    {
        Material coral = MakeLit("Hero coral", new Color(0.88f, 0.31f, 0.18f), 0f, 0.42f);
        for (int i = 0; i < 12; i++)
        {
            CreateCoral(new Vector3(-12f + Random.Range(-5f, 5f), -2.8f, 23f + Random.Range(-5f, 5f)), Random.Range(1.2f, 3.5f), coral);
        }
    }

    private void CreateFallbackWhale()
    {
        Material skin = MakeLit("Whale skin", new Color(0.08f, 0.16f, 0.22f), 0f, 0.58f);
        GameObject whale = new("Humpback silhouette");
        whale.transform.SetParent(transform);
        whale.transform.position = new Vector3(9f, 0.2f, 91f);
        whale.transform.rotation = Quaternion.Euler(4f, 205f, -3f);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.transform.SetParent(whale.transform, false);
        body.transform.localScale = new Vector3(10f, 2.3f, 2.5f);
        body.GetComponent<Renderer>().sharedMaterial = skin;
        Destroy(body.GetComponent<Collider>());

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(whale.transform, false);
        head.transform.localPosition = new Vector3(8f, -0.2f, 0f);
        head.transform.localScale = new Vector3(4f, 2.2f, 2.7f);
        head.GetComponent<Renderer>().sharedMaterial = skin;
        Destroy(head.GetComponent<Collider>());

        CreateWhaleFin(whale.transform, new Vector3(2f, -0.8f, 2.2f), new Vector3(5.5f, 0.18f, 1.2f), Quaternion.Euler(18f, -26f, 18f), skin);
        CreateWhaleFin(whale.transform, new Vector3(2f, -0.8f, -2.2f), new Vector3(5.5f, 0.18f, 1.2f), Quaternion.Euler(-18f, 26f, -18f), skin);
        CreateWhaleFin(whale.transform, new Vector3(-10f, 0f, 1.5f), new Vector3(3.2f, 0.2f, 1.4f), Quaternion.Euler(0f, 25f, 0f), skin);
        CreateWhaleFin(whale.transform, new Vector3(-10f, 0f, -1.5f), new Vector3(3.2f, 0.2f, 1.4f), Quaternion.Euler(0f, -25f, 0f), skin);

        HeroWhaleDrift drift = whale.AddComponent<HeroWhaleDrift>();
        drift.basePosition = whale.transform.position;
    }

    private void CreateWhaleFin(Transform parent, Vector3 localPosition, Vector3 scale, Quaternion rotation, Material material)
    {
        GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fin.transform.SetParent(parent, false);
        fin.transform.localPosition = localPosition;
        fin.transform.localScale = scale;
        fin.transform.localRotation = rotation;
        fin.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(fin.GetComponent<Collider>());
    }

    private void CreateFallbackRuins()
    {
        Material stone = MakeLit("Ancient stone", new Color(0.26f, 0.31f, 0.28f), 0f, 0.3f);
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.transform.SetParent(transform);
            pillar.transform.position = new Vector3(side * 5f, -6.2f, 168f);
            pillar.transform.localScale = new Vector3(1.2f, 5.4f, 1.2f);
            pillar.GetComponent<Renderer>().sharedMaterial = stone;
            Destroy(pillar.GetComponent<Collider>());
        }
        GameObject lintel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lintel.transform.SetParent(transform);
        lintel.transform.position = new Vector3(0f, -1.1f, 168f);
        lintel.transform.localScale = new Vector3(12.2f, 1.2f, 2.3f);
        lintel.transform.rotation = Quaternion.Euler(0f, 0f, 3f);
        lintel.GetComponent<Renderer>().sharedMaterial = stone;
        Destroy(lintel.GetComponent<Collider>());
    }

    private void CreateRock(Vector3 position, float size, Material material)
    {
        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Organic reef rock";
        rock.transform.SetParent(transform);
        rock.transform.position = position;
        rock.transform.rotation = Random.rotation;
        rock.transform.localScale = new Vector3(size * Random.Range(0.8f, 1.4f), size * Random.Range(0.45f, 0.95f), size * Random.Range(0.8f, 1.5f));
        rock.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(rock.GetComponent<Collider>());
    }

    private void CreateCoral(Vector3 position, float size, Material material)
    {
        GameObject root = new("Branching coral");
        root.transform.SetParent(transform);
        root.transform.position = position;
        int branches = Random.Range(4, 8);
        for (int i = 0; i < branches; i++)
        {
            GameObject branch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            branch.transform.SetParent(root.transform, false);
            float angle = i / (float)branches * 360f + Random.Range(-14f, 14f);
            float height = size * Random.Range(0.65f, 1.2f);
            branch.transform.localPosition = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * size * 0.18f, height * 0.5f, Mathf.Cos(angle * Mathf.Deg2Rad) * size * 0.18f);
            branch.transform.localScale = new Vector3(size * 0.11f, height * 0.5f, size * 0.11f);
            branch.transform.localRotation = Quaternion.Euler(Random.Range(-18f, 18f), angle, Random.Range(-22f, 22f));
            branch.GetComponent<Renderer>().sharedMaterial = material;
            Destroy(branch.GetComponent<Collider>());
        }
    }

    private Material MakeLit(string name, Color color, float metallic, float smoothness, Color? emission = null)
    {
        Material template = Resources.Load<Material>("Materials/UnderwaterLit");
        Shader shader = template != null ? template.shader : Shader.Find("Universal Render Pipeline/Lit");
        Material material = template != null ? new Material(template) : new Material(shader);
        material.name = name;
        material.SetColor("_BaseColor", color);
        material.SetColor("_Color", color);
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
        }
        runtimeMaterials.Add(material);
        return material;
    }

    private Material MakeSchoolFishGradient(string name, Color dorsal, Color mid, Color belly, Color head, float smoothness)
    {
        Material template = Resources.Load<Material>("Materials/SchoolFishGradient");
        Shader shader = template != null ? template.shader : Shader.Find("Underwater/SchoolFishGradient");
        Material material = template != null ? new Material(template) : new Material(shader);
        material.name = name;
        material.SetColor("_DorsalColor", dorsal);
        material.SetColor("_MidColor", mid);
        material.SetColor("_BellyColor", belly);
        material.SetColor("_HeadColor", head);
        material.SetFloat("_Smoothness", smoothness);
        material.enableInstancing = true;
        runtimeMaterials.Add(material);
        return material;
    }

    private Material MakeGlow(string name, Color color, float emission, float scale, float speed)
    {
        Material template = Resources.Load<Material>("Materials/TransparentGlow");
        Shader shader = template != null ? template.shader : Shader.Find("Underwater/TransparentGlow");
        Material material = template != null ? new Material(template) : new Material(shader);
        material.name = name;
        material.SetColor("_BaseColor", color);
        material.SetFloat("_EmissionStrength", emission);
        material.SetFloat("_PatternScale", scale);
        material.SetFloat("_Speed", speed);
        runtimeMaterials.Add(material);
        return material;
    }

    private Material MakeWaterSurfaceMaterial()
    {
        Material template = Resources.Load<Material>("Materials/UnderwaterSurface");
        Shader shader = template != null ? template.shader : Shader.Find("Underwater/Surface");
        Material material = template != null ? new Material(template) : new Material(shader);
        material.name = "Refractive Underwater Surface";
        material.SetColor("_ShallowColor", surfaceShallow);
        material.SetColor("_DeepColor", surfaceDeep);
        material.SetColor("_SunColor", new Color(0.88f, 0.96f, 1f, 1f));
        material.SetFloat("_SunIntensity", 1.15f);
        material.SetFloat("_Distortion", 0.0022f);
        material.SetFloat("_WaveScale", 0.13f);
        material.SetFloat("_WaveSpeed", 0.18f);
        material.SetFloat("_FresnelPower", 4.6f);
        runtimeMaterials.Add(material);
        return material;
    }

    private Material MakeBeamMaterial()
    {
        Material template = Resources.Load<Material>("Materials/VolumetricBeam");
        Shader shader = template != null ? template.shader : Shader.Find("Underwater/VolumetricBeam");
        Material material = template != null ? new Material(template) : new Material(shader);
        material.name = "Soft Volumetric Sun Beams";
        material.SetColor("_BaseColor", new Color(0.68f, 0.91f, 1f, beamAlpha));
        material.SetFloat("_Intensity", beamIntensity);
        material.SetFloat("_Speed", 0.24f);
        runtimeMaterials.Add(material);
        return material;
    }

    private Material MakeCausticMaterial()
    {
        Material template = Resources.Load<Material>("Materials/CausticSeafloor");
        Shader shader = template != null ? template.shader : Shader.Find("Underwater/CausticSeafloor");
        Material material = template != null ? new Material(template) : new Material(shader);
        material.name = "Animated caustic sand";
        Texture2D baseMap = Resources.Load<Texture2D>("Textures/CoastSandRocks02/coast_sand_rocks_02_diff_2k");
        Texture2D normalMap = Resources.Load<Texture2D>("Textures/CoastSandRocks02/coast_sand_rocks_02_nor_gl_2k");
        Texture2D roughnessMap = Resources.Load<Texture2D>("Textures/CoastSandRocks02/coast_sand_rocks_02_rough_2k");
        Texture2D aoMap = Resources.Load<Texture2D>("Textures/CoastSandRocks02/coast_sand_rocks_02_ao_2k");
        if (baseMap != null) material.SetTexture("_BaseMap", baseMap);
        if (normalMap != null) material.SetTexture("_NormalMap", normalMap);
        if (roughnessMap != null) material.SetTexture("_RoughnessMap", roughnessMap);
        if (aoMap != null) material.SetTexture("_AOMap", aoMap);
        material.SetColor("_BaseColor", clearWaterLook ? new Color(0.94f, 0.87f, 0.73f) : new Color(0.82f, 0.70f, 0.56f));
        material.SetColor("_CausticColor", clearWaterLook ? new Color(0.68f, 0.83f, 0.80f) : new Color(0.36f, 0.66f, 0.72f));
        material.SetFloat("_CausticStrength", causticStrength);
        material.SetFloat("_CausticScale", 1.55f);
        material.SetFloat("_CausticSpeed", clearWaterLook ? 0.16f : 0.22f);
        material.SetFloat("_NormalStrength", 0.72f);
        runtimeMaterials.Add(material);
        return material;
    }
}

public sealed class FollowCameraXZ : MonoBehaviour
{
    private Transform target;
    private float fixedHeight;

    public void Initialize(Transform followTarget, float height)
    {
        target = followTarget;
        fixedHeight = height;
        Follow();
    }

    private void LateUpdate()
    {
        Follow();
    }

    private void Follow()
    {
        if (target == null)
        {
            return;
        }

        Vector3 position = target.position;
        transform.position = new Vector3(position.x, fixedHeight, position.z);
    }
}

public sealed class InfiniteSeafloorGrid : MonoBehaviour
{
    private const int GridRadius = 4;
    private const int Segments = 64;
    private const float TileSize = 128f;
    private const float TextureScale = 0.14f;
    public const float VertexSpacing = TileSize / Segments;
    public const float ExplorationRadius = 400f;
    public static readonly Vector3 ExplorationCenter = new(0f, 0f, 64f);

    private sealed class Tile
    {
        public GameObject gameObject;
        public Mesh mesh;
        public Vector2Int coordinate = new(int.MinValue, int.MinValue);
    }

    private readonly List<Tile> tiles = new();
    private Material material;
    private int[] triangles;

    public void Initialize(Transform followTarget, Material sharedMaterial)
    {
        material = sharedMaterial;
        triangles = CreateTriangles();

        for (int z = -GridRadius; z <= GridRadius; z++)
        {
            for (int x = -GridRadius; x <= GridRadius; x++)
            {
                GameObject tileObject = new($"Fixed Seafloor {x}, {z}");
                tileObject.transform.SetParent(transform, false);
                Mesh mesh = new() { name = $"Fixed Seafloor {x}, {z}" };
                tileObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                tileObject.AddComponent<MeshRenderer>().sharedMaterial = material;
                Tile tile = new() { gameObject = tileObject, mesh = mesh, coordinate = new Vector2Int(x, z) };
                tiles.Add(tile);
                RebuildTile(tile);
            }
        }
        CreateDistantSeafloor();
    }

    private void OnDestroy()
    {
        foreach (Tile tile in tiles)
        {
            if (tile.mesh != null)
            {
                Destroy(tile.mesh);
            }
        }
    }

    private void CreateDistantSeafloor()
    {
        // A fixed horizon apron shares every boundary vertex with the detailed floor.
        // Its outer edge is kilometres beyond both the camera and the exploration area.
        float extent = (GridRadius + 0.5f) * TileSize;
        int edgeSegments = (GridRadius * 2 + 1) * Segments;
        for (int side = 0; side < 4; side++)
        {
            Vector3[] vertices = new Vector3[(edgeSegments + 1) * 2];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            int[] indices = new int[edgeSegments * 6];
            for (int i = 0; i <= edgeSegments; i++)
            {
                float edge = -extent + i * VertexSpacing;
                Vector3 inner = side == 0 ? new Vector3(edge, 0f, -extent)
                    : side == 1 ? new Vector3(extent, 0f, edge)
                    : side == 2 ? new Vector3(-edge, 0f, extent)
                    : new Vector3(-extent, 0f, -edge);
                Vector3 outer = inner * 8f;
                inner += ExplorationCenter;
                outer += ExplorationCenter;
                for (int row = 0; row < 2; row++)
                {
                    int index = i * 2 + row;
                    Vector3 point = row == 0 ? inner : outer;
                    point.y = UnderwaterWorld.SampleSeafloorHeight(point.x, point.z);
                    vertices[index] = point;
                    normals[index] = UnderwaterWorld.SampleSeafloorNormal(point.x, point.z);
                    uvs[index] = new Vector2(point.x * TextureScale, point.z * TextureScale);
                }
                if (i < edgeSegments)
                {
                    int index = i * 6;
                    int a = i * 2;
                    indices[index] = a;
                    indices[index + 1] = a + 2;
                    indices[index + 2] = a + 1;
                    indices[index + 3] = a + 2;
                    indices[index + 4] = a + 3;
                    indices[index + 5] = a + 1;
                }
            }
            GameObject horizon = new($"Distant Seafloor {side}");
            horizon.transform.SetParent(transform, false);
            Mesh mesh = new() { name = horizon.name, vertices = vertices, normals = normals, uv = uvs, triangles = indices };
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            horizon.AddComponent<MeshFilter>().sharedMesh = mesh;
            horizon.AddComponent<MeshRenderer>().sharedMaterial = material;
            tiles.Add(new Tile { gameObject = horizon, mesh = mesh });
        }
    }

    private void RebuildTile(Tile tile)
    {
        float centerX = tile.coordinate.x * TileSize;
        float centerZ = tile.coordinate.y * TileSize + ExplorationCenter.z;
        tile.gameObject.transform.position = new Vector3(centerX, 0f, centerZ);

        Vector3[] vertices = new Vector3[(Segments + 1) * (Segments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        Vector3[] normals = new Vector3[vertices.Length];
        for (int z = 0; z <= Segments; z++)
        {
            float localZ = (z / (float)Segments - 0.5f) * TileSize;
            float worldZ = centerZ + localZ;
            for (int x = 0; x <= Segments; x++)
            {
                float localX = (x / (float)Segments - 0.5f) * TileSize;
                float worldX = centerX + localX;
                int vertex = z * (Segments + 1) + x;
                vertices[vertex] = new Vector3(localX, UnderwaterWorld.SampleSeafloorHeight(worldX, worldZ), localZ);
                normals[vertex] = UnderwaterWorld.SampleSeafloorNormal(worldX, worldZ);
                uvs[vertex] = new Vector2(worldX * TextureScale, worldZ * TextureScale);
            }
        }

        tile.mesh.Clear();
        tile.mesh.vertices = vertices;
        tile.mesh.normals = normals;
        tile.mesh.uv = uvs;
        tile.mesh.triangles = triangles;
        tile.mesh.RecalculateTangents();
        tile.mesh.RecalculateBounds();
    }

    private static int[] CreateTriangles()
    {
        int[] result = new int[Segments * Segments * 6];
        int index = 0;
        for (int z = 0; z < Segments; z++)
        {
            for (int x = 0; x < Segments; x++)
            {
                int a = z * (Segments + 1) + x;
                int b = a + 1;
                int c = a + Segments + 1;
                int d = c + 1;
                result[index++] = a;
                result[index++] = c;
                result[index++] = b;
                result[index++] = b;
                result[index++] = c;
                result[index++] = d;
            }
        }
        return result;
    }
}

public sealed class UnderwaterObstacle : MonoBehaviour
{
    public bool isSeafloor;
}

public static class UnderwaterCollision
{
    private const float Skin = 0.06f;
    private static readonly RaycastHit[] Hits = new RaycastHit[64];
    private static readonly Collider[] Overlaps = new Collider[64];
    private static SphereCollider probe;

    public static Vector3 ResolveMove(Transform mover, Vector3 desiredPosition, float radius, float floorClearance, float noseDistance, out bool adjusted)
    {
        Vector3 start = mover.position;
        adjusted = false;
        float ceiling = UnderwaterWorld.WaterSurfaceHeight - 0.1f;
        if (desiredPosition.y > ceiling)
        {
            desiredPosition.y = ceiling;
            adjusted = true;
        }
        Vector3 requested = desiredPosition - start;
        if (!SeparateFromObstacles(mover, ref start, radius, ref adjusted))
        {
            return mover.position;
        }
        desiredPosition = start + requested;
        desiredPosition = ConstrainToSeafloor(start, desiredPosition, floorClearance, noseDistance, ref adjusted);
        Vector3 current = start;
        Vector3 remaining = desiredPosition - current;

        for (int pass = 0; pass < 3 && remaining.sqrMagnitude > 0.000001f; pass++)
        {
            float distance = remaining.magnitude;
            Vector3 direction = remaining / distance;
            if (!FindNearestObstacle(mover, current, radius, direction, distance + Skin, out RaycastHit hit))
            {
                current += remaining;
                break;
            }

            adjusted = true;
            float travel = Mathf.Max(0f, hit.distance - Skin);
            current += direction * travel;
            Vector3 unused = remaining - direction * travel;
            Vector3 slide = Vector3.ProjectOnPlane(unused, hit.normal);
            remaining = slide;
        }

        current = ConstrainToSeafloor(start, current, floorClearance, noseDistance, ref adjusted);
        return SeparateFromObstacles(mover, ref current, radius, ref adjusted) ? current : start;
    }

    private static bool SeparateFromObstacles(Transform mover, ref Vector3 position, float radius, ref bool adjusted)
    {
        if (probe == null)
        {
            GameObject helper = new("Swimmer collision probe") { hideFlags = HideFlags.HideAndDontSave };
            probe = helper.AddComponent<SphereCollider>();
            probe.isTrigger = true;
        }
        probe.radius = radius;
        for (int pass = 0; pass < 6; pass++)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, Overlaps, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            bool overlapping = false;
            for (int i = 0; i < count; i++)
            {
                Collider obstacle = Overlaps[i];
                if (!IsObstacle(mover, obstacle)) continue;
                if (Physics.ComputePenetration(probe, position, Quaternion.identity,
                    obstacle, obstacle.transform.position, obstacle.transform.rotation, out Vector3 direction, out float distance))
                {
                    position += direction * (distance + Skin);
                    adjusted = true;
                    overlapping = true;
                }
            }
            if (!overlapping) return true;
        }
        return false;
    }

    private static bool IsObstacle(Transform mover, Collider collider)
    {
        UnderwaterObstacle obstacle = collider.GetComponentInParent<UnderwaterObstacle>();
        return obstacle != null && !obstacle.isSeafloor && !collider.transform.IsChildOf(mover);
    }

    private static Vector3 ConstrainToSeafloor(Vector3 start, Vector3 proposed, float clearance, float noseDistance, ref bool adjusted)
    {
        Vector3 center = InfiniteSeafloorGrid.ExplorationCenter;
        float limit = InfiniteSeafloorGrid.ExplorationRadius;
        for (int axis = 0; axis <= 2; axis += 2)
        {
            float offset = start[axis] - center[axis];
            float move = proposed[axis] - start[axis];
            if (offset * move > 0f && Mathf.Abs(offset) > limit - 40f)
            {
                float flow = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((limit - Mathf.Abs(offset)) / 40f));
                proposed[axis] = start[axis] + move * flow;
                adjusted = true;
            }
        }
        Vector3 bounded = new(Mathf.Clamp(proposed.x, center.x - limit, center.x + limit), proposed.y,
            Mathf.Clamp(proposed.z, center.z - limit, center.z + limit));
        adjusted |= bounded != proposed;
        proposed = bounded;
        Vector3 requestedMove = proposed - start;
        Vector3 horizontal = new(proposed.x - start.x, 0f, proposed.z - start.z);
        Vector3 direction = horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : Vector3.zero;
        Vector3 nose = proposed + direction * noseDistance;
        float floorAtBody = UnderwaterWorld.SampleSeafloorHeight(proposed.x, proposed.z);
        float floorAtNose = UnderwaterWorld.SampleSeafloorHeight(nose.x, nose.z);
        float minimumY = Mathf.Max(floorAtBody, floorAtNose) + clearance;
        if (proposed.y >= minimumY)
        {
            return proposed;
        }

        adjusted = true;
        if (horizontal.sqrMagnitude < 0.0001f)
        {
            proposed.y = minimumY;
            return proposed;
        }
        Vector3 normal = UnderwaterWorld.SampleSeafloorNormal(nose.x, nose.z);
        Vector3 slide = Vector3.ProjectOnPlane(proposed - start, normal);
        proposed = start + slide;
        proposed.x = Mathf.Clamp(proposed.x, center.x - limit, center.x + limit);
        proposed.z = Mathf.Clamp(proposed.z, center.z - limit, center.z + limit);
        nose = proposed + direction * noseDistance;
        floorAtBody = UnderwaterWorld.SampleSeafloorHeight(proposed.x, proposed.z);
        floorAtNose = UnderwaterWorld.SampleSeafloorHeight(nose.x, nose.z);
        minimumY = Mathf.Max(floorAtBody, floorAtNose) + clearance;
        proposed.y = requestedMove.y < 0f ? minimumY : Mathf.Max(proposed.y, minimumY);
        return proposed;
    }

    private static bool FindNearestObstacle(Transform mover, Vector3 origin, float radius, Vector3 direction, float distance, out RaycastHit nearest)
    {
        nearest = default;
        int count = Physics.SphereCastNonAlloc(origin, radius, direction, Hits, distance, Physics.AllLayers, QueryTriggerInteraction.Ignore);
        float nearestDistance = float.MaxValue;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = Hits[i];
            if (!IsObstacle(mover, hit.collider))
            {
                continue;
            }
            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                nearest = hit;
                found = true;
            }
        }
        return found;
    }
}

public sealed class CinematicDive : MonoBehaviour
{
    public Vector3[] path;
    public float duration = 48f;
    private float clock;
    private float yawOffset;
    private float pitchOffset;
    private Vector2 previousPointer;
    private Vector2 pressPointer;
    private float pointerTravel;
    private bool dragging;
    private bool autoTour = true;
    private Camera diveCamera;
    private MarineEncounter lastClickedEncounter;
    private MarineEncounter guidedEncounter;
    private float lastClickTime = -10f;
    private bool guidingAnimal;
    private bool guidanceStarted;
    private MissionInteractable netCutTarget;
    private bool cuttingNet;
    private bool netCutApplied;
    private Vector2 netCutStart;
    private Vector2 previousNetCutPointer;
    private float netCutDepth;
    private Vector3 freePosition;
    private Vector3 swimVelocity;
    private Vector3 hudMove;
    private float freeYaw;
    private float freePitch;
    private Transform previewWhale;
    private OceanCreatureDrift debugReleaseCreature;
    private float playerRadius = 0.48f;
    private float playerFloorClearance = 1.2f;
    private float playerNoseDistance = 0.85f;
    private float playerSwimSpeed = 7.5f;
    private float playerBoostSpeed = 13f;
    private float playerTurnScale = 1f;

    public bool IsFreeDive => !autoTour;
    public float SwimSpeed01 => Mathf.Clamp01(swimVelocity.magnitude / Mathf.Max(1f, playerSwimSpeed * 1.15f));

    public void ConfigurePlayerRole(string roleKey)
    {
        switch (roleKey)
        {
            case "clownfish":
                playerRadius = 0.16f;
                playerFloorClearance = 0.38f;
                playerNoseDistance = 0.32f;
                playerSwimSpeed = 8.4f;
                playerBoostSpeed = 13.8f;
                playerTurnScale = 1.28f;
                break;
            case "manta":
                playerRadius = 0.62f;
                playerFloorClearance = 1.05f;
                playerNoseDistance = 1.10f;
                playerSwimSpeed = 6.8f;
                playerBoostSpeed = 11.2f;
                playerTurnScale = 0.74f;
                break;
            case "whale":
                playerRadius = 1.10f;
                playerFloorClearance = 1.85f;
                playerNoseDistance = 1.75f;
                playerSwimSpeed = 7.2f;
                playerBoostSpeed = 10.4f;
                playerTurnScale = 0.52f;
                break;
            default:
                playerRadius = 0.38f;
                playerFloorClearance = 0.95f;
                playerNoseDistance = 0.72f;
                playerSwimSpeed = 6.4f;
                playerBoostSpeed = 11.4f;
                playerTurnScale = 0.90f;
                break;
        }
    }

    private void Start()
    {
        diveCamera = GetComponent<Camera>();
        bool startAtArch = Application.absoluteURL.Contains("start=arch");
        bool startAtWhale = Application.absoluteURL.Contains("start=whale");
        bool startAtBlueTang = Application.absoluteURL.Contains("start=blue");
        bool startAtFish = Application.absoluteURL.Contains("start=fish");
        bool startAtManta = Application.absoluteURL.Contains("start=manta");
        bool startAtTurtle = Application.absoluteURL.Contains("start=turtle");
        bool startAtJelly = Application.absoluteURL.Contains("start=jelly");
        clock = startAtArch ? FindNearestPathClock(new Vector3(-2f, -8f, 158f)) : 0f;
        if (path != null && path.Length > 0)
        {
            transform.position = startAtArch ? EvaluatePath(clock / duration) : path[0];
            freePosition = transform.position;
        }

        if (startAtWhale)
        {
            HeroWhaleDrift whale = FindFirstObjectByType<HeroWhaleDrift>();
            if (whale != null)
            {
                previewWhale = whale.transform;
                autoTour = false;
                freePosition = previewWhale.position + new Vector3(-13.5f, 3.2f, -18f);
                transform.position = freePosition;
                Quaternion previewRotation = Quaternion.LookRotation(previewWhale.position - transform.position, Vector3.up);
                transform.rotation = previewRotation;
                freeYaw = previewRotation.eulerAngles.y;
                freePitch = NormalizeAngle(previewRotation.eulerAngles.x);
            }
        }
        else if (startAtBlueTang)
        {
            ReefFishDrift blueTang = null;
            float largestScale = 0f;
            foreach (ReefFishDrift candidate in FindObjectsByType<ReefFishDrift>(FindObjectsSortMode.None))
            {
                if (!candidate.isBlueTang)
                {
                    continue;
                }

                float candidateScale = candidate.transform.lossyScale.sqrMagnitude;
                if (blueTang == null || candidateScale > largestScale)
                {
                    blueTang = candidate;
                    largestScale = candidateScale;
                }
            }
            if (blueTang != null)
            {
                previewWhale = blueTang.transform;
                autoTour = false;
                freePosition = previewWhale.position + new Vector3(4.8f, 1.2f, -6.2f);
                transform.position = freePosition;
                Quaternion previewRotation = Quaternion.LookRotation(previewWhale.position + Vector3.up * 0.35f - transform.position, Vector3.up);
                transform.rotation = previewRotation;
                freeYaw = previewRotation.eulerAngles.y;
                freePitch = NormalizeAngle(previewRotation.eulerAngles.x);
                diveCamera.fieldOfView = 43f;
            }
        }
        else if (startAtFish)
        {
            FishSwimmer[] fish = FindObjectsByType<FishSwimmer>(FindObjectsSortMode.None);
            Vector3 center = new(-1f, 1.4f, 23f);
            int nearby = 0;
            Vector3 average = Vector3.zero;
            foreach (FishSwimmer swimmer in fish)
            {
                if (swimmer.transform.position.z < 34f)
                {
                    average += swimmer.transform.position;
                    nearby++;
                }
            }
            if (nearby > 0)
            {
                center = average / nearby;
            }

            autoTour = false;
            freePosition = center + new Vector3(0.8f, 2.8f, -8.2f);
            transform.position = freePosition;
            Quaternion previewRotation = Quaternion.LookRotation(center + Vector3.up * 0.9f - transform.position, Vector3.up);
            transform.rotation = previewRotation;
            freeYaw = previewRotation.eulerAngles.y;
            freePitch = NormalizeAngle(previewRotation.eulerAngles.x);
            diveCamera.fieldOfView = 43f;
        }
        else if (startAtManta || startAtTurtle)
        {
            OceanCreatureDrift[] creatures = FindObjectsByType<OceanCreatureDrift>(FindObjectsSortMode.None);
            foreach (OceanCreatureDrift creature in creatures)
            {
                if (creature.useMantaAxes != startAtManta) continue;
                previewWhale = creature.transform;
                autoTour = false;
                freePosition = previewWhale.position + (startAtManta ? new Vector3(0f, 2.8f, -14.5f) : new Vector3(0f, 1.8f, -9.5f));
                transform.position = freePosition;
                Quaternion previewRotation = Quaternion.LookRotation(previewWhale.position + Vector3.up * 0.7f - transform.position, Vector3.up);
                transform.rotation = previewRotation;
                freeYaw = previewRotation.eulerAngles.y;
                freePitch = NormalizeAngle(previewRotation.eulerAngles.x);
                diveCamera.fieldOfView = 46f;
                if (startAtTurtle && Application.absoluteURL.Contains("verify=release"))
                {
                    debugReleaseCreature = creature;
                    Invoke(nameof(BeginDebugTurtleRelease), 20f);
                }
                break;
            }
        }
        else if (startAtJelly)
        {
            JellyDrift jelly = null;
            foreach (JellyDrift candidate in FindObjectsByType<JellyDrift>(FindObjectsSortMode.None))
            {
                if (jelly == null || candidate.transform.position.z < jelly.transform.position.z)
                {
                    jelly = candidate;
                }
            }
            if (jelly != null)
            {
                previewWhale = jelly.transform;
                autoTour = false;
                freePosition = previewWhale.position + new Vector3(-5.8f, 2.2f, -9.8f);
                transform.position = freePosition;
                Quaternion previewRotation = Quaternion.LookRotation(previewWhale.position + Vector3.up * 0.8f - transform.position, Vector3.up);
                transform.rotation = previewRotation;
                freeYaw = previewRotation.eulerAngles.y;
                freePitch = NormalizeAngle(previewRotation.eulerAngles.x);
                diveCamera.fieldOfView = 46f;
            }
        }
    }

    private void BeginDebugTurtleRelease()
    {
        if (debugReleaseCreature == null)
        {
            return;
        }

        previewWhale = null;
        debugReleaseCreature.ResumeFromGuidance(new Vector3(4.2f, 0.35f, 2.6f));
    }

    private void Update()
    {
        if (path == null || path.Length < 4)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
        {
            CancelNetCut();
            guidingAnimal = false;
            guidanceStarted = false;
            dragging = false;
            if (guidedEncounter != null)
            {
                guidedEncounter.SetGuided(false, diveCamera);
                guidedEncounter = null;
            }
            clock = 0f;
            yawOffset = 0f;
            pitchOffset = 0f;
            autoTour = true;
            swimVelocity = Vector3.zero;
            if (diveCamera != null)
            {
                diveCamera.fieldOfView = 58f;
            }
            transform.position = path[0];
            freePosition = transform.position;
            UnderwaterHud.Instance?.ShowStatus("已返回珊瑚礁起点");
        }

        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            if (autoTour)
            {
                EnterFreeDive();
            }
            else
            {
                ReturnToAutoTour();
            }
        }

        Vector3 moveInput = ReadMoveInput(keyboard) + hudMove;
        moveInput = Vector3.ClampMagnitude(moveInput, 1f);
        bool boost = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        if (moveInput.sqrMagnitude > 0.001f && autoTour)
        {
            EnterFreeDive();
        }

        HandlePointer();
        if (autoTour)
        {
            UpdateAutoTour();
        }
        else
        {
            UpdateFreeDive(moveInput, boost);
        }
    }

    public void SetHudMove(Vector3 move) => hudMove = Vector3.ClampMagnitude(move, 1f);
    public MarineEncounter GuidedEncounter => guidedEncounter;
    public bool IsGuidingAnimal => guidingAnimal;

    public void StartMissionAt(Vector3 cameraPosition, Vector3 lookTarget)
    {
        CancelNetCut();
        guidedEncounter?.SetGuided(false, diveCamera);
        guidedEncounter = null;
        guidingAnimal = false;
        guidanceStarted = false;
        dragging = false;
        lastClickedEncounter = null;
        lastClickTime = -10f;
        hudMove = Vector3.zero;
        previewWhale = null;
        autoTour = false;
        swimVelocity = Vector3.zero;
        freePosition = cameraPosition;
        transform.position = cameraPosition;
        Quaternion rotation = Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up);
        transform.rotation = rotation;
        freeYaw = rotation.eulerAngles.y;
        freePitch = NormalizeAngle(rotation.eulerAngles.x);
        yawOffset = 0f;
        pitchOffset = 0f;
        if (diveCamera != null)
        {
            diveCamera.fieldOfView = 58f;
        }
    }

    private Vector3 ReadMoveInput(Keyboard keyboard)
    {
        if (keyboard == null)
        {
            return Vector3.zero;
        }

        float horizontal = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
            - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float forward = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
            - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        float vertical = (keyboard.eKey.isPressed ? 1f : 0f) - (keyboard.qKey.isPressed ? 1f : 0f);
        return new Vector3(horizontal, vertical, forward);
    }

    private void EnterFreeDive()
    {
        autoTour = false;
        freePosition = transform.position;
        Vector3 angles = transform.eulerAngles;
        freeYaw = angles.y;
        freePitch = NormalizeAngle(angles.x);
        yawOffset = 0f;
        pitchOffset = 0f;
        UnderwaterHud.Instance?.ShowStatus("开始自由探索");
    }

    private void ReturnToAutoTour()
    {
        autoTour = true;
        swimVelocity = Vector3.zero;
        clock = FindNearestPathClock(transform.position);
        yawOffset = 0f;
        pitchOffset = 0f;
        UnderwaterHud.Instance?.ShowStatus("已恢复自动巡游");
    }

    private void UpdateAutoTour()
    {
        clock = Mathf.Repeat(clock + Time.deltaTime, duration);
        float t = clock / duration;
        Vector3 position = EvaluatePath(t);
        Vector3 lookPoint = EvaluatePath(Mathf.Repeat(t + 0.012f, 1f)) + Vector3.up * 1.65f;
        Vector3 forward = (lookPoint - position).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
        float buoyancy = Mathf.Sin(Time.time * 0.62f) * 0.10f + Mathf.Sin(Time.time * 0.21f + 1.3f) * 0.055f;
        float lateralDrift = Mathf.Sin(Time.time * 0.17f + 0.8f) * 0.075f;
        position += Vector3.up * buoyancy + right * lateralDrift;

        Quaternion pathRotation = Quaternion.LookRotation((lookPoint - position).normalized, Vector3.up);
        Quaternion waterDrift = Quaternion.Euler(
            Mathf.Sin(Time.time * 0.27f) * 0.45f,
            Mathf.Sin(Time.time * 0.19f + 0.6f) * 0.55f,
            Mathf.Sin(Time.time * 0.24f + 1.4f) * 0.65f);
        Quaternion userRotation = Quaternion.Euler(pitchOffset, yawOffset, 0f);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, position, 1f - Mathf.Exp(-Time.deltaTime * 1.75f));
        smoothedPosition = UnderwaterCollision.ResolveMove(transform, smoothedPosition, playerRadius, playerFloorClearance, playerNoseDistance, out _);
        transform.position = smoothedPosition;
        transform.rotation = Quaternion.Slerp(transform.rotation, pathRotation * waterDrift * userRotation, 1f - Mathf.Exp(-Time.deltaTime * 1.9f));
    }

    private void UpdateFreeDive(Vector3 moveInput, bool boost)
    {
        if (previewWhale != null && moveInput.sqrMagnitude < 0.001f && !dragging)
        {
            Vector3 direction = previewWhale.position - transform.position;
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion previewRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, previewRotation, 1f - Mathf.Exp(-Time.deltaTime * 2.8f));
                Vector3 angles = transform.eulerAngles;
                freeYaw = angles.y;
                freePitch = NormalizeAngle(angles.x);
            }
            return;
        }

        previewWhale = null;
        Vector3 desiredDirection = transform.right * moveInput.x + Vector3.up * moveInput.y + transform.forward * moveInput.z;
        desiredDirection = Vector3.ClampMagnitude(desiredDirection, 1f);
        float topSpeed = boost ? playerBoostSpeed : playerSwimSpeed;
        Vector3 desiredVelocity = desiredDirection * topSpeed;
        float response = desiredDirection.sqrMagnitude > 0.001f ? 7.2f : 12f;
        swimVelocity = Vector3.MoveTowards(swimVelocity, desiredVelocity, response * Time.deltaTime);
        float motion = SwimSpeed01;
        float buoyancy = (Mathf.Sin(Time.time * 0.58f) - Mathf.Sin((Time.time - Time.deltaTime) * 0.58f))
            * (0.055f + motion * 0.018f);
        freePosition = transform.position + swimVelocity * Time.deltaTime + Vector3.up * buoyancy;
        freePosition.y = Mathf.Clamp(freePosition.y, -17f, UnderwaterWorld.WaterSurfaceHeight - 0.65f);
        freePosition = UnderwaterCollision.ResolveMove(transform, freePosition, playerRadius, playerFloorClearance, playerNoseDistance, out bool avoidedObstacle);
        if (avoidedObstacle)
        {
            Vector3 resolvedVelocity = (freePosition - transform.position) / Mathf.Max(Time.deltaTime, 0.0001f);
            swimVelocity = resolvedVelocity;
        }

        float breathingRoll = Mathf.Sin(Time.time * 0.36f + 0.8f) * (0.35f + motion * 0.25f);
        Quaternion targetRotation = Quaternion.Euler(freePitch, freeYaw, breathingRoll);
        if (moveInput.sqrMagnitude > 0.001f)
            UnderwaterHud.Instance?.RecordManualTravel(Vector3.Distance(transform.position, freePosition));
        transform.position = freePosition;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-Time.deltaTime * 7.5f));
    }

    private float FindNearestPathClock(Vector3 point)
    {
        float bestT = 0f;
        float bestDistance = float.MaxValue;
        const int sampleCount = 96;
        for (int i = 0; i <= sampleCount; i++)
        {
            float t = i / (float)sampleCount;
            float distance = (EvaluatePath(Mathf.Min(t, 0.99999f)) - point).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestT = t;
            }
        }
        return bestT * duration;
    }

    private static float NormalizeAngle(float angle) => angle > 180f ? angle - 360f : angle;

    private void HandlePointer()
    {
        Pointer pointer = Pointer.current;
        if (pointer == null)
        {
            CancelNetCut();
            return;
        }

        Vector2 pointerPosition = pointer.position.ReadValue();
        if (UnderwaterHud.IsPointerOverTravelControls(pointerPosition))
        {
            CancelNetCut();
            UnderwaterHud.Instance?.SetHover(string.Empty, false, pointerPosition);
            dragging = false;
            if (guidingAnimal)
            {
                guidingAnimal = false;
                if (guidanceStarted)
                {
                    guidedEncounter?.SetGuided(false, diveCamera);
                    guidedEncounter = null;
                }
                guidanceStarted = false;
            }
            return;
        }
        UpdateHover(pointerPosition);
        if (WasPointerPressed(pointer))
        {
            if (TryBeginNetCut(pointerPosition))
            {
                dragging = false;
                guidingAnimal = false;
                guidanceStarted = false;
                return;
            }
            guidingAnimal = guidedEncounter != null && PointerHitsEncounter(pointerPosition, guidedEncounter);
            guidanceStarted = false;
            dragging = !guidingAnimal;
            pressPointer = pointerPosition;
            previousPointer = pointerPosition;
            pointerTravel = 0f;
        }
        if (cuttingNet)
        {
            UpdateNetCut(pointerPosition, WasPointerReleased(pointer) || !IsPointerPressed(pointer));
            return;
        }
        if (guidingAnimal && guidedEncounter != null && IsPointerPressed(pointer))
        {
            Vector2 delta = pointerPosition - previousPointer;
            previousPointer = pointerPosition;
            pointerTravel += delta.magnitude;
            if (pointerTravel >= 4f)
            {
                if (!guidanceStarted)
                {
                    guidedEncounter.BeginGuidance(diveCamera);
                    guidanceStarted = true;
                }
                guidedEncounter.GuideTowardsPointer(pointerPosition, delta);
            }
        }
        else if (dragging)
        {
            Vector2 delta = pointerPosition - previousPointer;
            previousPointer = pointerPosition;
            pointerTravel += delta.magnitude;
            UnderwaterHud.Instance?.RecordManualLook(delta.magnitude);
            if (autoTour)
            {
                yawOffset += delta.x * 0.11f * playerTurnScale;
                pitchOffset = Mathf.Clamp(pitchOffset - delta.y * 0.09f * playerTurnScale, -35f, 25f);
            }
            else
            {
                freeYaw += delta.x * 0.11f * playerTurnScale;
                freePitch = Mathf.Clamp(freePitch - delta.y * 0.09f * playerTurnScale, -82f, 82f);
            }
        }
        if (WasPointerReleased(pointer))
        {
            if (guidingAnimal)
            {
                guidingAnimal = false;
                if (!guidanceStarted) return;
                guidanceStarted = false;
                if (guidedEncounter != null)
                {
                    guidedEncounter.SetGuided(false, diveCamera);
                    guidedEncounter = null;
                }
                return;
            }
            dragging = false;
            if (pointerTravel < 12f && Vector2.Distance(pressPointer, pointerPosition) < 12f)
            {
                TryDiscover(pointerPosition);
            }
        }

        Mouse mouse = Mouse.current;
        if (mouse != null && diveCamera != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                diveCamera.fieldOfView = Mathf.Clamp(diveCamera.fieldOfView - scroll * 0.012f, 38f, 75f);
            }
        }
    }

    private bool TryBeginNetCut(Vector2 pointerPosition)
    {
        CancelNetCut();
        OceanMissionDirector director = OceanMissionDirector.Instance;
        if (diveCamera == null || director == null || !director.IsNetCuttingStage
            || !director.NetCutterEquipped || !director.IsNetWithinReach || director.IsMenuOpen
            || UnderwaterHud.IsPointerOverTravelControls(pointerPosition)) return false;
        Ray ray = diveCamera.ScreenPointToRay(pointerPosition);
        // Reach is measured to the animal, not the far-side strand. Keep that
        // same gate as the director while allowing a ray across the shell.
        RaycastHit[] hits = Physics.RaycastAll(ray, 7.5f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        int visibleCount = VisibleHitCount(hits);
        for (int i = 0; i < visibleCount; i++)
        {
            MissionInteractable target = hits[i].collider.GetComponentInParent<MissionInteractable>();
            if (target == null || target.IsComplete || !director.IsCurrentNetTarget(target)) continue;
            netCutTarget = target;
            cuttingNet = true;
            netCutApplied = false;
            netCutStart = previousNetCutPointer = pointerPosition;
            netCutDepth = Mathf.Max(0.3f, Vector3.Dot(target.transform.position - ray.origin, ray.direction));
            pointerTravel = float.PositiveInfinity;
            director.NetCutter?.PoseAt(ray.GetPoint(netCutDepth), Vector2.zero);
            return true;
        }
        return false;
    }

    private void UpdateNetCut(Vector2 pointerPosition, bool released)
    {
        if (!cuttingNet) return;
        if (released)
        {
            CancelNetCut();
            return;
        }
        // The final cut changes mission stage while the button is still held.
        // Keep consuming that press until release instead of selecting the turtle.
        if (netCutApplied) return;
        OceanMissionDirector director = OceanMissionDirector.Instance;
        if (director == null || !director.IsNetCuttingStage || !director.NetCutterEquipped
            || director.IsMenuOpen || pointerPosition.x < 0f || pointerPosition.x > Screen.width
            || pointerPosition.y < 0f || pointerPosition.y > Screen.height
            || UnderwaterHud.IsPointerOverTravelControls(pointerPosition))
        {
            CancelNetCut();
            return;
        }
        if (netCutTarget == null || netCutTarget.IsComplete || !director.IsCurrentNetTarget(netCutTarget))
        {
            CancelNetCut();
            return;
        }
        Ray ray = diveCamera.ScreenPointToRay(pointerPosition);
        Vector2 delta = pointerPosition - previousNetCutPointer;
        previousNetCutPointer = pointerPosition;
        director.NetCutter?.PoseAt(ray.GetPoint(netCutDepth), delta);
        if (Vector2.Distance(netCutStart, pointerPosition) < 22f) return;
        int previousCut = netCutTarget.knotIndex;
        director.TryCutNet(previousCut);
        // Keep this press consumed even if its target was rejected (for example
        // after swimming out of reach). A held stroke can never cut the next span.
        netCutApplied = true;
    }

    private void CancelNetCut()
    {
        if (cuttingNet) pointerTravel = float.PositiveInfinity;
        cuttingNet = false;
        netCutApplied = false;
        netCutTarget = null;
        OceanMissionDirector.Instance?.NetCutter?.ResetPose();
    }

    private static bool WasPointerPressed(Pointer pointer)
    {
        if (pointer is Mouse mouse)
        {
            return mouse.leftButton.wasPressedThisFrame;
        }
        if (pointer is Touchscreen touchscreen)
        {
            return touchscreen.primaryTouch.press.wasPressedThisFrame;
        }
        return pointer is Pen pen && pen.tip.wasPressedThisFrame;
    }

    private static bool WasPointerReleased(Pointer pointer)
    {
        if (pointer is Mouse mouse)
        {
            return mouse.leftButton.wasReleasedThisFrame;
        }
        if (pointer is Touchscreen touchscreen)
        {
            return touchscreen.primaryTouch.press.wasReleasedThisFrame;
        }
        return pointer is Pen pen && pen.tip.wasReleasedThisFrame;
    }

    private static bool IsPointerPressed(Pointer pointer)
    {
        if (pointer is Mouse mouse)
        {
            return mouse.leftButton.isPressed;
        }
        if (pointer is Touchscreen touchscreen)
        {
            return touchscreen.primaryTouch.press.isPressed;
        }
        return pointer is Pen pen && pen.tip.isPressed;
    }

    private bool PointerHitsEncounter(Vector2 pointerPosition, MarineEncounter target)
    {
        if (diveCamera == null) return false;
        RaycastHit[] hits = Physics.RaycastAll(diveCamera.ScreenPointToRay(pointerPosition), 180f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        int visibleCount = VisibleHitCount(hits);
        for (int i = 0; i < visibleCount; i++)
            if (hits[i].collider.GetComponentInParent<MissionInteractable>() != null) return false;
        for (int i = 0; i < visibleCount; i++)
        {
            MarineEncounter encounter = hits[i].collider.GetComponentInParent<MarineEncounter>();
            if (encounter != null) return encounter == target;
        }
        return false;
    }

    private void TryDiscover(Vector2 pointerPosition)
    {
        if (diveCamera == null)
        {
            return;
        }

        Ray ray = diveCamera.ScreenPointToRay(pointerPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 180f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        int visibleCount = VisibleHitCount(hits);
        for (int i = 0; i < visibleCount; i++)
        {
            RaycastHit hit = hits[i];
            MissionInteractable missionTarget = hit.collider.GetComponentInParent<MissionInteractable>();
            if (missionTarget != null)
            {
                missionTarget.Activate();
                return;
            }
        }
        for (int i = 0; i < visibleCount; i++)
        {
            RaycastHit hit = hits[i];
            MarineEncounter encounter = hit.collider.GetComponentInParent<MarineEncounter>();
            if (encounter == null)
            {
                continue;
            }

            encounter.Discover();
            float now = Time.unscaledTime;
            if ((OceanMissionDirector.Instance?.IsRescueGuidanceTarget(encounter) ?? false)
                || (encounter == lastClickedEncounter && now - lastClickTime < 0.42f))
            {
                guidedEncounter?.SetGuided(false, diveCamera);
                guidedEncounter = encounter;
                guidedEncounter.SetGuided(true, diveCamera);
                lastClickedEncounter = null;
                lastClickTime = -10f;
            }
            else
            {
                lastClickedEncounter = encounter;
                lastClickTime = now;
            }
            return;
        }
    }

    private void UpdateHover(Vector2 pointerPosition)
    {
        if (diveCamera == null || UnderwaterHud.Instance == null)
        {
            return;
        }

        Ray ray = diveCamera.ScreenPointToRay(pointerPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 180f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        int visibleCount = VisibleHitCount(hits);
        for (int i = 0; i < visibleCount; i++)
        {
            RaycastHit hit = hits[i];
            MissionInteractable missionTarget = hit.collider.GetComponentInParent<MissionInteractable>();
            if (missionTarget != null)
            {
                UnderwaterHud.Instance.SetHover(missionTarget.DisplayName, missionTarget.IsComplete, pointerPosition);
                return;
            }
        }
        for (int i = 0; i < visibleCount; i++)
        {
            RaycastHit hit = hits[i];
            MarineEncounter encounter = hit.collider.GetComponentInParent<MarineEncounter>();
            if (encounter != null)
            {
                UnderwaterHud.Instance.SetHover(encounter.DisplayName, encounter.IsDiscovered, pointerPosition);
                return;
            }
        }
        UnderwaterHud.Instance.SetHover(null, false, pointerPosition);
    }

    private static int VisibleHitCount(RaycastHit[] hits)
    {
        for (int i = 0; i < hits.Length; i++)
        {
            Collider collider = hits[i].collider;
            // Animal body colliders must not hide a rescue knot on that animal.
            // Solid scenery, however, ends the selectable part of the ray.
            if (!collider.isTrigger && collider.GetComponentInParent<UnderwaterObstacle>() != null
                && collider.GetComponentInParent<MarineEncounter>() == null)
            {
                return i;
            }
        }
        return hits.Length;
    }

    private Vector3 EvaluatePath(float t)
    {
        int sectionCount = path.Length - 1;
        float scaled = t * sectionCount;
        int section = Mathf.Min(Mathf.FloorToInt(scaled), sectionCount - 1);
        float localT = scaled - section;
        Vector3 p0 = path[Mathf.Max(section - 1, 0)];
        Vector3 p1 = path[section];
        Vector3 p2 = path[Mathf.Min(section + 1, path.Length - 1)];
        Vector3 p3 = path[Mathf.Min(section + 2, path.Length - 1)];
        return 0.5f * ((2f * p1) + (-p0 + p2) * localT + (2f * p0 - 5f * p1 + 4f * p2 - p3) * localT * localT + (-p0 + 3f * p1 - 3f * p2 + p3) * localT * localT * localT);
    }
}

public sealed class FishSwimmer : MonoBehaviour
{
    public Vector3 schoolCenter;
    public Vector3 formationOffset;
    public float radius = 8f;
    public float speed = 1.5f;
    public float phase;
    private Vector3 baseScale;
    private Vector3 velocity;
    private Vector3 steeringVelocity;
    private Transform tail;
    private Transform fins;
    private float routeRadius;
    private float routeSpeed;
    private float schoolPhase;
    private Vector3 localFormationOffset;

    private void Start()
    {
        baseScale = transform.localScale;
        velocity = transform.right * speed;
        tail = transform.Find("Tail");
        fins = transform.Find("Dorsal and pectoral fins");
        routeRadius = radius * 0.70f;
        routeSpeed = 2.5f;
        schoolPhase = schoolCenter.x * 0.173f + schoolCenter.z * 0.071f;
        float initialClock = Time.time * 0.11f * routeSpeed + schoolPhase;
        Vector3 initialCourse = SchoolVelocity(initialClock).normalized;
        Quaternion initialFormationRotation = Quaternion.LookRotation(initialCourse, Vector3.up);
        Vector3 initialOffset = formationOffset.sqrMagnitude > 0.001f ? formationOffset : transform.position - schoolCenter;
        localFormationOffset = Quaternion.Inverse(initialFormationRotation) * initialOffset;
    }

    private void Update()
    {
        SwimStep(Time.time, Time.deltaTime);
    }

    private void SwimStep(float time, float deltaTime)
    {
        float groupClock = time * 0.11f * routeSpeed + schoolPhase;
        Vector3 routeVelocity = SchoolVelocity(groupClock);
        Vector3 course = routeVelocity.normalized;
        Quaternion formationRotation = Quaternion.LookRotation(course, Vector3.up);
        Vector3 movingCenter = schoolCenter + new Vector3(
            Mathf.Sin(groupClock) * routeRadius,
            Mathf.Sin(groupClock * 0.63f + schoolPhase) * radius * 0.08f,
            Mathf.Cos(groupClock * 0.82f) * routeRadius * 0.82f);
        Vector3 personalCurrent = new(
            Mathf.Sin(time * 0.42f + phase) * radius * 0.022f,
            Mathf.Sin(time * 0.31f + phase * 1.7f) * radius * 0.018f,
            Mathf.Cos(time * 0.37f + phase * 0.8f) * radius * 0.022f);
        Vector3 target = movingCenter + formationRotation * localFormationOffset * 0.56f + personalCurrent;
        Vector3 toTarget = target - transform.position;
        float currentNoise = Mathf.PerlinNoise(phase * 0.21f + 7.3f, time * 0.075f);
        float burst = Mathf.Lerp(0.82f, 1.16f, currentNoise);
        // Follow the actual travelling route, rather than swimming against a
        // nearly stationary formation target that cancels the forward velocity.
        Vector3 courseVelocity = routeVelocity * burst;
        Vector3 formationCorrection = Vector3.ClampMagnitude(toTarget * 0.84f, speed * 0.94f);
        Vector3 desiredVelocity = Vector3.ClampMagnitude(courseVelocity + formationCorrection, speed * 1.35f);
        velocity = Vector3.SmoothDamp(velocity, desiredVelocity, ref steeringVelocity, 0.42f, speed * 1.85f, deltaTime);
        Vector3 beforeMove = transform.position;
        Vector3 desiredPosition = beforeMove + velocity * deltaTime;
        transform.position = UnderwaterCollision.ResolveMove(transform, desiredPosition, 0.055f, 0.13f, 0.11f, out bool avoidedObstacle);
        if (avoidedObstacle)
        {
            velocity = (transform.position - beforeMove) / Mathf.Max(deltaTime, 0.0001f);
            velocity += (Vector3.up * 0.34f + transform.right * Mathf.Sign(Mathf.Sin(phase)) * 0.42f) * speed;
        }
        if (velocity.sqrMagnitude > 0.01f)
        {
            float bank = Mathf.Clamp(Vector3.SignedAngle(transform.right, velocity.normalized, Vector3.up) * -0.42f, -18f, 18f);
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up) * Quaternion.Euler(0f, -90f, bank);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-deltaTime * 2.75f));
        }
        transform.localScale = baseScale;
        float beat = time * (7.2f + speed * 1.35f) + phase;
        if (tail != null)
        {
            tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(beat) * 25f + Mathf.Sin(beat * 2f) * 3.5f, 0f);
        }
        if (fins != null)
        {
            fins.localRotation = Quaternion.Euler(Mathf.Sin(beat - 0.8f) * 5.5f, 0f, Mathf.Sin(beat * 0.5f + phase) * 2.2f);
        }
    }

    private Vector3 SchoolVelocity(float clock)
    {
        float angularSpeed = 0.11f * routeSpeed;
        return new Vector3(
            Mathf.Cos(clock) * routeRadius,
            Mathf.Cos(clock * 0.63f + schoolPhase) * radius * 0.08f * 0.63f,
            -Mathf.Sin(clock * 0.82f) * routeRadius * 0.82f * 0.82f) * angularSpeed;
    }
}

public sealed class ReefFishDrift : MonoBehaviour
{
    public Vector3 basePosition;
    public float phase;
    public float swimScale = 1f;
    public bool isBlueTang;
    public float collisionRadius = 0.24f;
    public float floorClearance = 0.38f;
    public float noseDistance = 0.48f;
    private Vector3 baseScale;
    private Vector3 motionVelocity;
    private Vector3 releaseVelocity;
    private float releaseGlide;
    private float travel;

    private void Start()
    {
        baseScale = transform.localScale;
        travel = phase;
        basePosition = transform.position - OrbitOffset();
        motionVelocity = -transform.forward * Mathf.Lerp(1.8f, 3.2f, swimScale);
    }

    private void Update()
    {
        if (releaseGlide > 0f)
        {
            releaseGlide = Mathf.Max(0f, releaseGlide - Time.deltaTime);
            float naturalSpeed = Mathf.Lerp(1.6f, 3.8f, swimScale);
            Vector3 glideDirection = releaseVelocity.sqrMagnitude > 0.001f ? releaseVelocity.normalized : -transform.forward;
            releaseVelocity = Vector3.MoveTowards(releaseVelocity, glideDirection * naturalSpeed, 1.8f * Time.deltaTime);
            Vector3 beforeMove = transform.position;
            Vector3 desiredPosition = beforeMove + releaseVelocity * Time.deltaTime;
            transform.position = UnderwaterCollision.ResolveMove(transform, desiredPosition, collisionRadius, floorClearance, noseDistance, out bool avoidedObstacle);
            if (avoidedObstacle)
            {
                releaseVelocity = (transform.position - beforeMove) / Mathf.Max(Time.deltaTime, 0.0001f);
            }
            basePosition = transform.position - OrbitOffset();
            motionVelocity = releaseVelocity;
            ApplySwimRotation(releaseVelocity, 0f, 5.5f);
            transform.localScale = baseScale;
            return;
        }

        float burst = isBlueTang
            ? 0.88f + Mathf.Sin(Time.time * 0.74f + phase * 1.7f) * 0.13f
            : 0.76f + Mathf.Max(0f, Mathf.Sin(Time.time * 1.18f + phase * 1.7f)) * 0.34f;
        travel += Time.deltaTime * (isBlueTang ? 0.43f : 0.58f) * burst;
        Vector3 target = basePosition + OrbitOffset();
        Vector3 beforeDrift = transform.position;
        float smoothTime = isBlueTang ? 0.32f : 0.24f;
        float maxSpeed = isBlueTang ? Mathf.Lerp(2.5f, 4.8f, swimScale) : 5.8f * swimScale;
        Vector3 desiredDrift = Vector3.SmoothDamp(beforeDrift, target, ref motionVelocity, smoothTime, maxSpeed, Time.deltaTime);
        transform.position = UnderwaterCollision.ResolveMove(transform, desiredDrift, collisionRadius, floorClearance, noseDistance, out bool driftAvoided);
        if (driftAvoided)
        {
            motionVelocity = (transform.position - beforeDrift) / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        Vector3 currentHeading = -transform.forward;
        float turnAngle = motionVelocity.sqrMagnitude > 0.01f
            ? Vector3.SignedAngle(currentHeading, motionVelocity.normalized, Vector3.up)
            : 0f;
        float finRhythm = Mathf.Sin(Time.time * (isBlueTang ? 5.8f : 7.2f) + phase) * (isBlueTang ? 0.28f : 1.1f);
        float bank = Mathf.Clamp(-turnAngle * (isBlueTang ? 0.20f : 0.28f) + finRhythm, isBlueTang ? -5.5f : -7f, isBlueTang ? 5.5f : 7f);
        ApplySwimRotation(motionVelocity, bank, isBlueTang ? 3.45f : 4f);
        transform.localScale = baseScale;
    }

    private Vector3 OrbitOffset()
    {
        if (isBlueTang)
        {
            return new Vector3(
                Mathf.Sin(travel) * 3.4f * swimScale,
                Mathf.Sin(travel * 0.54f + phase) * 0.31f * swimScale,
                Mathf.Cos(travel) * 2.25f * swimScale);
        }
        return new Vector3(
            Mathf.Sin(travel) * 2.8f * swimScale,
            Mathf.Sin(travel * 1.52f + 0.8f) * 0.46f * swimScale,
            Mathf.Cos(travel * 0.78f + 1.4f) * 1.35f * swimScale);
    }

    public Quaternion RotationForVelocity(Vector3 velocity, float bank)
    {
        Vector3 direction = velocity.sqrMagnitude > 0.001f ? velocity.normalized : transform.forward;
        return Quaternion.AngleAxis(bank, direction) * Quaternion.LookRotation(-direction, Vector3.up);
    }

    public void ResumeFromGuidance(Vector3 velocity)
    {
        releaseVelocity = velocity.sqrMagnitude > 0.01f ? Vector3.ClampMagnitude(velocity, 8.5f) : -transform.forward * 2.2f;
        releaseGlide = 2.2f;
        basePosition = transform.position - OrbitOffset();
        motionVelocity = releaseVelocity;
    }

    private void ApplySwimRotation(Vector3 velocity, float bank, float sharpness)
    {
        if (velocity.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = RotationForVelocity(velocity, bank);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-Time.deltaTime * sharpness));
    }
}

public sealed class BlueTangSwimAnimator : MonoBehaviour
{
    public float phase;
    private Mesh animatedMesh;
    private Vector3[] restVertices;
    private Vector3[] deformedVertices;
    private float minimumZ;
    private float length;

    private void Start()
    {
        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
        {
            enabled = false;
            return;
        }

        try
        {
            animatedMesh = Instantiate(filter.sharedMesh);
            animatedMesh.name = $"{filter.sharedMesh.name} swimming";
            animatedMesh.MarkDynamic();
            restVertices = animatedMesh.vertices;
            deformedVertices = new Vector3[restVertices.Length];
            filter.sharedMesh = animatedMesh;
        }
        catch (UnityException)
        {
            enabled = false;
            return;
        }

        Bounds bounds = animatedMesh.bounds;
        minimumZ = bounds.min.z;
        length = Mathf.Max(0.001f, bounds.size.z);
        bounds.Expand(new Vector3(bounds.size.x * 0.20f, bounds.size.y * 0.12f, 0f));
        animatedMesh.bounds = bounds;
    }

    private void LateUpdate()
    {
        if (animatedMesh == null || restVertices == null)
        {
            return;
        }

        if ((Time.frameCount & 1) != 0)
        {
            return;
        }

        float cycle = Time.time * 6.1f + phase;
        for (int i = 0; i < restVertices.Length; i++)
        {
            Vector3 rest = restVertices[i];
            float alongBody = Mathf.Clamp01((rest.z - minimumZ) / length);
            float tailMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.52f, 1f, alongBody));
            float wave = Mathf.Sin(cycle - tailMask * 1.48f) + Mathf.Sin(cycle * 2f - tailMask * 0.72f) * 0.10f;
            rest.x += wave * tailMask * tailMask * length * 0.062f;
            rest.y += Mathf.Sin(cycle - 1.15f - tailMask) * tailMask * tailMask * length * 0.006f;
            deformedVertices[i] = rest;
        }

        animatedMesh.vertices = deformedVertices;
    }

    private void OnDestroy()
    {
        if (animatedMesh != null)
        {
            Destroy(animatedMesh);
        }
    }
}

public sealed class HeroWhaleDrift : MonoBehaviour
{
    public Vector3 basePosition;
    private Vector3 motionVelocity;
    private Vector3 heading;
    private float courseClock;

    private void Start()
    {
        basePosition = transform.position;
        heading = transform.forward;
        courseClock = 0.45f;
        motionVelocity = heading * 3f;
    }

    private void Update()
    {
        float tailBeat = Mathf.Sin(Time.time * 2.18f - 0.72f);
        float propulsion = 0.92f + Mathf.Max(0f, tailBeat) * 0.12f;
        courseClock += Time.deltaTime * 0.065f;
        // A travelling course, with slow steering rather than a fixed orbit.
        float turn = Mathf.Sin(courseClock) * 2.4f;
        heading = Quaternion.AngleAxis(turn * Time.deltaTime, Vector3.up) * heading;
        float floor = UnderwaterWorld.SampleSeafloorHeight(transform.position.x, transform.position.z) + 5.5f;
        heading.y = Mathf.Clamp((Mathf.Max(basePosition.y, floor) - transform.position.y) * 0.12f, -0.12f, 0.18f);
        heading.Normalize();
        motionVelocity = Vector3.Lerp(motionVelocity, heading * (3f * propulsion), 1f - Mathf.Exp(-Time.deltaTime * 0.85f));
        Vector3 beforeMove = transform.position;
        Vector3 desiredPosition = beforeMove + motionVelocity * Time.deltaTime;
        transform.position = UnderwaterCollision.ResolveMove(transform, desiredPosition, 1.45f, 2.1f, 4f, out bool avoidedObstacle);
        if (avoidedObstacle)
        {
            motionVelocity = (transform.position - beforeMove) / Mathf.Max(Time.deltaTime, 0.0001f);
            if (motionVelocity.sqrMagnitude > 0.01f) heading = motionVelocity.normalized;
        }

        if (motionVelocity.sqrMagnitude > 0.01f)
        {
            Vector3 direction = motionVelocity.normalized;
            float bank = Mathf.Clamp(-motionVelocity.x * 1.6f, -7f, 7f);
            Quaternion targetRotation = Quaternion.AngleAxis(bank, direction)
                * Quaternion.LookRotation(direction, Vector3.up)
                * Quaternion.Euler(tailBeat * 0.85f, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-Time.deltaTime * 2.4f));
        }
    }

    public void ResumeFromGuidance(Vector3 velocity)
    {
        heading = transform.forward;
        motionVelocity = heading * Mathf.Clamp(velocity.magnitude, 2.4f, 5f);
        basePosition = transform.position;
    }
}

public sealed class WhaleBodyAnimator : MonoBehaviour
{
    private Transform body;
    private Transform tailBase;
    private Transform tailTip;
    private Quaternion bodyRest;
    private Quaternion tailBaseRest;
    private Quaternion tailTipRest;

    private void Start()
    {
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            if (child.name == "WhaleBody") body = child;
            if (child.name == "WhaleTailBase") tailBase = child;
            if (child.name == "WhaleTailTip") tailTip = child;
        }

        if (body == null || tailBase == null || tailTip == null)
        {
            enabled = false;
            return;
        }

        bodyRest = body.localRotation;
        tailBaseRest = tailBase.localRotation;
        tailTipRest = tailTip.localRotation;
    }

    private void LateUpdate()
    {
        float cycle = Time.time * 2.18f;
        float secondHarmonic = Mathf.Sin(cycle * 2f - 0.35f);
        float bodyWave = Mathf.Sin(cycle) * 1.8f + secondHarmonic * 0.28f;
        float tailWave = Mathf.Sin(cycle - 0.72f) * 6.8f + secondHarmonic * 0.75f;
        float tipWave = Mathf.Sin(cycle - 1.38f) * 14f + secondHarmonic * 1.35f;
        float bodyRoll = Mathf.Sin(cycle * 0.5f + 0.7f) * 0.55f;

        body.localRotation = bodyRest * Quaternion.Euler(bodyWave, 0f, bodyRoll);
        tailBase.localRotation = tailBaseRest * Quaternion.Euler(tailWave, 0f, -bodyWave * 0.12f);
        tailTip.localRotation = tailTipRest * Quaternion.Euler(tipWave, 0f, -tailWave * 0.08f);
    }
}

public sealed class OceanCreatureDrift : MonoBehaviour
{
    public Vector3 basePosition;
    public float phase;
    public bool useMantaAxes;
    private Vector3 motionVelocity;
    private Vector3 releaseVelocity;
    private bool continuingFromGuidance;
    private float travel;
    private TurtleStrokeAnimator turtleStroke;
    private bool entangled;
    private bool awaitingGuidance;
    private Vector3 entangledOrigin;
    private float struggleClock;

    private void Start()
    {
        basePosition = transform.position;
        travel = phase;
        turtleStroke = GetComponent<TurtleStrokeAnimator>();
    }

    private void Update()
    {
        if (entangled || awaitingGuidance)
        {
            UpdateEntangled(Time.deltaTime);
            return;
        }
        if (continuingFromGuidance)
        {
            ContinueFromGuidance(Time.deltaTime);
            return;
        }

        float strokeBoost = turtleStroke == null
            ? 0.20f + Mathf.Sin(Time.time * 0.72f + phase) * 0.045f
            : 0.135f + turtleStroke.Propulsion * 0.21f;
        travel += Time.deltaTime * strokeBoost;
        Vector3 target = basePosition + OrbitOffset();
        float maxSpeed = turtleStroke == null ? 5.6f : Mathf.Lerp(2.8f, 6.2f, turtleStroke.Propulsion);
        Vector3 beforeDrift = transform.position;
        Vector3 desiredDrift = Vector3.SmoothDamp(beforeDrift, target, ref motionVelocity, 0.46f, maxSpeed, Time.deltaTime);
        float driftRadius = useMantaAxes ? 0.92f : 0.62f;
        float driftClearance = useMantaAxes ? 1.15f : 0.78f;
        float driftNoseDistance = useMantaAxes ? 2.1f : 1.55f;
        transform.position = UnderwaterCollision.ResolveMove(transform, desiredDrift, driftRadius, driftClearance, driftNoseDistance, out bool driftAvoided);
        if (driftAvoided)
        {
            motionVelocity = (transform.position - beforeDrift) / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        float turn = Mathf.Cos(travel) * (turtleStroke == null ? 13f : 9f);
        float bank = Mathf.Clamp(-turn * (turtleStroke == null ? 0.28f : 0.18f), -6f, 6f);
        ApplySwimRotation(motionVelocity, bank, 2.7f);
    }

    public void SetEntangled(bool value)
    {
        awaitingGuidance = false;
        if (entangled == value) return;
        entangled = value;
        if (value)
        {
            entangledOrigin = transform.position;
            struggleClock = 0f;
            enabled = true;
        }
        else
        {
            basePosition = transform.position - OrbitOffset();
            motionVelocity = Vector3.zero;
            if (continuingFromGuidance) ResumeFromGuidance(releaseVelocity);
        }
    }

    public void WaitForGuidance()
    {
        // Keep paddling and floating nearby while the child reads the next step.
        awaitingGuidance = true;
        entangledOrigin = transform.position;
        struggleClock = 0f;
        enabled = true;
    }

    private void UpdateEntangled(float deltaTime)
    {
        struggleClock += deltaTime;
        Vector3 struggle = new(
            Mathf.Sin(struggleClock * 1.35f) * 0.08f,
            Mathf.Sin(struggleClock * 1.85f) * 0.09f,
            (Mathf.Cos(struggleClock * 0.95f) - 1f) * 0.06f);
        transform.position = UnderwaterCollision.ResolveMove(transform, entangledOrigin + struggle,
            useMantaAxes ? 0.92f : 0.62f, useMantaAxes ? 1.15f : 0.78f, useMantaAxes ? 2.1f : 1.55f, out _);
    }

    private Vector3 OrbitOffset()
    {
        return OrbitOffsetAt(travel);
    }

    public Quaternion RotationForVelocity(Vector3 velocity, float bank)
    {
        Vector3 fallback = useMantaAxes ? transform.up : transform.forward;
        Vector3 direction = ClampSwimDirection(velocity.sqrMagnitude > 0.001f ? velocity : fallback);
        if (useMantaAxes)
        {
            Quaternion alignForwardAndDorsal = Quaternion.LookRotation(Vector3.right, Vector3.forward);
            return Quaternion.AngleAxis(bank, direction)
                * Quaternion.LookRotation(direction, Vector3.up)
                * alignForwardAndDorsal;
        }
        return Quaternion.AngleAxis(bank, direction) * Quaternion.LookRotation(direction, Vector3.up);
    }

    private Vector3 ClampSwimDirection(Vector3 heading)
    {
        Vector3 fallback = useMantaAxes ? transform.up : transform.forward;
        Vector3 direction = heading.normalized;
        // Protect the dorsal-up posture when a drag approaches the camera's
        // vertical axis. A near-vertical LookRotation can suddenly roll over.
        Vector3 horizontal = Vector3.ProjectOnPlane(direction, Vector3.up);
        if (horizontal.sqrMagnitude < 0.001f) horizontal = Vector3.ProjectOnPlane(fallback, Vector3.up);
        if (horizontal.sqrMagnitude < 0.001f) horizontal = Vector3.forward;
        float pitch = Mathf.Clamp(direction.y, -0.30f, 0.30f);
        return horizontal.normalized * Mathf.Sqrt(1f - pitch * pitch) + Vector3.up * pitch;
    }

    public void ResumeFromGuidance(Vector3 velocity)
    {
        Vector3 fallback = useMantaAxes ? transform.up : transform.forward;
        awaitingGuidance = false;
        // The animal leaves a drag in its last visible heading; the final
        // pointer correction can otherwise send it sharply head-down.
        releaseVelocity = ClampSwimDirection(fallback) * Mathf.Clamp(velocity.magnitude, 2.6f, 6f);
        continuingFromGuidance = true;
        basePosition = transform.position - OrbitOffset();
        motionVelocity = releaseVelocity;
    }

    private void ContinueFromGuidance(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        float naturalSpeed = turtleStroke == null ? 2.8f : 2.4f;
        Vector3 heading = releaseVelocity.sqrMagnitude > 0.001f ? releaseVelocity : useMantaAxes ? transform.up : transform.forward;
        heading = ClampSwimDirection(heading);
        releaseVelocity = Vector3.MoveTowards(releaseVelocity, heading * naturalSpeed, 1.35f * deltaTime);
        Vector3 before = transform.position;
        Vector3 requested = releaseVelocity * deltaTime;
        float radius = useMantaAxes ? 0.92f : 0.62f;
        float clearance = useMantaAxes ? 1.15f : 0.78f;
        float nose = useMantaAxes ? 2.1f : 1.55f;
        Vector3 next = UnderwaterCollision.ResolveMove(transform, before + requested, radius, clearance, nose, out bool adjusted);
        Vector3 displacement = next - before;
        float horizontalProgress = Vector3.ProjectOnPlane(displacement, Vector3.up).sqrMagnitude;
        float minimumProgress = Vector3.ProjectOnPlane(requested, Vector3.up).sqrMagnitude * 0.0625f;
        if (adjusted && horizontalProgress < minimumProgress)
        {
            // Test the same collision-safe movement on either side. Do not
            // retain a zero velocity that would repeatedly face into the wall.
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 2 + 1) * 45f * ((i & 1) == 0 ? 1f : -1f);
                Vector3 candidateHeading = Quaternion.AngleAxis(angle, Vector3.up) * heading;
                Vector3 candidate = UnderwaterCollision.ResolveMove(transform,
                    before + candidateHeading * naturalSpeed * deltaTime, radius, clearance, nose, out _);
                float progress = Vector3.ProjectOnPlane(candidate - before, Vector3.up).sqrMagnitude;
                if (progress <= horizontalProgress) continue;
                next = candidate;
                displacement = next - before;
                horizontalProgress = progress;
                if (progress >= naturalSpeed * naturalSpeed * deltaTime * deltaTime * 0.25f) break;
            }
        }
        transform.position = next;
        if (adjusted && displacement.sqrMagnitude > 0.00000001f)
            releaseVelocity = ClampSwimDirection(displacement) * naturalSpeed;
        motionVelocity = releaseVelocity;
        basePosition = transform.position - OrbitOffset();
        Quaternion rotation = RotationForVelocity(releaseVelocity, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, 1f - Mathf.Exp(-deltaTime * 4.8f));
    }

    private Vector3 OrbitOffsetAt(float t)
    {
        float radius = turtleStroke == null ? 6.4f : 4.8f;
        return new Vector3(
            Mathf.Sin(t) * radius,
            Mathf.Sin(t * 1.22f + 0.6f) * (turtleStroke == null ? 1.2f : 0.82f),
            Mathf.Cos(t * 0.74f) * radius * 0.68f);
    }

    private void ApplySwimRotation(Vector3 velocity, float bank, float sharpness)
    {
        if (velocity.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = RotationForVelocity(velocity, bank);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-Time.deltaTime * sharpness));
    }
}

public sealed class TurtleStrokeAnimator : MonoBehaviour
{
    public float phase;
    private Transform leftFlipper;
    private Transform rightFlipper;
    private Quaternion leftRest;
    private Quaternion rightRest;
    public float Propulsion { get; private set; }

    private void Start()
    {
        Transform[] hierarchy = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in hierarchy)
        {
            if (child.name == "FrontFlipper_L") leftFlipper = child;
            if (child.name == "FrontFlipper_R") rightFlipper = child;
        }

        if (leftFlipper == null || rightFlipper == null)
        {
            enabled = false;
            return;
        }

        leftRest = leftFlipper.localRotation;
        rightRest = rightFlipper.localRotation;
    }

    private void Update()
    {
        float cycle = Mathf.Repeat(Time.time * 0.78f + phase, 1f);
        float stroke;
        if (cycle < 0.36f)
        {
            stroke = Mathf.SmoothStep(22f, -38f, cycle / 0.36f);
            Propulsion = Mathf.Sin(cycle / 0.36f * Mathf.PI);
        }
        else
        {
            stroke = Mathf.SmoothStep(-38f, 22f, (cycle - 0.36f) / 0.64f);
            Propulsion = 0f;
        }

        float feather = Mathf.Sin((cycle + 0.12f) * Mathf.PI * 2f) * 9f;
        leftFlipper.localRotation = leftRest * Quaternion.Euler(0f, stroke, feather);
        rightFlipper.localRotation = rightRest * Quaternion.Euler(0f, -stroke * 0.94f, -feather * 0.92f);
    }
}

public sealed class MantaWingAnimator : MonoBehaviour
{
    public float phase;
    private Mesh animatedMesh;
    private Vector3[] restVertices;
    private Vector3[] deformedVertices;
    private Vector3[] restNormals;
    private Vector3[] deformedNormals;
    private Vector4[] restTangents;
    private Vector4[] deformedTangents;
    private Bounds expandedBounds;
    private float halfSpan;

    private void Start()
    {
        MeshFilter filter = GetComponentInChildren<MeshFilter>();
        if (filter == null || filter.sharedMesh == null)
        {
            enabled = false;
            return;
        }

        try
        {
            animatedMesh = Instantiate(filter.sharedMesh);
            animatedMesh.name = $"{filter.sharedMesh.name} wing stroke";
            animatedMesh.MarkDynamic();
            restVertices = animatedMesh.vertices;
            deformedVertices = new Vector3[restVertices.Length];
            restNormals = animatedMesh.normals;
            deformedNormals = new Vector3[restNormals.Length];
            restTangents = animatedMesh.tangents;
            deformedTangents = new Vector4[restTangents.Length];
            filter.sharedMesh = animatedMesh;
        }
        catch (UnityException)
        {
            enabled = false;
            return;
        }

        expandedBounds = animatedMesh.bounds;
        halfSpan = Mathf.Max(Mathf.Abs(expandedBounds.min.z), Mathf.Abs(expandedBounds.max.z));
        expandedBounds.Expand(new Vector3(expandedBounds.size.x * 0.32f, 0f, 0f));
        animatedMesh.bounds = expandedBounds;
    }

    private void LateUpdate()
    {
        if (animatedMesh == null || restVertices == null || ((Time.frameCount + Mathf.RoundToInt(phase * 10f)) & 1) != 0)
        {
            return;
        }

        float cycle = Time.time * 1.72f + phase;
        for (int i = 0; i < restVertices.Length; i++)
        {
            Vector3 rest = restVertices[i];
            float wingDistance = Mathf.Clamp01(Mathf.Abs(rest.z) / Mathf.Max(0.001f, halfSpan));
            float wingMask = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.22f, 1f, wingDistance));
            float sidePhase = Mathf.Sign(rest.z) * 0.08f;
            float broadStroke = Mathf.Sin(cycle - wingDistance * 0.82f + sidePhase);
            float recovery = Mathf.Sin(cycle * 2f - wingDistance * 0.35f) * 0.12f;
            rest.x += (broadStroke + recovery) * Mathf.Pow(wingMask, 1.25f) * halfSpan * 0.115f;
            deformedVertices[i] = rest;
            float blend = Mathf.InverseLerp(0.22f, 1f, wingDistance);
            float maskDerivative = 6f * blend * (1f - blend) / 0.78f;
            float waveDerivative = -0.82f * Mathf.Cos(cycle - wingDistance * 0.82f + sidePhase)
                - 0.042f * Mathf.Cos(cycle * 2f - wingDistance * 0.35f);
            float displacementDerivative = Mathf.Sign(rest.z) * 0.115f * (
                waveDerivative * Mathf.Pow(wingMask, 1.25f)
                + (broadStroke + recovery) * 1.25f * Mathf.Pow(wingMask, 0.25f) * maskDerivative);
            if (i < restNormals.Length)
            {
                Vector3 normal = restNormals[i];
                normal.z -= displacementDerivative * normal.x;
                deformedNormals[i] = normal.normalized;
            }
            if (i < restTangents.Length)
            {
                Vector4 tangent = restTangents[i];
                Vector3 direction = new Vector3(tangent.x + displacementDerivative * tangent.z, tangent.y, tangent.z).normalized;
                deformedTangents[i] = new Vector4(direction.x, direction.y, direction.z, tangent.w);
            }
        }

        animatedMesh.vertices = deformedVertices;
        if (deformedNormals.Length == deformedVertices.Length) animatedMesh.normals = deformedNormals;
        if (deformedTangents.Length == deformedVertices.Length) animatedMesh.tangents = deformedTangents;
        animatedMesh.bounds = expandedBounds;
    }

    private void OnDestroy()
    {
        if (animatedMesh != null)
        {
            Destroy(animatedMesh);
        }
    }
}

public sealed class JellyPulseAnimator : MonoBehaviour
{
    public float phase;
    private Transform bell;
    private Transform upperTentacles;
    private Transform middleTentacles;
    private Transform tipTentacles;
    private Vector3 bellRestScale;
    private Quaternion upperRest;
    private Quaternion middleRest;
    private Quaternion tipRest;
    private float previousContraction;
    public float PropulsionPulse { get; private set; }

    private void Start()
    {
        Transform[] hierarchy = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in hierarchy)
        {
            if (child.name == "Bell") bell = child;
            if (child.name == "TentacleUpper") upperTentacles = child;
            if (child.name == "TentacleMiddle") middleTentacles = child;
            if (child.name == "TentacleTip") tipTentacles = child;
        }

        if (bell == null || upperTentacles == null || middleTentacles == null || tipTentacles == null)
        {
            enabled = false;
            return;
        }

        bellRestScale = bell.localScale;
        upperRest = upperTentacles.localRotation;
        middleRest = middleTentacles.localRotation;
        tipRest = tipTentacles.localRotation;
    }

    private void Update()
    {
        float cycle = Mathf.Repeat(Time.time * 0.48f + phase, 1f);
        float contraction = cycle < 0.24f
            ? Mathf.SmoothStep(0f, 1f, cycle / 0.24f)
            : Mathf.SmoothStep(1f, 0f, (cycle - 0.24f) / 0.76f);
        PropulsionPulse = Mathf.Max(0f, contraction - previousContraction) / Mathf.Max(Time.deltaTime, 0.0001f);
        previousContraction = contraction;

        float radial = 1f - contraction * 0.13f;
        float height = 1f + contraction * 0.10f;
        bell.localScale = Vector3.Scale(bellRestScale, new Vector3(radial, height, radial));

        float currentPhase = phase * Mathf.PI * 2f;
        float slowCurrent = Mathf.Sin(Time.time * 0.72f + currentPhase);
        float crossCurrent = Mathf.Sin(Time.time * 0.53f + currentPhase * 1.37f + 1.2f);
        float upperLag = Mathf.Sin((cycle - 0.08f) * Mathf.PI * 2f);
        float middleLag = Mathf.Sin((cycle - 0.18f) * Mathf.PI * 2f);
        float tipLag = Mathf.Sin((cycle - 0.31f) * Mathf.PI * 2f);

        upperTentacles.localRotation = upperRest * Quaternion.Euler(
            upperLag * 4.5f + slowCurrent * 3.5f,
            crossCurrent * 2.2f,
            upperLag * 3.5f + crossCurrent * 4f);
        middleTentacles.localRotation = middleRest * Quaternion.Euler(
            middleLag * 8f + slowCurrent * 6f,
            crossCurrent * 4.5f,
            -middleLag * 6f + crossCurrent * 7f);
        tipTentacles.localRotation = tipRest * Quaternion.Euler(
            tipLag * 14f + slowCurrent * 10f,
            crossCurrent * 7f,
            -tipLag * 11f + crossCurrent * 12f);
    }
}

public sealed class JellyDrift : MonoBehaviour
{
    public float phase;
    public float uprightBias = 0.9f;
    private Vector3 origin;
    private Vector3 baseScale;
    private Vector3 velocity;
    private float restYaw;
    private JellyPulseAnimator pulseAnimator;

    private void Start()
    {
        origin = transform.position;
        baseScale = transform.localScale;
        restYaw = transform.eulerAngles.y;
        pulseAnimator = GetComponent<JellyPulseAnimator>();
    }

    private void Update()
    {
        float t = Time.time * 0.34f + phase;
        float lateralStrength = Mathf.Lerp(0.15f, 0.07f, uprightBias);
        Vector3 current = new(
            Mathf.Sin(t * 0.73f + phase) * lateralStrength,
            Mathf.Sin(t * 0.58f + phase * 0.7f) * 0.075f,
            Mathf.Cos(t * 0.61f) * lateralStrength * 0.86f);
        Vector3 restoring = (origin - transform.position) * 0.075f;
        float pulseThrust = pulseAnimator == null ? Mathf.Max(0f, Mathf.Sin(t * 2.4f)) * 0.16f : pulseAnimator.PropulsionPulse * 0.035f;
        velocity += (current + restoring + Vector3.up * pulseThrust) * Time.deltaTime;
        velocity *= Mathf.Exp(-Time.deltaTime * 0.82f);
        Vector3 beforeMove = transform.position;
        Vector3 desiredPosition = beforeMove + velocity * Time.deltaTime;
        transform.position = UnderwaterCollision.ResolveMove(transform, desiredPosition, 0.18f, 0.38f, 0.22f, out bool avoidedObstacle);
        if (avoidedObstacle)
        {
            velocity = (transform.position - beforeMove) / Mathf.Max(Time.deltaTime, 0.0001f);
        }

        Vector3 sideways = new(velocity.x, 0f, velocity.z);
        float tiltStrength = Mathf.Lerp(1.65f, 0.38f, uprightBias);
        Vector3 desiredUp = (Vector3.up + sideways * tiltStrength).normalized;
        Quaternion gentleTurn = Quaternion.AngleAxis(Mathf.Sin(t * 0.31f + phase) * 12f, Vector3.up);
        Quaternion targetRotation = Quaternion.FromToRotation(Vector3.up, desiredUp)
            * Quaternion.Euler(0f, restYaw, 0f)
            * gentleTurn;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-Time.deltaTime * 1.15f));
        transform.localScale = baseScale;
    }
}

public sealed class SwayingPlant : MonoBehaviour
{
    public float speed = 0.8f;
    public float amount = 5f;
    public float phase;
    private Quaternion origin;

    private void Start() => origin = transform.localRotation;

    private void Update()
    {
        float sway = Mathf.Sin(Time.time * speed + phase) * amount;
        transform.localRotation = origin * Quaternion.Euler(sway, sway * 0.25f, sway * 0.35f);
    }
}

public sealed class SoftPulse : MonoBehaviour
{
    public float speed = 0.25f;
    private Vector3 origin;
    private Vector3 originPosition;
    private Quaternion originRotation;
    private float phase;

    private void Start()
    {
        origin = transform.localScale;
        originPosition = transform.position;
        originRotation = transform.rotation;
        phase = transform.position.x * 0.19f + transform.position.z * 0.11f;
    }

    private void Update()
    {
        float wave = Time.time * speed + phase;
        float pulse = 0.88f + Mathf.Sin(wave) * 0.12f;
        transform.localScale = new Vector3(origin.x * pulse, origin.y, origin.z * pulse);
        transform.position = originPosition + new Vector3(Mathf.Sin(wave * 0.71f) * 0.7f, 0f, Mathf.Cos(wave * 0.58f) * 0.55f);
        transform.rotation = originRotation * Quaternion.Euler(Mathf.Sin(wave * 0.43f) * 0.7f, Mathf.Sin(wave * 0.31f) * 1.8f, 0f);
    }
}

public sealed class SurfaceDrift : MonoBehaviour
{
    private Vector3 origin;
    private void Start() => origin = transform.position;
    private void Update() => transform.position = origin + new Vector3(Mathf.Sin(Time.time * 0.17f) * 0.4f, Mathf.Sin(Time.time * 0.33f) * 0.08f, Mathf.Cos(Time.time * 0.13f) * 0.35f);
}

public sealed class UnderwaterAudio : MonoBehaviour
{
    private const int SampleRate = 22050;
    public static bool GlobalMuted { get; private set; }
    private AudioSource ambienceSource;
    private AudioSource movementSource;
    private AudioSource whaleSource;
    private AudioClip ambienceClip;
    private AudioClip movementClip;
    private AudioClip whaleClip;
    private CinematicDive diver;
    private bool started;
    private bool muted;
    private float nextWhaleCall;

    private void Start()
    {
        diver = Camera.main == null ? null : Camera.main.GetComponent<CinematicDive>();
        ambienceClip = CreateAmbientClip();
        movementClip = CreateMovementClip();
        whaleClip = CreateWhaleClip();

        ambienceSource = CreateSource("Deep ocean ambience", ambienceClip, true, 0.30f);
        movementSource = CreateSource("Diver water movement", movementClip, true, 0f);

        HeroWhaleDrift whale = FindFirstObjectByType<HeroWhaleDrift>();
        GameObject emitter = new("Distant whale voice");
        if (whale != null)
        {
            emitter.transform.SetParent(whale.transform, false);
        }
        whaleSource = emitter.AddComponent<AudioSource>();
        whaleSource.clip = whaleClip;
        whaleSource.playOnAwake = false;
        whaleSource.spatialBlend = 0.82f;
        whaleSource.minDistance = 9f;
        whaleSource.maxDistance = 115f;
        whaleSource.rolloffMode = AudioRolloffMode.Logarithmic;
        whaleSource.volume = 0.58f;
        nextWhaleCall = Time.unscaledTime + 7f;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.mKey.wasPressedThisFrame)
        {
            muted = !muted;
            GlobalMuted = muted;
            ApplyMute();
            UnderwaterHud.Instance?.ShowStatus(muted ? "海洋声音已关闭" : "海洋声音已开启");
        }

        if (!started && HasUserGesture())
        {
            started = true;
            ambienceSource.Play();
            movementSource.Play();
        }

        if (!started)
        {
            return;
        }

        float swim = diver == null ? 0f : diver.SwimSpeed01;
        movementSource.volume = muted ? 0f : Mathf.Lerp(0.015f, 0.26f, swim * swim);
        movementSource.pitch = Mathf.Lerp(0.72f, 1.22f, swim);
        ambienceSource.volume = muted ? 0f : 0.30f;

        if (!muted && Time.unscaledTime >= nextWhaleCall && !whaleSource.isPlaying)
        {
            whaleSource.pitch = Random.Range(0.88f, 1.04f);
            whaleSource.Play();
            nextWhaleCall = Time.unscaledTime + Random.Range(21f, 34f);
        }
    }

    private void OnDestroy()
    {
        GlobalMuted = false;
        if (ambienceClip != null) Destroy(ambienceClip);
        if (movementClip != null) Destroy(movementClip);
        if (whaleClip != null) Destroy(whaleClip);
    }

    private AudioSource CreateSource(string sourceName, AudioClip clip, bool loop, float volume)
    {
        GameObject go = new(sourceName);
        go.transform.SetParent(transform, false);
        AudioSource source = go.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        source.volume = volume;
        return source;
    }

    private void ApplyMute()
    {
        if (ambienceSource != null) ambienceSource.mute = muted;
        if (movementSource != null) movementSource.mute = muted;
        if (whaleSource != null) whaleSource.mute = muted;
    }

    public static bool HasUserGesture()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            return true;
        }
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }
        Touchscreen touch = Touchscreen.current;
        return touch != null && touch.primaryTouch.press.wasPressedThisFrame;
    }

    private static AudioClip CreateAmbientClip()
    {
        int length = SampleRate * 12;
        float[] samples = new float[length];
        System.Random random = new(9073);
        float filteredNoise = 0f;
        for (int i = 0; i < length; i++)
        {
            float time = i / (float)SampleRate;
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.018f);
            float current = Mathf.Sin(time * Mathf.PI * 2f * 31f) * 0.055f
                + Mathf.Sin(time * Mathf.PI * 2f * 47f + 1.4f) * 0.025f;
            float bubble = 0f;
            float bubblePhase = Mathf.Repeat(time * 0.31f, 1f);
            if (bubblePhase < 0.045f)
            {
                float envelope = Mathf.Sin(bubblePhase / 0.045f * Mathf.PI);
                bubble = Mathf.Sin(time * Mathf.PI * 2f * (190f + bubblePhase * 2400f)) * envelope * 0.022f;
            }
            samples[i] = filteredNoise * 0.21f + current + bubble;
        }
        AudioClip clip = AudioClip.Create("Procedural deep ocean ambience", length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateMovementClip()
    {
        int length = SampleRate * 5;
        float[] samples = new float[length];
        System.Random random = new(4412);
        float low = 0f;
        float slower = 0f;
        for (int i = 0; i < length; i++)
        {
            float noise = (float)(random.NextDouble() * 2.0 - 1.0);
            low = Mathf.Lerp(low, noise, 0.10f);
            slower = Mathf.Lerp(slower, noise, 0.018f);
            samples[i] = (low - slower * 0.52f) * 0.34f;
        }
        AudioClip clip = AudioClip.Create("Procedural water movement", length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateWhaleClip()
    {
        int length = SampleRate * 6;
        float[] samples = new float[length];
        float phase = 0f;
        for (int i = 0; i < length; i++)
        {
            float time = i / (float)SampleRate;
            float normalized = time / 6f;
            float frequency = Mathf.Lerp(96f, 42f, Mathf.SmoothStep(0f, 1f, normalized));
            phase += Mathf.PI * 2f * frequency / SampleRate;
            float envelope = Mathf.Sin(Mathf.Clamp01(normalized) * Mathf.PI);
            envelope *= Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - normalized) * 3f));
            float voice = Mathf.Sin(phase) * 0.62f + Mathf.Sin(phase * 0.51f + 0.7f) * 0.28f;
            samples[i] = voice * envelope * 0.34f;
        }
        AudioClip clip = AudioClip.Create("Procedural distant whale call", length, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}

public sealed class UnderwaterHud : MonoBehaviour
{
    public static UnderwaterHud Instance { get; private set; }

    private readonly HashSet<string> discoveries = new();
    private GUIStyle panelTitleStyle;
    private GUIStyle panelTextStyle;
    private GUIStyle toastTitleStyle;
    private GUIStyle toastTextStyle;
    private GUIStyle touchButtonStyle;
    private GUIStyle menuButtonStyle;
    private Texture2D panelTexture;
    private Texture2D softPanelTexture;
    private Texture2D progressTexture;
    private Texture2D travelButtonTexture;
    private Font chineseFont;
    private bool discoveryOpen;
    private float manualTravelDistance;
    private float manualLookPixels;
    private float toastUntil;
    private string toastTitle;
    private string toastMessage;
    private string hoverName;
    private bool hoverDiscovered;
    private Vector2 hoverPointer;

    public string FreeRoamObjective => discoveries.Count > 0 ? "继续探索，认识更多海洋伙伴"
        : manualTravelDistance < 0.65f ? "四处游游，认识海洋伙伴"
        : manualLookPixels < 36f ? "看看四周，找一位海洋伙伴" : "找一位海洋伙伴，点一下认识它";

    public void RecordManualTravel(float distance)
    {
        if (!(OceanMissionDirector.Instance?.IsMenuOpen ?? false)) manualTravelDistance += Mathf.Max(0f, distance);
    }

    public void RecordManualLook(float pixels) => manualLookPixels += Mathf.Max(0f, pixels);

    private string BuildControlsHint(CinematicDive dive)
    {
        string missionHint = OceanMissionDirector.Instance?.ActionHint;
        if (!string.IsNullOrEmpty(missionHint)) return missionHint;
        if (dive != null && dive.IsGuidingAnimal) return "松开后，伙伴会沿这个方向继续游";
        if (dive != null && dive.GuidedEncounter != null) return $"按住{dive.GuidedEncounter.DisplayName}，拖动引导它";
        if (!string.IsNullOrEmpty(hoverName)) return hoverDiscovered ? $"双击{hoverName}，选中它" : $"点一下{hoverName}，认识它";
        if (manualTravelDistance < 0.65f) return "按住左侧 ↑，往前游";
        if (manualLookPixels < 36f) return "拖动空白处，看看四周";
        return string.Empty;
    }

    private void Awake()
    {
        Instance = this;
        chineseFont = Resources.Load<Font>("Fonts/BlueLanternUI");
        panelTexture = MakeTexture(new Color(0.012f, 0.075f, 0.11f, 0.82f));
        softPanelTexture = MakeTexture(new Color(0.01f, 0.06f, 0.09f, 0.62f));
        progressTexture = MakeTexture(new Color(0.24f, 0.82f, 0.82f, 0.92f));
        travelButtonTexture = MakeTexture(new Color(0.015f, 0.10f, 0.14f, 0.30f));
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Destroy(panelTexture);
        Destroy(softPanelTexture);
        Destroy(progressTexture);
        Destroy(travelButtonTexture);
    }

    public void Discover(string id, string displayName, string description)
    {
        bool isNew = discoveries.Add(id);
        toastTitle = isNew ? $"新发现 · {displayName}" : $"海洋图鉴 · {displayName}";
        toastMessage = isNew ? description : "这个伙伴已经收录在海洋图鉴里。";
        toastUntil = Time.unscaledTime + 5f;
    }

    public void ShowStatus(string message)
    {
        toastTitle = message;
        toastMessage = string.Empty;
        toastUntil = Time.unscaledTime + 2.4f;
    }

    public void SetHover(string displayName, bool discovered, Vector2 pointerPosition)
    {
        hoverName = displayName;
        hoverDiscovered = discovered;
        hoverPointer = pointerPosition;
    }

    public static bool IsPointerOverTravelControls(Vector2 screenPointer)
    {
        if (OceanMissionDirector.Instance != null && OceanMissionDirector.Instance.IsPointerOverUi(screenPointer))
        {
            return true;
        }
        Vector2 guiPoint = new(screenPointer.x, Screen.height - screenPointer.y);
        if (Instance != null && (GetDiscoveryButtonRect().Contains(guiPoint)
            || Instance.discoveryOpen && GetDiscoveryRect().Contains(guiPoint))) return true;
        foreach (Rect rect in GetTravelControlRects())
        {
            if (rect.Contains(guiPoint))
            {
                return true;
            }
        }
        return false;
    }

    private void OnGUI()
    {
        CinematicDive dive = Camera.main == null ? null : Camera.main.GetComponent<CinematicDive>();
        if (OceanMissionDirector.Instance != null && OceanMissionDirector.Instance.IsMenuOpen)
        {
            dive?.SetHudMove(Vector3.zero);
            return;
        }
        float uiScale = Mathf.Clamp(Screen.width / 1440f, 0.72f, 1.08f);

        panelTitleStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.RoundToInt(14f * uiScale),
            fontStyle = FontStyle.Bold
        };
        panelTitleStyle.normal.textColor = new Color(0.80f, 0.98f, 0.98f, 0.96f);

        panelTextStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.RoundToInt(12f * uiScale),
            richText = true,
            wordWrap = true
        };
        panelTextStyle.normal.textColor = new Color(0.73f, 0.89f, 0.91f, 0.90f);

        toastTitleStyle ??= new GUIStyle(panelTitleStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = Mathf.RoundToInt(17f * uiScale)
        };
        toastTextStyle ??= new GUIStyle(panelTextStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(13f * uiScale)
        };
        touchButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            font = chineseFont,
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(11f * uiScale),
            fontStyle = FontStyle.Bold
        };
        touchButtonStyle.normal.background = travelButtonTexture;
        touchButtonStyle.hover.background = softPanelTexture;
        touchButtonStyle.active.background = progressTexture;
        touchButtonStyle.normal.textColor = new Color(0.86f, 0.98f, 1f, 0.90f);
        touchButtonStyle.hover.textColor = Color.white;
        touchButtonStyle.active.textColor = Color.white;
        menuButtonStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            alignment = TextAnchor.MiddleCenter
        };
        menuButtonStyle.normal.textColor = new Color(0.90f, 0.97f, 0.98f);
        menuButtonStyle.hover.background = softPanelTexture;

        panelTitleStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(14f * uiScale));
        panelTextStyle.fontSize = Mathf.Max(11, Mathf.RoundToInt(12f * uiScale));
        toastTitleStyle.fontSize = Mathf.Max(14, Mathf.RoundToInt(17f * uiScale));
        toastTextStyle.fontSize = Mathf.Max(13, Mathf.RoundToInt(13f * uiScale));
        touchButtonStyle.fontSize = Mathf.RoundToInt(25f * uiScale);
        menuButtonStyle.fontSize = Mathf.RoundToInt(15f * Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 900f), 0.82f, 1.35f));

        if (GUI.Button(GetDiscoveryButtonRect(), discoveryOpen ? "收起图鉴" : "图鉴", menuButtonStyle)) discoveryOpen = !discoveryOpen;
        if (discoveryOpen)
        {
            Rect logPanel = GetDiscoveryRect();
            float contentWidth = logPanel.width - 32f * uiScale;
            float headerHeight = panelTitleStyle.CalcHeight(new GUIContent($"海洋图鉴   {discoveries.Count} / 4"), contentWidth);
            float bodyHeight = panelTextStyle.CalcHeight(new GUIContent(BuildDiscoveryLine()), contentWidth);
            GUI.DrawTexture(logPanel, panelTexture, ScaleMode.StretchToFill);
            GUI.Label(new Rect(logPanel.x + 16f * uiScale, logPanel.y + 12f * uiScale, contentWidth, headerHeight), $"海洋图鉴   {discoveries.Count} / 4", panelTitleStyle);
            GUI.Label(new Rect(logPanel.x + 16f * uiScale, logPanel.y + 18f * uiScale + headerHeight, contentWidth, bodyHeight), BuildDiscoveryLine(), panelTextStyle);
            Rect progressBack = new Rect(logPanel.x + 16f * uiScale, logPanel.yMax - 19f * uiScale, logPanel.width - 32f * uiScale, 3f * uiScale);
            GUI.DrawTexture(progressBack, softPanelTexture, ScaleMode.StretchToFill);
            if (discoveries.Count > 0)
                GUI.DrawTexture(new Rect(progressBack.x, progressBack.y, progressBack.width * discoveries.Count / 4f, progressBack.height), progressTexture, ScaleMode.StretchToFill);
        }

        string controls = BuildControlsHint(dive);
        float controlsWidth = Mathf.Min(600f * uiScale, Screen.width - 24f);
        float controlsHeight = Mathf.Max(30f * uiScale, toastTextStyle.CalcHeight(new GUIContent(controls), controlsWidth - 12f));
        Rect controlsPanel = new Rect((Screen.width - controlsWidth) * 0.5f, Screen.height - controlsHeight - 16f, controlsWidth, controlsHeight);
        if (!string.IsNullOrEmpty(controls)) DrawOverlayLabel(controlsPanel, controls, toastTextStyle);

        if (dive != null)
        {
            DrawTravelControls(dive);
        }

        if (!string.IsNullOrEmpty(hoverName))
        {
            Vector2 guiPointer = new(hoverPointer.x, Screen.height - hoverPointer.y);
            float tooltipWidth = Mathf.Max(142f * uiScale, hoverName.Length * 8.8f * uiScale);
            Rect tooltip = new Rect(
                Mathf.Clamp(guiPointer.x + 16f, 8f, Screen.width - tooltipWidth - 8f),
                Mathf.Clamp(guiPointer.y - 40f * uiScale, 8f, Screen.height - 36f * uiScale),
                tooltipWidth,
                30f * uiScale);
            DrawOverlayLabel(tooltip, hoverName, toastTextStyle);
        }

        if (Time.unscaledTime < toastUntil && !(OceanMissionDirector.Instance?.SubtitlesVisible ?? false))
        {
            Rect[] arrows = GetTravelControlRects();
            float left = arrows[3].xMax + 12f;
            float right = arrows[4].xMin - 12f;
            float width = Mathf.Min(540f * uiScale, right - left);
            float bodyHeight = string.IsNullOrEmpty(toastMessage) ? 0f : toastTextStyle.CalcHeight(new GUIContent(toastMessage), width);
            float titleHeight = toastTitleStyle.CalcHeight(new GUIContent(toastTitle), width);
            Rect toastPanel = new Rect((left + right - width) * 0.5f, Screen.height - 80f - titleHeight - bodyHeight, width, titleHeight + bodyHeight);
            DrawOverlayLabel(new Rect(toastPanel.x, toastPanel.y, width, titleHeight), toastTitle, toastTitleStyle);
            if (!string.IsNullOrEmpty(toastMessage))
            {
                DrawOverlayLabel(new Rect(toastPanel.x, toastPanel.y + titleHeight, width, bodyHeight), toastMessage, toastTextStyle);
            }
        }
    }

    public static Rect GetDiscoveryRect()
    {
        float scale = Mathf.Clamp(Screen.width / 1440f, 0.72f, 1.08f);
        if (Instance == null || !Instance.discoveryOpen) return GetDiscoveryButtonRect();
        float width = 286f * scale;
        float contentWidth = width - 32f * scale;
        float headerHeight = Instance.panelTitleStyle == null ? 24f * scale
            : Instance.panelTitleStyle.CalcHeight(new GUIContent($"海洋图鉴   {Instance.discoveries.Count} / 4"), contentWidth);
        float bodyHeight = Instance.panelTextStyle == null ? 40f * scale
            : Instance.panelTextStyle.CalcHeight(new GUIContent(Instance.BuildDiscoveryLine()), contentWidth);
        return new Rect(Screen.width - 310f * scale, 68f * scale, width, Mathf.Max(110f * scale, headerHeight + bodyHeight + 48f * scale));
    }

    private static Rect GetDiscoveryButtonRect()
    {
        float scale = Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 900f), 0.82f, 1.35f);
        return new Rect(Screen.width - 160f * scale, 20f * scale, 72f * scale, 34f * scale);
    }

    public static void DrawOverlayLabel(Rect rect, string text, GUIStyle style)
    {
        Color color = style.normal.textColor;
        style.normal.textColor = new Color(0.005f, 0.035f, 0.055f, 0.75f);
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
    }

    public static Rect[] GetTravelControlRects()
    {
        float size = Mathf.Clamp(Screen.width * 0.045f, 44f, 48f);
        float gap = 7f;
        float x = 22f;
        float y = Screen.height - size * 2f - gap - 72f;
        float depthX = Screen.width - size - 22f;
        return new[]
        {
            new Rect(x + size + gap, y, size, size),
            new Rect(x + size + gap, y + size + gap, size, size),
            new Rect(x, y + size + gap, size, size),
            new Rect(x + (size + gap) * 2f, y + size + gap, size, size),
            new Rect(depthX, y, size, size),
            new Rect(depthX, y + size + gap, size, size)
        };
    }

    private void DrawTravelControls(CinematicDive dive)
    {
        Rect[] rects = GetTravelControlRects();
        bool forward = GUI.RepeatButton(rects[0], "↑", touchButtonStyle);
        bool backward = GUI.RepeatButton(rects[1], "↓", touchButtonStyle);
        bool left = GUI.RepeatButton(rects[2], "←", touchButtonStyle);
        bool right = GUI.RepeatButton(rects[3], "→", touchButtonStyle);
        bool ascend = GUI.RepeatButton(rects[4], "↑", touchButtonStyle);
        bool descend = GUI.RepeatButton(rects[5], "↓", touchButtonStyle);
        dive.SetHudMove(new Vector3(
            (right ? 1f : 0f) - (left ? 1f : 0f),
            (ascend ? 1f : 0f) - (descend ? 1f : 0f),
            (forward ? 1f : 0f) - (backward ? 1f : 0f)));
        DrawOverlayLabel(new Rect(rects[2].x, rects[2].yMax + 6f, rects[3].xMax - rects[2].x, 24f), "移动", toastTextStyle);
        DrawOverlayLabel(new Rect(rects[5].x - 8f, rects[5].yMax + 6f, rects[5].width + 16f, 24f), "升降", toastTextStyle);
    }

    private string BuildDiscoveryLine()
    {
        string clownfish = discoveries.Contains("clownfish") ? "<color=#7FE0D9>●</color> 小丑鱼" : "○ 小丑鱼";
        string turtle = discoveries.Contains("turtle") ? "<color=#7FE0D9>●</color> 绿海龟" : "○ 绿海龟";
        string manta = discoveries.Contains("manta") ? "<color=#7FE0D9>●</color> 魔鬼鱼" : "○ 魔鬼鱼";
        string whale = discoveries.Contains("whale") ? "<color=#7FE0D9>●</color> 座头鲸" : "○ 座头鲸";
        return $"{clownfish}   {turtle}\n{manta}   {whale}";
    }

    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}

public sealed class MarineEncounter : MonoBehaviour
{
    public string id;
    public string displayName;
    public string description;
    private bool discovered;
    private float feedback;
    private Vector3 baseScale;
    private bool guided;
    private bool guiding;
    private Camera followCamera;
    private float followDistance;
    private Vector3 followVelocity;
    private Vector3 lastGuidanceVelocity;

    public string DisplayName => displayName;
    public bool IsDiscovered => discovered;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    private void Update()
    {
        if (feedback <= 0f)
        {
            return;
        }

        feedback = Mathf.Max(0f, feedback - Time.deltaTime * 1.7f);
        float pulse = Mathf.Sin((1f - feedback) * Mathf.PI * 5f) * feedback * 0.045f;
        transform.localScale = baseScale * (1f + pulse);
        if (feedback <= 0f)
        {
            transform.localScale = baseScale;
        }
    }

    public void Discover()
    {
        discovered = true;
        feedback = 1f;
        UnderwaterHud.Instance?.Discover(id, displayName, description);
    }

    public void SetGuided(bool value, Camera cameraToFollow)
    {
        guided = value;
        followCamera = cameraToFollow;
        if (guided)
        {
            followDistance = GuidanceDistance(followCamera, 13f);
            UnderwaterHud.Instance?.ShowStatus($"已选中 {displayName} · 按住并拖动引导它游动");
        }
        else
        {
            Vector3 releaseVelocity = lastGuidanceVelocity.sqrMagnitude > 0.000001f ? lastGuidanceVelocity : followVelocity;
            guiding = false;
            SetDriftEnabled(true, releaseVelocity);
            followVelocity = Vector3.zero;
            lastGuidanceVelocity = Vector3.zero;
            UnderwaterHud.Instance?.ShowStatus($"{displayName} 已恢复自由巡游");
        }
    }

    public void BeginGuidance(Camera cameraToFollow)
    {
        if (!guided || cameraToFollow == null)
        {
            return;
        }

        followCamera = cameraToFollow;
        followDistance = GuidanceDistance(followCamera, 15f);
        guiding = true;
        followVelocity *= 0.35f;
        lastGuidanceVelocity = Vector3.zero;
        SetDriftEnabled(false);
    }

    public void GuideTowardsPointer(Vector2 pointerPosition, Vector2 pointerDelta)
    {
        GuideTowardsPointerStep(pointerPosition, pointerDelta, Time.deltaTime);
    }

    private float GuidanceDistance(Camera camera, float maximum)
    {
        Ray ray = camera.ScreenPointToRay(camera.WorldToScreenPoint(transform.position));
        // ScreenPointToRay starts on the near plane, not at the camera origin.
        // Preserve an already close animal instead of pushing it out to 6 m.
        return Mathf.Clamp(Vector3.Dot(transform.position - ray.origin, ray.direction), 1f, maximum);
    }

    private void GuideTowardsPointerStep(Vector2 pointerPosition, Vector2 pointerDelta, float deltaTime)
    {
        if (!guided || !guiding || followCamera == null || deltaTime <= 0f)
        {
            return;
        }

        Ray pointerRay = followCamera.ScreenPointToRay(pointerPosition);
        Vector3 target = pointerRay.GetPoint(followDistance);
        Vector3 direction = target - transform.position;
        float distance = direction.magnitude;
        if (distance > 0.04f)
        {
            float targetSpeed = Mathf.Clamp(distance * 1.45f, 1.2f, 9.5f);
            float speed = Mathf.MoveTowards(followVelocity.magnitude, targetSpeed, 12f * deltaTime);
            Vector3 beforeMove = transform.position;
            // Integrate towards the target, bounded by the remaining distance.
            // Low frame rates must not overshoot and feed a reversal back into
            // the body's heading while the child pauses before releasing.
            Vector3 desiredPosition = Vector3.MoveTowards(beforeMove, target, speed * deltaTime);
            OceanCreatureDrift creatureDrift = GetComponent<OceanCreatureDrift>();
            bool isManta = creatureDrift != null && creatureDrift.useMantaAxes;
            bool isLargeCreature = creatureDrift != null;
            bool isWhale = GetComponent<HeroWhaleDrift>() != null;
            float collisionRadius = isWhale ? 1.45f : isManta ? 0.92f : isLargeCreature ? 0.62f : 0.24f;
            float floorClearance = isWhale ? 2.1f : isManta ? 1.15f : isLargeCreature ? 0.78f : 0.38f;
            float noseDistance = isWhale ? 4f : isManta ? 2.1f : isLargeCreature ? 1.55f : 0.48f;
            transform.position = UnderwaterCollision.ResolveMove(
                transform,
                desiredPosition,
                collisionRadius,
                floorClearance,
                noseDistance,
                out _);
            followVelocity = (transform.position - beforeMove) / deltaTime;
            if (followVelocity.sqrMagnitude <= 0.000001f || Vector3.Dot(followVelocity, direction) <= 0f) return;
            lastGuidanceVelocity = followVelocity;

            float bank = Mathf.Clamp(-pointerDelta.x * 0.16f, -12f, 12f);
            Quaternion swimRotation = RotationForVelocity(followVelocity, bank);
            transform.rotation = Quaternion.Slerp(transform.rotation, swimRotation, 1f - Mathf.Exp(-deltaTime * 5.5f));
        }
        else followVelocity = Vector3.zero;
    }

    public void EndGuidance()
    {
        if (!guiding)
        {
            return;
        }

        Vector3 releaseVelocity = lastGuidanceVelocity.sqrMagnitude > 0.000001f ? lastGuidanceVelocity : followVelocity;
        guiding = false;
        SetDriftEnabled(true, releaseVelocity);
        followVelocity = Vector3.zero;
        lastGuidanceVelocity = Vector3.zero;
    }

    private Quaternion RotationForVelocity(Vector3 velocity, float bank)
    {
        ReefFishDrift fishDrift = GetComponent<ReefFishDrift>();
        if (fishDrift != null)
        {
            return fishDrift.RotationForVelocity(velocity, bank);
        }

        OceanCreatureDrift creatureDrift = GetComponent<OceanCreatureDrift>();
        if (creatureDrift != null)
        {
            return creatureDrift.RotationForVelocity(velocity, bank);
        }

        Vector3 direction = velocity.sqrMagnitude > 0.001f ? velocity.normalized : transform.forward;
        return Quaternion.AngleAxis(bank, direction) * Quaternion.LookRotation(direction, Vector3.up);
    }

    private void SetDriftEnabled(bool enabled, Vector3 releaseVelocity = default)
    {
        HeroWhaleDrift whaleDrift = GetComponent<HeroWhaleDrift>();
        if (whaleDrift != null)
        {
            if (enabled) whaleDrift.ResumeFromGuidance(releaseVelocity);
            whaleDrift.enabled = enabled;
        }

        ReefFishDrift fishDrift = GetComponent<ReefFishDrift>();
        if (fishDrift != null)
        {
            if (enabled)
            {
                fishDrift.ResumeFromGuidance(releaseVelocity);
            }
            fishDrift.enabled = enabled;
        }

        OceanCreatureDrift creatureDrift = GetComponent<OceanCreatureDrift>();
        if (creatureDrift != null)
        {
            if (enabled)
            {
                creatureDrift.ResumeFromGuidance(releaseVelocity);
            }
            creatureDrift.enabled = enabled;
        }
    }
}
