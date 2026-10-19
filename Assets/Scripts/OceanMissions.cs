using System.Collections.Generic;
using UnityEngine;

public sealed class OceanMissionDirector : MonoBehaviour
{
    private enum MissionMode
    {
        None,
        Adventure,
        Rescue,
        Complete
    }

    public static OceanMissionDirector Instance { get; private set; }

    private readonly List<GameObject> adventureBeacons = new();
    private MissionMode mode;
    private MissionMode completedMission;
    private int stage;
    private string missionTitle;
    private string objective;
    private string speaker;
    private string dialogue;
    private float dialogueUntil;
    private bool showMenu;
    private Font chineseFont;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;
    private GUIStyle objectiveStyle;
    private Texture2D panelTexture;
    private Texture2D accentTexture;
    private GameObject rescueTurtle;
    private OceanCreatureDrift rescueDrift;
    private FishingLineRescue fishingLine;
    private CinematicDive diver;

    public bool IsMissionActive => mode == MissionMode.Adventure || mode == MissionMode.Rescue;

    private void Awake()
    {
        Instance = this;
        chineseFont = Resources.Load<Font>("Fonts/DroidSansFallbackFull");
        panelTexture = MakeTexture(new Color(0.008f, 0.055f, 0.09f, 0.92f));
        accentTexture = MakeTexture(new Color(0.20f, 0.82f, 0.84f, 0.94f));
    }

    private void Start()
    {
        diver = Camera.main == null ? null : Camera.main.GetComponent<CinematicDive>();
        CreateAdventureBeacons();
        FindRescueTurtle();

        string url = Application.absoluteURL;
        if (url.Contains("mission=adventure"))
        {
            StartAdventure();
        }
        else if (url.Contains("mission=rescue"))
        {
            StartRescue();
        }
        else
        {
            showMenu = !url.Contains("start=");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (panelTexture != null) Destroy(panelTexture);
        if (accentTexture != null) Destroy(accentTexture);
    }

    private void Update()
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        if (mode == MissionMode.Adventure && stage < adventureBeacons.Count)
        {
            GameObject target = adventureBeacons[stage];
            float distance = Vector3.Distance(camera.transform.position, target.transform.position);
            objective = AdventureObjective(stage, distance);
            if (distance < 4.2f)
            {
                ReachAdventureBeacon();
            }
        }
        else if (mode == MissionMode.Rescue && stage == 1 && rescueTurtle != null)
        {
            float height = rescueTurtle.transform.position.y;
            objective = $"双击海龟并按住拖向上方蓝光　高度 {Mathf.Max(0f, height):0.0} / 6.2 米";
            if (height >= 6.2f)
            {
                CompleteMission(
                    "海龟豆豆",
                    "我换好气啦！谢谢你把我安全送到水面。以后看到旧鱼线，我们一起把它带离海洋。",
                    "解锁：海洋守护员徽章 · 海龟图鉴");
            }
        }
    }

    public bool IsPointerOverUi(Vector2 screenPointer)
    {
        Vector2 guiPoint = new(screenPointer.x, Screen.height - screenPointer.y);
        if (showMenu && GetMenuRect().Contains(guiPoint)) return true;
        if (mode != MissionMode.None && GetMissionRect().Contains(guiPoint)) return true;
        return GetMissionButtonRect().Contains(guiPoint);
    }

    public void TryUntie(int knotIndex)
    {
        if (mode != MissionMode.Rescue || stage != 0 || fishingLine == null) return;
        if (knotIndex != fishingLine.NextKnot)
        {
            ShowDialogue("海龟悠悠", "先解开最靠近前鳍的结，再沿着鱼线往后找。这样不会越拉越紧。", 4.8f);
            return;
        }

        fishingLine.ReleaseKnot(knotIndex);
        int remaining = fishingLine.KnotCount - fishingLine.NextKnot;
        if (remaining > 0)
        {
            objective = $"点击下一个发光绳结　还剩 {remaining} 个";
            ShowDialogue("小丑鱼点点", remaining == 2 ? "第一处松开了！鱼线还绕在龟壳边缘。" : "只剩最后一个结，在礁石这一侧。", 3.6f);
            return;
        }

        stage = 1;
        if (rescueDrift != null) rescueDrift.enabled = true;
        objective = "双击海龟，按住鼠标把它带向上方蓝光";
        ShowDialogue("海龟豆豆", "鱼线解开了！请带我往上游。我松开以后，也会沿着你给的方向继续前进。", 6.5f);
    }

