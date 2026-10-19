using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class UnderwaterWorld : MonoBehaviour
{
    private readonly List<Material> runtimeMaterials = new();
    private Camera journeyCamera;
    private Light sunlight;
    private Volume postVolume;
    private Mesh schoolingFishBodyMesh;
    private Mesh schoolingFishTailMesh;
    private Mesh schoolingFishFinMesh;
    private Mesh schoolingFishEyeMesh;
    private float baseFogDensity = 0.0135f;

    private static readonly Color ShallowWater = new(0.025f, 0.34f, 0.48f);
    private static readonly Color MidWater = new(0.012f, 0.18f, 0.30f);
    private static readonly Color DeepWater = new(0.008f, 0.075f, 0.15f);

    private void Awake()
    {
        Random.InitState(73021);
        BuildWorld();
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
        Color atmosphere = Color.Lerp(Color.Lerp(ShallowWater, MidWater, midBlend), DeepWater, deepBlend);
        RenderSettings.fogColor = atmosphere;
        RenderSettings.fogDensity = Mathf.Lerp(Mathf.Lerp(0.0105f, 0.0175f, midBlend), 0.027f, deepBlend);
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.30f, 0.58f, 0.62f), new Color(0.08f, 0.24f, 0.34f), midBlend);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.11f, 0.31f, 0.34f), new Color(0.025f, 0.11f, 0.18f), deepBlend);
        journeyCamera.backgroundColor = Color.Lerp(atmosphere, new Color(0.035f, 0.14f, 0.20f), 0.12f + deepBlend * 0.18f);
        if (sunlight != null)
        {
            sunlight.intensity = Mathf.Lerp(Mathf.Lerp(2.35f, 1.35f, midBlend), 0.58f, deepBlend);
            sunlight.color = Color.Lerp(Color.Lerp(new Color(0.78f, 0.95f, 1f), new Color(0.48f, 0.78f, 0.90f), midBlend), new Color(0.25f, 0.48f, 0.72f), deepBlend);
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
        RenderSettings.ambientSkyColor = new Color(0.30f, 0.58f, 0.62f);
        RenderSettings.ambientEquatorColor = new Color(0.11f, 0.31f, 0.34f);
        RenderSettings.ambientGroundColor = new Color(0.055f, 0.17f, 0.17f);
        RenderSettings.reflectionIntensity = 0.72f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = ShallowWater;
        RenderSettings.fogDensity = baseFogDensity;

        CreateLighting();
        CreateCamera();
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
        sunlight.color = new Color(0.78f, 0.95f, 1f);
        sunlight.intensity = 2.35f;
        sunlight.shadows = LightShadows.Soft;
        sunlight.shadowStrength = 0.64f;
        sunlight.shadowBias = 0.04f;

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
        light.intensity = intensity;
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
        journeyCamera.backgroundColor = ShallowWater;
        journeyCamera.allowHDR = true;
        journeyCamera.allowMSAA = true;
        cameraObject.AddComponent<AudioListener>();

        UniversalAdditionalCameraData cameraData = cameraObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;

        CinematicDive dive = cameraObject.AddComponent<CinematicDive>();
        dive.duration = 68f;
        dive.path = new[]
        {
            new Vector3(0f, 1.8f, -12f),
            new Vector3(-1.4f, 1.25f, 1f),
            new Vector3(2.2f, 0.45f, 16f),
            new Vector3(-2.4f, 0.15f, 32f),
            new Vector3(1.6f, 0.25f, 49f),
            new Vector3(4f, 0.55f, 70f),
            new Vector3(1f, -2.8f, 117f),
            new Vector3(-4f, -6.2f, 139f),
            new Vector3(3f, -5.2f, 164f),
            new Vector3(0f, -2f, 190f),
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
        bloom.intensity.Override(0.30f);
        bloom.threshold.Override(0.95f);
        bloom.scatter.Override(0.62f);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.78f);
        color.contrast.Override(6f);
        color.saturation.Override(18f);
        color.colorFilter.Override(new Color(0.96f, 1f, 1f));

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color(0.01f, 0.05f, 0.08f));
        vignette.intensity.Override(0.08f);
        vignette.smoothness.Override(0.78f);

        WhiteBalance whiteBalance = profile.Add<WhiteBalance>(true);
        whiteBalance.temperature.Override(-2f);
        whiteBalance.tint.Override(-1f);

        LensDistortion lens = profile.Add<LensDistortion>(true);
        lens.intensity.Override(-0.035f);
        lens.scale.Override(0.985f);

        ChromaticAberration chromatic = profile.Add<ChromaticAberration>(true);
        chromatic.intensity.Override(0.018f);

        FilmGrain grain = profile.Add<FilmGrain>(true);
        grain.intensity.Override(0.055f);
        grain.response.Override(0.72f);

        DepthOfField depthOfField = profile.Add<DepthOfField>(true);
        depthOfField.mode.Override(DepthOfFieldMode.Gaussian);
        depthOfField.gaussianStart.Override(11f);
        depthOfField.gaussianEnd.Override(42f);
        depthOfField.gaussianMaxRadius.Override(0.48f);
        depthOfField.highQualitySampling.Override(false);
    }

    private void CreateWaterSurface()
    {
        Material surface = MakeWaterSurfaceMaterial();
        GameObject plane = new("Moving Water Surface");
        plane.name = "Moving Water Surface";
        plane.transform.SetParent(transform);
        plane.transform.position = new Vector3(0f, 48f, 100f);
        plane.AddComponent<MeshFilter>().sharedMesh = CreateSurfaceMesh(360f, 520f, 40, 104);
        plane.AddComponent<MeshRenderer>().sharedMaterial = surface;
        plane.AddComponent<SurfaceDrift>();
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
        const int xSegments = 64;
        const int zSegments = 220;
        const float width = 90f;
        const float length = 260f;
        Vector3[] vertices = new Vector3[(xSegments + 1) * (zSegments + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[xSegments * zSegments * 6];

        for (int z = 0; z <= zSegments; z++)
        {
            float z01 = z / (float)zSegments;
            float worldZ = z01 * length - 28f;
            float routeDepth = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(75f, 150f, worldZ));
            for (int x = 0; x <= xSegments; x++)
            {
                float x01 = x / (float)xSegments;
                float worldX = (x01 - 0.5f) * width;
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
                float height = broad + detail + banks + rockyRidges + sandRipples + outcrops - 3.3f + trench;
                vertices[z * (xSegments + 1) + x] = new Vector3(worldX, height, worldZ);
                uvs[z * (xSegments + 1) + x] = new Vector2(x01 * 16f, z01 * 46f);
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

        Mesh mesh = new() { name = "Sculpted Seafloor" };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        GameObject floor = new("Sculpted Seafloor");
        floor.transform.SetParent(transform);
        floor.AddComponent<MeshFilter>().sharedMesh = mesh;
        floor.AddComponent<MeshRenderer>().sharedMaterial = MakeCausticMaterial();
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
        for (int i = 0; i < 8; i++)
        {
            GameObject shaft = new("Volumetric Sun Ray");
            shaft.name = "Volumetric Sun Ray";
            shaft.transform.SetParent(transform);
            float routeT = i / 7f;
            shaft.transform.position = new Vector3(Random.Range(-16f, 16f), 12f, Mathf.Lerp(-6f, 92f, routeT) + Random.Range(-5f, 5f));
            shaft.transform.rotation = Quaternion.Euler(Random.Range(-1.2f, 1.2f), -32f + Random.Range(-6f, 6f), Random.Range(6f, 10f));
            shaft.transform.localScale = new Vector3(Random.Range(10f, 17f), Random.Range(70f, 92f), Random.Range(10f, 17f));
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
            new Color(0.56f, 0.74f, 0.78f),
            new Color(0.12f, 0.40f, 0.82f),
            new Color(0.95f, 0.68f, 0.15f),
            new Color(0.96f, 0.30f, 0.10f),
            new Color(0.20f, 0.68f, 0.62f),
            new Color(0.18f, 0.24f, 0.32f)
        };

        Material[] bodyMaterials = new Material[fishColors.Length];
        Material[] finMaterials = new Material[fishColors.Length];
        Material[] deepBodyMaterials = new Material[fishColors.Length];
        Material[] deepFinMaterials = new Material[fishColors.Length];
        for (int i = 0; i < fishColors.Length; i++)
        {
            Color bodyColor = fishColors[i];
            Color finColor = Color.Lerp(bodyColor, new Color(0.035f, 0.08f, 0.12f), 0.34f);
            bodyMaterials[i] = MakeLit($"School fish body {i}", bodyColor, 0.03f, 0.68f);
            finMaterials[i] = MakeLit($"School fish fins {i}", finColor, 0.01f, 0.48f);
            deepBodyMaterials[i] = MakeLit($"Deep school fish body {i}", bodyColor * 0.48f, 0.02f, 0.62f, bodyColor * 0.34f);
            deepFinMaterials[i] = MakeLit($"Deep school fish fins {i}", finColor * 0.45f, 0f, 0.46f, bodyColor * 0.18f);
            bodyMaterials[i].enableInstancing = true;
            finMaterials[i].enableInstancing = true;
            deepBodyMaterials[i].enableInstancing = true;
            deepFinMaterials[i].enableInstancing = true;
        }

        Material eyeMaterial = MakeLit("School fish eyes", new Color(0.008f, 0.012f, 0.015f), 0f, 0.86f);
        eyeMaterial.enableInstancing = true;

        CreateFishSchool(new Vector3(-1f, 1.4f, 23f), 32, 6f, bodyMaterials, finMaterials, eyeMaterial, 0.20f);
        CreateFishSchool(new Vector3(-3f, 1f, 39f), 46, 9f, bodyMaterials, finMaterials, eyeMaterial, 0.17f);
        CreateFishSchool(new Vector3(4f, 0f, 67f), 44, 12f, bodyMaterials, finMaterials, eyeMaterial, 0.19f);
        CreateFishSchool(new Vector3(-3f, -6f, 126f), 34, 8f, deepBodyMaterials, deepFinMaterials, eyeMaterial, 0.14f);
    }

    private void CreateFishSchool(Vector3 center, int count, float radius, Material[] bodyMaterials, Material[] finMaterials, Material eyeMaterial, float scale)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new(Random.Range(-radius, radius), Random.Range(-radius * 0.34f, radius * 0.34f), Random.Range(-radius, radius));
            int palette = i % bodyMaterials.Length;
            GameObject fish = CreateFish(
                center + offset,
                scale * Random.Range(0.72f, 1.28f),
                bodyMaterials[palette],
                finMaterials[palette],
                eyeMaterial,
                i % 3);
            FishSwimmer swimmer = fish.AddComponent<FishSwimmer>();
            swimmer.schoolCenter = center;
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
        SpawnReefFish("Models/BlueTang", "Hero Blue Tang", new Vector3(6.2f, 1.0f, 10.5f), Quaternion.Euler(-2f, 0f, 0f), 3.9f, 0.2f, 1f);
        SpawnReefFish("Models/BlueTang", "Blue Tang companion 1", new Vector3(-4.2f, 1.8f, 14.5f), Quaternion.Euler(-4f, 178f, 0f), 2.1f, 1.7f, 0.72f);
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
        AddEncounter(clownfish, "clownfish", "CLOWNFISH", "A reef specialist that shelters among anemone tentacles.");
        AddEncounter(turtle, "turtle", "GREEN SEA TURTLE", "A calm ocean traveller that can cross entire seas.");
        AddEncounter(manta, "manta", "OCEANIC MANTA RAY", "A gentle filter-feeder gliding on wings up to seven metres wide.");
        AddEncounter(turtleCompanion, "turtle", "GREEN SEA TURTLE", "A calm ocean traveller that can cross entire seas.");
        AddEncounter(mantaCompanion, "manta", "OCEANIC MANTA RAY", "A gentle filter-feeder gliding on wings up to seven metres wide.");
        SpawnResourceModel("Models/WhaleRigged", "Tripo Humpback Whale", new Vector3(20f, 5.5f, 100f), Quaternion.identity, 14f, CreateFallbackWhale);
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
            instance.transform.position += position - bounds.center;
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
        }
        else if (resourcePath.EndsWith("BlueTang") || resourcePath.EndsWith("Clownfish"))
        {
            ReefFishDrift drift = instance.AddComponent<ReefFishDrift>();
            drift.basePosition = instance.transform.position;
        }
        return instance;
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
        material.SetColor("_ShallowColor", new Color(0.10f, 0.56f, 0.68f, 0.12f));
        material.SetColor("_DeepColor", new Color(0.018f, 0.13f, 0.23f, 0.20f));
        material.SetColor("_SunColor", new Color(0.88f, 0.96f, 1f, 1f));
        material.SetFloat("_SunIntensity", 1.15f);
        material.SetFloat("_Distortion", 0.003f);
        material.SetFloat("_WaveScale", 0.06f);
        material.SetFloat("_WaveSpeed", 0.24f);
        material.SetFloat("_FresnelPower", 4.1f);
        runtimeMaterials.Add(material);
        return material;
    }

    private Material MakeBeamMaterial()
    {
        Material template = Resources.Load<Material>("Materials/VolumetricBeam");
        Shader shader = template != null ? template.shader : Shader.Find("Underwater/VolumetricBeam");
        Material material = template != null ? new Material(template) : new Material(shader);
        material.name = "Soft Volumetric Sun Beams";
        material.SetColor("_BaseColor", new Color(0.68f, 0.91f, 1f, 0.038f));
        material.SetFloat("_Intensity", 1.02f);
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
        material.SetColor("_BaseColor", new Color(0.82f, 0.70f, 0.56f));
        material.SetColor("_CausticColor", new Color(0.36f, 0.66f, 0.72f));
        material.SetFloat("_CausticStrength", 0.34f);
        material.SetFloat("_CausticScale", 0.72f);
        material.SetFloat("_CausticSpeed", 0.32f);
        material.SetFloat("_NormalStrength", 0.72f);
        runtimeMaterials.Add(material);
        return material;
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
    private Vector3 freePosition;
    private Vector3 swimVelocity;
    private Vector3 hudMove;
    private float freeYaw;
    private float freePitch;
    private Transform previewWhale;

    public bool IsFreeDive => !autoTour;
    public float SwimSpeed01 => Mathf.Clamp01(swimVelocity.magnitude / 8.5f);

    private void Start()
    {
        diveCamera = GetComponent<Camera>();
        bool startAtArch = Application.absoluteURL.Contains("start=arch");
        bool startAtWhale = Application.absoluteURL.Contains("start=whale");
        bool startAtFish = Application.absoluteURL.Contains("start=fish");
        bool startAtManta = Application.absoluteURL.Contains("start=manta");
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
            freePosition = center + new Vector3(0.8f, 0.15f, -5.4f);
            transform.position = freePosition;
            Quaternion previewRotation = Quaternion.LookRotation(center - transform.position, Vector3.up);
            transform.rotation = previewRotation;
            freeYaw = previewRotation.eulerAngles.y;
            freePitch = NormalizeAngle(previewRotation.eulerAngles.x);
            diveCamera.fieldOfView = 43f;
        }
        else if (startAtManta)
        {
            OceanCreatureDrift[] creatures = FindObjectsByType<OceanCreatureDrift>(FindObjectsSortMode.None);
            foreach (OceanCreatureDrift creature in creatures)
            {
                if (!creature.useMantaAxes) continue;
                previewWhale = creature.transform;
                autoTour = false;
                freePosition = previewWhale.position + new Vector3(0f, 8.5f, -13f);
                transform.position = freePosition;
                Quaternion previewRotation = Quaternion.LookRotation(previewWhale.position - transform.position, Vector3.up);
                transform.rotation = previewRotation;
                freeYaw = previewRotation.eulerAngles.y;
                freePitch = NormalizeAngle(previewRotation.eulerAngles.x);
                diveCamera.fieldOfView = 46f;
                break;
            }
        }
        else if (startAtJelly)
        {
            JellyDrift jelly = FindFirstObjectByType<JellyDrift>();
            if (jelly != null)
            {
                previewWhale = jelly.transform;
                autoTour = false;
                freePosition = previewWhale.position + new Vector3(4.8f, 0.4f, -6.2f);
                transform.position = freePosition;
                Quaternion previewRotation = Quaternion.LookRotation(previewWhale.position - transform.position, Vector3.up);
                transform.rotation = previewRotation;
                freeYaw = previewRotation.eulerAngles.y;
                freePitch = NormalizeAngle(previewRotation.eulerAngles.x);
                diveCamera.fieldOfView = 46f;
            }
        }
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
            UnderwaterHud.Instance?.ShowStatus("RETURNED TO THE REEF");
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

    public void StartMissionAt(Vector3 cameraPosition, Vector3 lookTarget)
    {
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
        UnderwaterHud.Instance?.ShowStatus("FREE DIVE · WASD MOVE · Q/E DEPTH");
    }

    private void ReturnToAutoTour()
    {
        autoTour = true;
        swimVelocity = Vector3.zero;
        clock = FindNearestPathClock(transform.position);
        yawOffset = 0f;
        pitchOffset = 0f;
        UnderwaterHud.Instance?.ShowStatus("AUTO TOUR RESUMED");
    }

    private void UpdateAutoTour()
    {
        clock = Mathf.Repeat(clock + Time.deltaTime, duration);
        float t = clock / duration;
        Vector3 position = EvaluatePath(t);
        Vector3 lookPoint = EvaluatePath(Mathf.Repeat(t + 0.012f, 1f)) + Vector3.up * 0.65f;
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
        transform.position = Vector3.Lerp(transform.position, position, 1f - Mathf.Exp(-Time.deltaTime * 1.75f));
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
        float topSpeed = boost ? 13f : 7.5f;
        Vector3 desiredVelocity = desiredDirection * topSpeed;
        float response = desiredDirection.sqrMagnitude > 0.001f ? 7.2f : 2.1f;
        swimVelocity = Vector3.MoveTowards(swimVelocity, desiredVelocity, response * Time.deltaTime);
        freePosition += swimVelocity * Time.deltaTime;
        freePosition = new Vector3(
            Mathf.Clamp(freePosition.x, -42f, 42f),
            Mathf.Clamp(freePosition.y, -17f, 7.5f),
            Mathf.Clamp(freePosition.z, -4f, 205f));

        float motion = SwimSpeed01;
        float buoyancy = Mathf.Sin(Time.time * 0.58f) * (0.055f + motion * 0.018f);
        float breathingRoll = Mathf.Sin(Time.time * 0.36f + 0.8f) * (0.35f + motion * 0.25f);
        Quaternion targetRotation = Quaternion.Euler(freePitch, freeYaw, breathingRoll);
        transform.position = Vector3.Lerp(transform.position, freePosition + Vector3.up * buoyancy, 1f - Mathf.Exp(-Time.deltaTime * 7f));
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
            return;
        }

        Vector2 pointerPosition = pointer.position.ReadValue();
        if (UnderwaterHud.IsPointerOverTravelControls(pointerPosition))
        {
            dragging = false;
            if (guidingAnimal)
            {
                guidingAnimal = false;
                guidedEncounter?.EndGuidance();
            }
            return;
        }
        UpdateHover(pointerPosition);
        if (WasPointerPressed(pointer))
        {
            guidingAnimal = guidedEncounter != null;
            dragging = !guidingAnimal;
            pressPointer = pointerPosition;
            previousPointer = pointerPosition;
            pointerTravel = 0f;
            if (guidingAnimal)
            {
                guidedEncounter.BeginGuidance(diveCamera);
            }
        }
        if (guidingAnimal && IsPointerPressed(pointer))
        {
            Vector2 delta = pointerPosition - previousPointer;
            previousPointer = pointerPosition;
            pointerTravel += delta.magnitude;
            guidedEncounter.GuideTowardsPointer(pointerPosition, delta);
        }
        else if (dragging)
        {
            Vector2 delta = pointerPosition - previousPointer;
            previousPointer = pointerPosition;
            pointerTravel += delta.magnitude;
            if (autoTour)
            {
                yawOffset += delta.x * 0.11f;
                pitchOffset = Mathf.Clamp(pitchOffset - delta.y * 0.09f, -65f, 65f);
            }
            else
            {
                freeYaw += delta.x * 0.11f;
                freePitch = Mathf.Clamp(freePitch - delta.y * 0.09f, -82f, 82f);
            }
        }
        if (WasPointerReleased(pointer))
        {
            if (guidingAnimal)
            {
                guidingAnimal = false;
                guidedEncounter?.EndGuidance();
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

    private void TryDiscover(Vector2 pointerPosition)
    {
        if (diveCamera == null)
        {
            return;
        }

        Ray ray = diveCamera.ScreenPointToRay(pointerPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 180f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            MissionInteractable missionTarget = hit.collider.GetComponentInParent<MissionInteractable>();
            if (missionTarget != null)
            {
                missionTarget.Activate();
                return;
            }

            MarineEncounter encounter = hit.collider.GetComponentInParent<MarineEncounter>();
            if (encounter == null)
            {
                continue;
            }

            encounter.Discover();
            float now = Time.unscaledTime;
            if (encounter == lastClickedEncounter && now - lastClickTime < 0.42f)
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
        foreach (RaycastHit hit in hits)
        {
            MissionInteractable missionTarget = hit.collider.GetComponentInParent<MissionInteractable>();
            if (missionTarget != null)
            {
                UnderwaterHud.Instance.SetHover(missionTarget.DisplayName, missionTarget.IsComplete, pointerPosition);
                return;
            }

            MarineEncounter encounter = hit.collider.GetComponentInParent<MarineEncounter>();
            if (encounter != null)
            {
                UnderwaterHud.Instance.SetHover(encounter.DisplayName, encounter.IsDiscovered, pointerPosition);
                return;
            }
        }
        UnderwaterHud.Instance.SetHover(null, false, pointerPosition);
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
    public float radius = 8f;
    public float speed = 1.5f;
    public float phase;
    private Vector3 baseScale;
    private Vector3 velocity;
    private Transform tail;

    private void Start()
    {
        baseScale = transform.localScale;
        velocity = transform.right * speed;
        tail = transform.Find("Tail");
    }

    private void Update()
    {
        float t = Time.time * speed * 0.15f + phase;
        Vector3 target = schoolCenter + new Vector3(
            Mathf.Cos(t) * radius,
            Mathf.Sin(t * 1.7f + phase) * radius * 0.20f,
            Mathf.Sin(t) * radius);
        Vector3 toTarget = target - transform.position;
        float burst = 0.82f + Mathf.SmoothStep(0f, 0.38f, Mathf.Max(0f, Mathf.Sin(Time.time * 1.6f + phase)));
        Vector3 desiredVelocity = toTarget.sqrMagnitude > 0.001f ? toTarget.normalized * speed * burst : velocity;
        velocity = Vector3.MoveTowards(velocity, desiredVelocity, speed * 1.45f * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;
        if (velocity.sqrMagnitude > 0.01f)
        {
            float bank = Mathf.Clamp(Vector3.SignedAngle(transform.right, velocity.normalized, Vector3.up) * -0.42f, -18f, 18f);
            Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, Vector3.up) * Quaternion.Euler(0f, -90f, bank);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-Time.deltaTime * 3.2f));
        }
        transform.localScale = baseScale;
        if (tail != null)
        {
            tail.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * (7.5f + speed) + phase) * 27f, 0f);
        }
    }
}

public sealed class ReefFishDrift : MonoBehaviour
{
    public Vector3 basePosition;
    public float phase;
    public float swimScale = 1f;
    private Vector3 baseScale;
    private Vector3 motionVelocity;
    private Vector3 releaseVelocity;
    private float releaseGlide;
    private float travel;

    private void Start()
    {
        basePosition = transform.position;
        baseScale = transform.localScale;
        travel = phase;
    }

    private void Update()
    {
        if (releaseGlide > 0f)
        {
            releaseGlide = Mathf.Max(0f, releaseGlide - Time.deltaTime);
            float naturalSpeed = Mathf.Lerp(1.6f, 3.8f, swimScale);
            Vector3 glideDirection = releaseVelocity.sqrMagnitude > 0.001f ? releaseVelocity.normalized : -transform.forward;
            releaseVelocity = Vector3.MoveTowards(releaseVelocity, glideDirection * naturalSpeed, 1.8f * Time.deltaTime);
            transform.position += releaseVelocity * Time.deltaTime;
            basePosition = transform.position - OrbitOffset();
            motionVelocity = releaseVelocity;
            ApplySwimRotation(releaseVelocity, 0f, 5.5f);
            transform.localScale = baseScale;
            return;
        }

        float burst = 0.76f + Mathf.Max(0f, Mathf.Sin(Time.time * 1.18f + phase * 1.7f)) * 0.34f;
        travel += Time.deltaTime * 0.58f * burst;
        Vector3 target = basePosition + OrbitOffset();
        transform.position = Vector3.SmoothDamp(transform.position, target, ref motionVelocity, 0.24f, 5.8f * swimScale, Time.deltaTime);

        float bodyWave = Mathf.Sin(Time.time * 7.2f + phase) * 2.2f;
        float turn = Mathf.Cos(travel) * 9f;
        float bank = Mathf.Clamp(-turn * 0.32f + bodyWave, -7f, 7f);
        ApplySwimRotation(motionVelocity, bank, 4f);
        transform.localScale = baseScale;
    }

    private Vector3 OrbitOffset()
    {
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

public sealed class HeroWhaleDrift : MonoBehaviour
{
    public Vector3 basePosition;
    private Vector3 routeCenter;
    private Vector3 motionVelocity;
    private float travel;

    private void Start()
    {
        basePosition = transform.position;
        travel = 0.45f;
        routeCenter = basePosition - RouteOffset(travel);
    }

    private void Update()
    {
        float tailBeat = Mathf.Sin(Time.time * 1.82f);
        float propulsion = 0.92f + Mathf.Max(0f, tailBeat) * 0.12f;
        travel += Time.deltaTime * 0.115f * propulsion;
        Vector3 target = routeCenter + RouteOffset(travel);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref motionVelocity, 0.72f, 6.2f, Time.deltaTime);

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

    private static Vector3 RouteOffset(float t)
    {
        return new Vector3(
            Mathf.Sin(t) * 24f,
            Mathf.Sin(t * 1.55f + 0.35f) * 2.6f,
            Mathf.Cos(t) * 16f);
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
        float cycle = Time.time * 1.82f;
        float secondHarmonic = Mathf.Sin(cycle * 2f - 0.35f);
        float bodyWave = Mathf.Sin(cycle) * 3.6f + secondHarmonic * 0.55f;
        float tailWave = Mathf.Sin(cycle - 0.72f) * 11.5f + secondHarmonic * 1.4f;
        float tipWave = Mathf.Sin(cycle - 1.38f) * 23f + secondHarmonic * 2.4f;
        float bodyRoll = Mathf.Sin(cycle * 0.5f + 0.7f) * 0.55f;

        body.localRotation = bodyRest * Quaternion.Euler(bodyWave - 1.5f, 0f, bodyRoll);
        tailBase.localRotation = tailBaseRest * Quaternion.Euler(tailWave - 8.5f, 0f, -bodyWave * 0.12f);
        tailTip.localRotation = tailTipRest * Quaternion.Euler(tipWave - 18f, 0f, -tailWave * 0.08f);
    }
}

public sealed class OceanCreatureDrift : MonoBehaviour
{
    public Vector3 basePosition;
    public float phase;
    public bool useMantaAxes;
    private Vector3 motionVelocity;
    private Vector3 releaseVelocity;
    private float releaseGlide;
    private float travel;
    private TurtleStrokeAnimator turtleStroke;

    private void Start()
    {
        basePosition = transform.position;
        travel = phase;
        turtleStroke = GetComponent<TurtleStrokeAnimator>();
    }

    private void Update()
    {
        if (releaseGlide > 0f)
        {
            releaseGlide = Mathf.Max(0f, releaseGlide - Time.deltaTime);
            float naturalSpeed = turtleStroke == null ? 2.8f : 2.4f;
            Vector3 glideDirection = releaseVelocity.sqrMagnitude > 0.001f ? releaseVelocity.normalized : -transform.forward;
            releaseVelocity = Vector3.MoveTowards(releaseVelocity, glideDirection * naturalSpeed, 1.35f * Time.deltaTime);
            transform.position += releaseVelocity * Time.deltaTime;
            basePosition = transform.position - OrbitOffset();
            motionVelocity = releaseVelocity;
            ApplySwimRotation(releaseVelocity, 0f, 4.8f);
            return;
        }

        float strokeBoost = turtleStroke == null
            ? 0.12f + Mathf.Sin(Time.time * 0.55f + phase) * 0.035f
            : 0.095f + turtleStroke.Propulsion * 0.16f;
        travel += Time.deltaTime * strokeBoost;
        Vector3 target = basePosition + OrbitOffset();
        float maxSpeed = turtleStroke == null ? 3.8f : Mathf.Lerp(2.1f, 5.4f, turtleStroke.Propulsion);
        transform.position = Vector3.SmoothDamp(transform.position, target, ref motionVelocity, 0.46f, maxSpeed, Time.deltaTime);

        float turn = Mathf.Cos(travel) * (turtleStroke == null ? 13f : 9f);
        float bank = Mathf.Clamp(-turn * (turtleStroke == null ? 0.28f : 0.18f), -6f, 6f);
        ApplySwimRotation(motionVelocity, bank, 2.7f);
    }

    private Vector3 OrbitOffset()
    {
        float radius = turtleStroke == null ? 6.4f : 4.8f;
        return new Vector3(
            Mathf.Sin(travel) * radius,
            Mathf.Sin(travel * 1.22f + 0.6f) * (turtleStroke == null ? 1.2f : 0.82f),
            Mathf.Cos(travel * 0.74f) * radius * 0.68f);
    }

    public Quaternion RotationForVelocity(Vector3 velocity, float bank)
    {
        Vector3 fallback = useMantaAxes ? transform.up : -transform.forward;
        Vector3 direction = velocity.sqrMagnitude > 0.001f ? velocity.normalized : fallback;
        if (useMantaAxes)
        {
            Quaternion alignForwardAndDorsal = Quaternion.LookRotation(Vector3.right, Vector3.forward);
            return Quaternion.AngleAxis(bank, direction)
                * Quaternion.LookRotation(direction, Vector3.up)
                * alignForwardAndDorsal;
        }
        return Quaternion.AngleAxis(bank, direction) * Quaternion.LookRotation(-direction, Vector3.up);
    }

    public void ResumeFromGuidance(Vector3 velocity)
    {
        Vector3 fallback = useMantaAxes ? transform.up : -transform.forward;
        releaseVelocity = velocity.sqrMagnitude > 0.01f ? Vector3.ClampMagnitude(velocity, 8.5f) : fallback * 2.6f;
        releaseGlide = 2.8f;
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
        float cycle = Mathf.Repeat(Time.time * 0.56f + phase, 1f);
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
        transform.position += velocity * Time.deltaTime;

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
            ApplyMute();
            UnderwaterHud.Instance?.ShowStatus(muted ? "OCEAN AUDIO MUTED" : "OCEAN AUDIO ON");
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

    private static bool HasUserGesture()
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
    private GUIStyle titleStyle;
    private GUIStyle hintStyle;
    private GUIStyle panelTitleStyle;
    private GUIStyle panelTextStyle;
    private GUIStyle toastTitleStyle;
    private GUIStyle toastTextStyle;
    private GUIStyle touchButtonStyle;
    private Texture2D panelTexture;
    private Texture2D softPanelTexture;
    private Texture2D progressTexture;
    private float alpha = 1f;
    private float toastUntil;
    private string toastTitle;
    private string toastMessage;
    private string hoverName;
    private bool hoverDiscovered;
    private Vector2 hoverPointer;

    private void Awake()
    {
        Instance = this;
        panelTexture = MakeTexture(new Color(0.012f, 0.075f, 0.11f, 0.82f));
        softPanelTexture = MakeTexture(new Color(0.01f, 0.06f, 0.09f, 0.62f));
        progressTexture = MakeTexture(new Color(0.24f, 0.82f, 0.82f, 0.92f));
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
    }

    public void Discover(string id, string displayName, string description)
    {
        bool isNew = discoveries.Add(id);
        toastTitle = isNew ? $"NEW ENCOUNTER · {displayName}" : $"FIELD LOG · {displayName}";
        toastMessage = isNew ? description : "This species is already in your field log.";
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
        if (Touchscreen.current == null)
        {
            return false;
        }

        Vector2 guiPoint = new(screenPointer.x, Screen.height - screenPointer.y);
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
        float uiScale = Mathf.Clamp(Screen.width / 1440f, 0.72f, 1.08f);
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Screen.width / 42, 18, 36),
            fontStyle = FontStyle.Normal
        };
        titleStyle.normal.textColor = new Color(0.86f, 0.98f, 1f, alpha * 0.88f);

        hintStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Screen.width / 95, 11, 17)
        };
        hintStyle.normal.textColor = new Color(0.75f, 0.92f, 0.95f, alpha * 0.64f);

        panelTitleStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.RoundToInt(14f * uiScale),
            fontStyle = FontStyle.Bold
        };
        panelTitleStyle.normal.textColor = new Color(0.80f, 0.98f, 0.98f, 0.96f);

        panelTextStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = Mathf.RoundToInt(12f * uiScale),
            richText = true,
            wordWrap = true
        };
        panelTextStyle.normal.textColor = new Color(0.73f, 0.89f, 0.91f, 0.90f);

        toastTitleStyle ??= new GUIStyle(panelTitleStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(17f * uiScale)
        };
        toastTextStyle ??= new GUIStyle(panelTextStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(13f * uiScale)
        };
        touchButtonStyle ??= new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(11f * uiScale),
            fontStyle = FontStyle.Bold
        };

        GUI.Label(new Rect(0f, Screen.height * 0.07f, Screen.width, 48f), "THE BLUE BETWEEN WORLDS", titleStyle);

        Rect logPanel = new Rect(24f * uiScale, 22f * uiScale, 286f * uiScale, 110f * uiScale);
        GUI.DrawTexture(logPanel, panelTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(logPanel.x + 16f * uiScale, logPanel.y + 12f * uiScale, logPanel.width - 32f * uiScale, 24f * uiScale), $"OCEAN FIELD LOG   {discoveries.Count} / 3", panelTitleStyle);
        GUI.Label(new Rect(logPanel.x + 16f * uiScale, logPanel.y + 40f * uiScale, logPanel.width - 32f * uiScale, 40f * uiScale), BuildDiscoveryLine(), panelTextStyle);
        Rect progressBack = new Rect(logPanel.x + 16f * uiScale, logPanel.yMax - 19f * uiScale, logPanel.width - 32f * uiScale, 3f * uiScale);
        GUI.DrawTexture(progressBack, softPanelTexture, ScaleMode.StretchToFill);
        if (discoveries.Count > 0)
        {
            GUI.DrawTexture(new Rect(progressBack.x, progressBack.y, progressBack.width * discoveries.Count / 3f, progressBack.height), progressTexture, ScaleMode.StretchToFill);
        }

        CinematicDive dive = Camera.main == null ? null : Camera.main.GetComponent<CinematicDive>();
        if (dive != null && dive.IsFreeDive)
        {
            Rect pauseBadge = new Rect(Screen.width - 150f * uiScale, 22f * uiScale, 126f * uiScale, 34f * uiScale);
            GUI.DrawTexture(pauseBadge, panelTexture, ScaleMode.StretchToFill);
            GUI.Label(pauseBadge, "FREE DIVE", toastTextStyle);
        }

        string controls = string.IsNullOrEmpty(hoverName)
            ? "WASD SWIM  ·  Q/E DEPTH  ·  DRAG LOOK  ·  SPACE AUTO/FREE  ·  M AUDIO"
            : hoverDiscovered ? $"DOUBLE-CLICK TO SELECT · HOLD AND DRAG TO GUIDE · RELEASE TO LET GO · {hoverName}" : $"CLICK TO LOG · DOUBLE-CLICK TO SELECT · {hoverName}";
        Rect controlsPanel = new Rect(Screen.width * 0.5f - 370f * uiScale, Screen.height - 52f * uiScale, 740f * uiScale, 30f * uiScale);
        GUI.DrawTexture(controlsPanel, softPanelTexture, ScaleMode.StretchToFill);
        GUI.Label(controlsPanel, controls, toastTextStyle);

        if (dive != null && Touchscreen.current != null)
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
            GUI.DrawTexture(tooltip, panelTexture, ScaleMode.StretchToFill);
            GUI.Label(tooltip, hoverName, toastTextStyle);
        }

        if (Time.unscaledTime < toastUntil)
        {
            float toastHeight = string.IsNullOrEmpty(toastMessage) ? 62f : 94f;
            Rect toastPanel = new Rect(Screen.width * 0.5f - 235f * uiScale, Screen.height * 0.67f, 470f * uiScale, toastHeight * uiScale);
            GUI.DrawTexture(toastPanel, panelTexture, ScaleMode.StretchToFill);
            GUI.Label(new Rect(toastPanel.x + 18f * uiScale, toastPanel.y + 10f * uiScale, toastPanel.width - 36f * uiScale, 28f * uiScale), toastTitle, toastTitleStyle);
            if (!string.IsNullOrEmpty(toastMessage))
            {
                GUI.Label(new Rect(toastPanel.x + 22f * uiScale, toastPanel.y + 42f * uiScale, toastPanel.width - 44f * uiScale, 40f * uiScale), toastMessage, toastTextStyle);
            }
        }

        alpha = Mathf.MoveTowards(alpha, Time.time < 4f ? 1f : 0f, Time.deltaTime * 0.25f);
    }

    private static Rect[] GetTravelControlRects()
    {
        float size = Mathf.Clamp(Screen.width * 0.085f, 48f, 70f);
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
        bool forward = GUI.RepeatButton(rects[0], "FWD", touchButtonStyle);
        bool backward = GUI.RepeatButton(rects[1], "BACK", touchButtonStyle);
        bool left = GUI.RepeatButton(rects[2], "LEFT", touchButtonStyle);
        bool right = GUI.RepeatButton(rects[3], "RIGHT", touchButtonStyle);
        bool ascend = GUI.RepeatButton(rects[4], "UP", touchButtonStyle);
        bool descend = GUI.RepeatButton(rects[5], "DOWN", touchButtonStyle);
        dive.SetHudMove(new Vector3(
            (right ? 1f : 0f) - (left ? 1f : 0f),
            (ascend ? 1f : 0f) - (descend ? 1f : 0f),
            (forward ? 1f : 0f) - (backward ? 1f : 0f)));
    }

    private string BuildDiscoveryLine()
    {
        string clownfish = discoveries.Contains("clownfish") ? "<color=#7FE0D9>●</color> CLOWNFISH" : "○ CLOWNFISH";
        string turtle = discoveries.Contains("turtle") ? "<color=#7FE0D9>●</color> TURTLE" : "○ TURTLE";
        string manta = discoveries.Contains("manta") ? "<color=#7FE0D9>●</color> MANTA RAY" : "○ MANTA RAY";
        return $"{clownfish}   {turtle}\n{manta}";
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
            followDistance = Mathf.Clamp(Vector3.Distance(transform.position, followCamera.transform.position), 6f, 13f);
            UnderwaterHud.Instance?.ShowStatus($"{displayName} SELECTED · HOLD AND DRAG TO GUIDE");
        }
        else
        {
            Vector3 releaseVelocity = followVelocity;
            guiding = false;
            SetDriftEnabled(true, releaseVelocity);
            followVelocity = Vector3.zero;
            UnderwaterHud.Instance?.ShowStatus($"{displayName} RETURNED TO THE REEF");
        }
    }

    public void BeginGuidance(Camera cameraToFollow)
    {
        if (!guided || cameraToFollow == null)
        {
            return;
        }

        followCamera = cameraToFollow;
        followDistance = Mathf.Clamp(Vector3.Distance(transform.position, followCamera.transform.position), 6f, 15f);
        guiding = true;
        followVelocity *= 0.35f;
        SetDriftEnabled(false);
    }

    public void GuideTowardsPointer(Vector2 pointerPosition, Vector2 pointerDelta)
    {
        if (!guided || !guiding || followCamera == null)
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
            Vector3 desiredVelocity = direction / distance * targetSpeed;
            followVelocity = Vector3.MoveTowards(followVelocity, desiredVelocity, 12f * Time.deltaTime);
            transform.position += followVelocity * Time.deltaTime;

            float bank = Mathf.Clamp(-pointerDelta.x * 0.16f, -12f, 12f);
            Quaternion swimRotation = RotationForVelocity(followVelocity, bank);
            transform.rotation = Quaternion.Slerp(transform.rotation, swimRotation, 1f - Mathf.Exp(-Time.deltaTime * 5.5f));
        }
    }

    public void EndGuidance()
    {
        if (!guiding)
        {
            return;
        }

        Vector3 releaseVelocity = followVelocity;
        guiding = false;
        SetDriftEnabled(true, releaseVelocity);
        followVelocity = Vector3.zero;
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