    private void OnGUI()
    {
        EnsureStyles();
        if (showMenu)
        {
            DrawMissionMenu();
            return;
        }

        if (mode == MissionMode.None)
        {
            if (GUI.Button(GetMissionButtonRect(), "任务", buttonStyle)) showMenu = true;
            return;
        }

        Rect panel = GetMissionRect();
        GUI.DrawTexture(panel, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panel.x, panel.y, 5f, panel.height), accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, 26f), missionTitle, titleStyle);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 43f, panel.width - 36f, 72f), objective, objectiveStyle);
        if (GUI.Button(new Rect(panel.x + panel.width - 102f, panel.y + panel.height - 34f, 88f, 24f), "任务菜单", buttonStyle))
        {
            ResetMission();
            showMenu = true;
        }

        if (Time.unscaledTime < dialogueUntil && !string.IsNullOrEmpty(dialogue))
        {
            Rect dialoguePanel = new(Screen.width * 0.5f - 350f, Screen.height - 178f, 700f, 106f);
            GUI.DrawTexture(dialoguePanel, panelTexture, ScaleMode.StretchToFill);
            GUI.Label(new Rect(dialoguePanel.x + 22f, dialoguePanel.y + 12f, 160f, 25f), speaker, titleStyle);
            GUI.Label(new Rect(dialoguePanel.x + 22f, dialoguePanel.y + 39f, dialoguePanel.width - 44f, 56f), dialogue, bodyStyle);
        }
    }

    private void DrawMissionMenu()
    {
        Rect panel = GetMenuRect();
        GUI.DrawTexture(panel, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f), accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(panel.x + 28f, panel.y + 22f, panel.width - 56f, 42f), "选择今天的海底任务", titleStyle);
        GUI.Label(new Rect(panel.x + 32f, panel.y + 68f, panel.width - 64f, 64f), "两个任务都没有失败惩罚。迷路了会回到上一处提示，可以慢慢探索。", bodyStyle);

        if (GUI.Button(new Rect(panel.x + 34f, panel.y + 140f, panel.width - 68f, 66f), "探险：追着鲸歌找星光", buttonStyle))
        {
            StartAdventure();
        }
        if (GUI.Button(new Rect(panel.x + 34f, panel.y + 218f, panel.width - 68f, 66f), "救援：送小海龟去呼吸", buttonStyle))
        {
            StartRescue();
        }
        if (GUI.Button(new Rect(panel.x + panel.width - 126f, panel.y + panel.height - 38f, 94f, 25f), "继续漫游", buttonStyle))
        {
            showMenu = false;
        }
    }

    private void StartAdventure()
    {
        ResetMission();
        showMenu = false;
        mode = MissionMode.Adventure;
        missionTitle = "追着鲸歌找星光";
        stage = 0;
        adventureBeacons[0].SetActive(true);
        diver?.StartMissionAt(new Vector3(0f, 1.5f, -6f), new Vector3(0f, 1.2f, 18f));
        ShowDialogue("海龟悠悠", "昨晚的洋流冲散了海底拱门的三盏星光灯。跟着鱼群和鲸歌，把回家的路重新点亮吧。", 7f);
    }

    private void ReachAdventureBeacon()
    {
        adventureBeacons[stage].SetActive(false);
        stage++;
        if (stage == 1)
        {
            adventureBeacons[stage].SetActive(true);
            ShowDialogue("小丑鱼点点", "第一盏亮了！鱼群转弯时会一起改变方向。下一盏灯在魔鬼鱼经过的水道。", 6f);
        }
        else if (stage == 2)
        {
            adventureBeacons[stage].SetActive(true);
            ShowDialogue("魔鬼鱼阿翼", "跟在我的背后，水流会平稳一些。听见更清楚的鲸歌，就朝那个方向游。", 6f);
        }
        else
        {
            CompleteMission(
                "海龟悠悠",
                "三盏星光灯都亮了！小鱼群已经沿着光路回到珊瑚家园。",
                "解锁：鲸歌探路员徽章 · 鲸鱼与水母图鉴");
        }
    }

    private void StartRescue()
    {
        ResetMission();
        showMenu = false;
        mode = MissionMode.Rescue;
        missionTitle = "送小海龟去呼吸";
        stage = 0;

        if (rescueTurtle == null)
        {
            objective = "暂时找不到救援海龟，请返回任务菜单重试";
            return;
        }

        rescueDrift = rescueTurtle.GetComponent<OceanCreatureDrift>();
        if (rescueDrift != null) rescueDrift.enabled = false;
        fishingLine = rescueTurtle.AddComponent<FishingLineRescue>();
        fishingLine.Initialize(this);
        Vector3 turtlePosition = rescueTurtle.transform.position;
        diver?.StartMissionAt(turtlePosition + new Vector3(-5.5f, 1.5f, -8f), turtlePosition);
        objective = "找到海龟身上的发光绳结，先点击最靠近前鳍的一处";
        ShowDialogue("小丑鱼点点", "海龟豆豆被旧鱼线缠住了。它没有受伤，但要先解开三个绳结，才能游到水面呼吸。", 7f);
    }

    private void CompleteMission(string who, string line, string reward)
    {
        completedMission = mode;
        mode = MissionMode.Complete;
        missionTitle = completedMission == MissionMode.Adventure ? "探险完成" : "救援完成";
        objective = reward;
        ShowDialogue(who, line, 12f);
    }

    private void ResetMission()
    {
        foreach (GameObject beacon in adventureBeacons) beacon.SetActive(false);
        if (fishingLine != null)
        {
            fishingLine.RemoveAll();
            Destroy(fishingLine);
            fishingLine = null;
        }
        if (rescueDrift != null) rescueDrift.enabled = true;
        rescueDrift = null;
        mode = MissionMode.None;
        stage = 0;
        missionTitle = string.Empty;
        objective = string.Empty;
        dialogue = string.Empty;
    }

    private void FindRescueTurtle()
    {
        foreach (OceanCreatureDrift creature in FindObjectsByType<OceanCreatureDrift>(FindObjectsSortMode.None))
        {
            if (!creature.useMantaAxes && creature.name.Contains("Companion"))
            {
                rescueTurtle = creature.gameObject;
                return;
            }
        }
    }

    private void CreateAdventureBeacons()
    {
        Vector3[] positions =
        {
            new(-1f, 1.8f, 23f),
            new(4f, 2.2f, 67f),
            new(-2f, -5.5f, 158f)
        };
        foreach (Vector3 position in positions)
        {
            GameObject beacon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            beacon.name = "Star Route Beacon";
            beacon.transform.SetParent(transform);
            beacon.transform.position = position;
            beacon.transform.localScale = Vector3.one * 0.72f;
            Destroy(beacon.GetComponent<Collider>());
            Material material = new(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = new Color(0.35f, 0.96f, 1f, 1f);
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.18f, 1.4f, 1.7f));
            beacon.GetComponent<Renderer>().sharedMaterial = material;
            Light light = beacon.AddComponent<Light>();
            light.color = new Color(0.28f, 0.88f, 1f);
            light.intensity = 2.2f;
            light.range = 7f;
            light.shadows = LightShadows.None;
            beacon.AddComponent<MissionBeacon>();
            beacon.SetActive(false);
            adventureBeacons.Add(beacon);
        }
    }

    private string AdventureObjective(int currentStage, float distance)
    {
        string[] prompts =
        {
            "跟随黄色鱼群，找到珊瑚花园的第一盏灯",
            "穿过魔鬼鱼经过的水道，找到第二盏灯",
            "听着鲸歌前往岩石拱门，点亮最后一盏灯"
        };
        return $"{prompts[Mathf.Clamp(currentStage, 0, prompts.Length - 1)]}　距离 {distance:0} 米";
    }

    private void ShowDialogue(string who, string line, float seconds)
    {
        speaker = who;
        dialogue = line;
        dialogueUntil = Time.unscaledTime + seconds;
    }

    private void EnsureStyles()
    {
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            fontSize = Mathf.Clamp(Screen.width / 62, 18, 27),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        titleStyle.normal.textColor = new Color(0.82f, 0.98f, 1f);

        bodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            fontSize = Mathf.Clamp(Screen.width / 92, 14, 19),
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        bodyStyle.normal.textColor = new Color(0.90f, 0.97f, 0.98f);

        objectiveStyle ??= new GUIStyle(bodyStyle) { fontStyle = FontStyle.Bold };
        objectiveStyle.normal.textColor = new Color(0.58f, 0.96f, 0.92f);

        buttonStyle ??= new GUIStyle(GUI.skin.button)
        {
            font = chineseFont,
            fontSize = Mathf.Clamp(Screen.width / 95, 13, 18),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
    }

    private static Rect GetMenuRect()
    {
        float width = Mathf.Min(570f, Screen.width - 40f);
        float height = 340f;
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    private static Rect GetMissionRect()
    {
        float width = Mathf.Min(390f, Screen.width * 0.46f);
        return new Rect(Screen.width - width - 22f, 70f, width, 132f);
    }

    private static Rect GetMissionButtonRect() => new(Screen.width - 134f, 68f, 112f, 36f);

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

public sealed class MissionBeacon : MonoBehaviour
{
    private Vector3 baseScale;
    private Vector3 basePosition;
    private float phase;

    private void Awake()
    {
        baseScale = transform.localScale;
        basePosition = transform.position;
        phase = transform.position.z * 0.19f;
    }

    private void Update()
    {
        float pulse = 1f + Mathf.Sin(Time.time * 2.4f + phase) * 0.14f;
        transform.localScale = baseScale * pulse;
        transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * 0.8f + phase) * 0.22f;
        transform.Rotate(Vector3.up, 24f * Time.deltaTime, Space.World);
    }
}

public sealed class MissionInteractable : MonoBehaviour
{
    public OceanMissionDirector director;
    public int knotIndex;
    public string displayName;
    public bool IsComplete { get; private set; }
    public string DisplayName => displayName;

    public void Activate()
    {
        if (!IsComplete) director?.TryUntie(knotIndex);
    }

    public void MarkComplete()
    {
        IsComplete = true;
        gameObject.SetActive(false);
    }
}

public sealed class FishingLineRescue : MonoBehaviour
{
    private readonly List<LineRenderer> loops = new();
    private readonly List<MissionInteractable> knots = new();
    private Renderer[] turtleRenderers;
    private Material lineMaterial;
    private Material knotMaterial;
    private OceanMissionDirector director;

    public int NextKnot { get; private set; }
    public int KnotCount => 3;

    public void Initialize(OceanMissionDirector owner)
    {
        director = owner;
        turtleRenderers = GetComponentsInChildren<Renderer>();
        lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineMaterial.color = new Color(0.44f, 0.55f, 0.50f, 1f);
        knotMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        knotMaterial.color = new Color(1f, 0.48f, 0.10f);
        knotMaterial.EnableKeyword("_EMISSION");
        knotMaterial.SetColor("_EmissionColor", new Color(1.25f, 0.26f, 0.04f));

        for (int i = 0; i < KnotCount; i++)
        {
            GameObject lineObject = new($"Old fishing line loop {i + 1}");
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 32;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.sharedMaterial = lineMaterial;
            loops.Add(line);

            GameObject knot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            knot.name = $"Fishing line knot {i + 1}";
            knot.GetComponent<Renderer>().sharedMaterial = knotMaterial;
            MissionInteractable interactable = knot.AddComponent<MissionInteractable>();
            interactable.director = director;
            interactable.knotIndex = i;
            interactable.displayName = $"旧鱼线结 {i + 1} / {KnotCount}";
            knots.Add(interactable);
        }
        UpdateGeometry();
    }

    private void LateUpdate() => UpdateGeometry();

    public void ReleaseKnot(int index)
    {
        if (index != NextKnot || index < 0 || index >= loops.Count) return;
        loops[index].gameObject.SetActive(false);
        knots[index].MarkComplete();
        NextKnot++;
    }

    public void RemoveAll()
    {
        foreach (LineRenderer line in loops)
        {
            if (line != null) Destroy(line.gameObject);
        }
        foreach (MissionInteractable knot in knots)
        {
            if (knot != null) Destroy(knot.gameObject);
        }
        loops.Clear();
        knots.Clear();
        if (lineMaterial != null) Destroy(lineMaterial);
        if (knotMaterial != null) Destroy(knotMaterial);
    }

    private void OnDestroy()
    {
        if (loops.Count > 0 || knots.Count > 0) RemoveAll();
    }

    private void UpdateGeometry()
    {
        if (turtleRenderers == null || turtleRenderers.Length == 0 || loops.Count == 0) return;
        Bounds bounds = turtleRenderers[0].bounds;
        for (int i = 1; i < turtleRenderers.Length; i++) bounds.Encapsulate(turtleRenderers[i].bounds);

        float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        Vector3 forward = -transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;
        for (int loopIndex = 0; loopIndex < loops.Count; loopIndex++)
        {
            LineRenderer line = loops[loopIndex];
            if (!line.gameObject.activeSelf) continue;
            line.startWidth = size * 0.012f;
            line.endWidth = size * 0.012f;
            Vector3 loopCenter = bounds.center + forward * ((loopIndex - 1) * size * 0.17f);
            float rightRadius = size * (0.25f - loopIndex * 0.018f);
            float upRadius = size * (0.17f + loopIndex * 0.012f);
            for (int point = 0; point < line.positionCount; point++)
            {
                float angle = point / (float)line.positionCount * Mathf.PI * 2f;
                Vector3 ripple = forward * Mathf.Sin(angle * 2f + loopIndex) * size * 0.018f;
                line.SetPosition(point, loopCenter + right * Mathf.Cos(angle) * rightRadius + up * Mathf.Sin(angle) * upRadius + ripple);
            }

            float knotAngle = 0.35f + loopIndex * 1.72f;
            Vector3 knotPosition = loopCenter
                + right * Mathf.Cos(knotAngle) * rightRadius
                + up * Mathf.Sin(knotAngle) * upRadius
                - forward * size * 0.045f;
            knots[loopIndex].transform.position = knotPosition;
            knots[loopIndex].transform.localScale = Vector3.one * size * 0.075f;
        }
    }
}
