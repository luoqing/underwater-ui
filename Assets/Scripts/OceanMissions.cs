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

    private enum MenuPage
    {
        Mission,
        Role
    }

    private enum PlayerRole
    {
        Turtle,
        Manta,
        Clownfish,
        Whale
    }

    public static OceanMissionDirector Instance { get; private set; }

    private readonly List<GameObject> adventureBeacons = new();
    private MissionMode mode;
    private MissionMode completedMission;
    private MissionMode pendingMission;
    private MenuPage menuPage;
    private PlayerRole selectedRole = PlayerRole.Turtle;
    private string selectedRoleName = "海龟";
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
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private GameObject rescueTurtle;
    private OceanCreatureDrift rescueDrift;
    private FishingLineRescue fishingLine;
    private CinematicDive diver;
    private AudioSource dialogueSource;
    private AudioClip pendingDialogueClip;

    public bool IsMissionActive => mode == MissionMode.Adventure || mode == MissionMode.Rescue;

    private void Awake()
    {
        Instance = this;
        chineseFont = Resources.Load<Font>("Fonts/DroidSansFallbackFull");
        panelTexture = MakeTexture(new Color(0.008f, 0.055f, 0.09f, 0.92f));
        accentTexture = MakeTexture(new Color(0.20f, 0.82f, 0.84f, 0.94f));
        buttonTexture = MakeTexture(new Color(0.025f, 0.20f, 0.26f, 0.96f));
        buttonHoverTexture = MakeTexture(new Color(0.04f, 0.34f, 0.40f, 0.98f));
        dialogueSource = gameObject.AddComponent<AudioSource>();
        dialogueSource.playOnAwake = false;
        dialogueSource.loop = false;
        dialogueSource.spatialBlend = 0f;
        dialogueSource.volume = 0.92f;
    }

    private void Start()
    {
        diver = Camera.main == null ? null : Camera.main.GetComponent<CinematicDive>();
        CreateAdventureBeacons();
        FindRescueTurtle();

        string url = Application.absoluteURL;
        if (url.Contains("mission=adventure"))
        {
            ChooseMission(MissionMode.Adventure);
        }
        else if (url.Contains("mission=rescue"))
        {
            ChooseMission(MissionMode.Rescue);
        }
        else
        {
            showMenu = !url.Contains("start=");
            menuPage = MenuPage.Mission;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (panelTexture != null) Destroy(panelTexture);
        if (accentTexture != null) Destroy(accentTexture);
        if (buttonTexture != null) Destroy(buttonTexture);
        if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
    }

    private void Update()
    {
        if (dialogueSource != null)
        {
            dialogueSource.mute = UnderwaterAudio.GlobalMuted;
            if (pendingDialogueClip != null && UnderwaterAudio.HasUserGesture())
            {
                PlayDialogueClip(pendingDialogueClip);
                pendingDialogueClip = null;
            }
        }
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
            float remainingHeight = Mathf.Max(0f, 6.2f - height);
            objective = $"双击海龟后按住拖动，带它向上游\n距离水面还差 {remainingHeight:0.0} 米";
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
            ShowDialogue("海龟悠悠", "先解开最靠近前鳍的结，再沿着鱼线往后找。这样不会越拉越紧。", 4.8f, "rescue_order");
            return;
        }

        fishingLine.ReleaseKnot(knotIndex);
        int remaining = fishingLine.KnotCount - fishingLine.NextKnot;
        if (remaining > 0)
        {
            objective = $"把光标移到下一个橙色绳结，单击解开\n还剩 {remaining} 个绳结";
            bool firstKnot = remaining == 2;
            ShowDialogue(
                "小丑鱼点点",
                firstKnot ? "第一处松开了！鱼线还绕在龟壳边缘。" : "只剩最后一个结，在礁石这一侧。",
                3.6f,
                firstKnot ? "rescue_knot_1" : "rescue_knot_2");
            return;
        }

        stage = 1;
        if (rescueDrift != null) rescueDrift.enabled = true;
        objective = "双击海龟后按住拖动，带它向上游\n松开后海龟会沿当前方向继续游";
        ShowDialogue("海龟豆豆", "鱼线解开了！请带我往上游。我松开以后，也会沿着你给的方向继续前进。", 6.5f, "rescue_freed");
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
            if (GUI.Button(GetMissionButtonRect(), "任务", buttonStyle))
            {
                menuPage = MenuPage.Mission;
                showMenu = true;
            }
            return;
        }

        Rect panel = GetMissionRect();
        GUI.DrawTexture(panel, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panel.x, panel.y, 5f, panel.height), accentTexture, ScaleMode.StretchToFill);
        float scale = UiScale();
        GUI.Label(new Rect(panel.x + 20f * scale, panel.y + 12f * scale, panel.width - 40f * scale, 34f * scale), missionTitle, titleStyle);
        GUI.Label(new Rect(panel.x + 20f * scale, panel.y + 50f * scale, 72f * scale, 27f * scale), "下一步", buttonStyle);
        DrawObjective(new Rect(panel.x + 104f * scale, panel.y + 48f * scale, panel.width - 126f * scale, 82f * scale));
        if (GUI.Button(new Rect(panel.x + panel.width - 116f * scale, panel.y + panel.height - 38f * scale, 96f * scale, 28f * scale), "任务菜单", buttonStyle))
        {
            ResetMission();
            menuPage = MenuPage.Mission;
            showMenu = true;
        }

        DrawTargetGuide();

        if (Time.unscaledTime < dialogueUntil && !string.IsNullOrEmpty(dialogue))
        {
            float dialogueWidth = Mathf.Min(760f * scale, Screen.width - 36f * scale);
            Rect dialoguePanel = new((Screen.width - dialogueWidth) * 0.5f, Screen.height - 188f * scale, dialogueWidth, 116f * scale);
            GUI.DrawTexture(dialoguePanel, panelTexture, ScaleMode.StretchToFill);
            GUI.Label(new Rect(dialoguePanel.x + 22f * scale, dialoguePanel.y + 10f * scale, 180f * scale, 30f * scale), speaker, titleStyle);
            GUI.Label(new Rect(dialoguePanel.x + 22f * scale, dialoguePanel.y + 43f * scale, dialoguePanel.width - 44f * scale, 60f * scale), dialogue, bodyStyle);
        }
    }

    private void DrawMissionMenu()
    {
        if (menuPage == MenuPage.Role)
        {
            DrawRoleMenu();
            return;
        }

        Rect panel = GetMenuRect();
        GUI.DrawTexture(panel, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f), accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(panel.x + 28f, panel.y + 22f, panel.width - 56f, 42f), "选择今天的海底任务", titleStyle);
        GUI.Label(new Rect(panel.x + 32f, panel.y + 68f, panel.width - 64f, 64f), "两个任务都没有失败惩罚。迷路了会回到上一处提示，可以慢慢探索。", bodyStyle);

        if (GUI.Button(new Rect(panel.x + 34f, panel.y + 140f, panel.width - 68f, 66f), "探险：追着鲸歌找星光", buttonStyle))
        {
            ChooseMission(MissionMode.Adventure);
        }
        if (GUI.Button(new Rect(panel.x + 34f, panel.y + 218f, panel.width - 68f, 66f), "救援：送小海龟去呼吸", buttonStyle))
        {
            ChooseMission(MissionMode.Rescue);
        }
        if (GUI.Button(new Rect(panel.x + panel.width - 126f, panel.y + panel.height - 38f, 94f, 25f), "继续漫游", buttonStyle))
        {
            showMenu = false;
        }
    }

    private void DrawRoleMenu()
    {
        Rect panel = GetMenuRect();
        GUI.DrawTexture(panel, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f), accentTexture, ScaleMode.StretchToFill);
        GUI.Label(new Rect(panel.x + 28f, panel.y + 20f, panel.width - 56f, 38f), "选择你要扮演的海洋伙伴", titleStyle);

        string missionName = pendingMission == MissionMode.Rescue ? "救援：送小海龟去呼吸" : "探险：追着鲸歌找星光";
        GUI.Label(new Rect(panel.x + 30f, panel.y + 60f, panel.width - 60f, 48f), $"当前任务：{missionName}\n体型会决定你能穿过多窄的缝隙。", bodyStyle);

        float gap = 12f;
        float buttonWidth = (panel.width - 80f - gap) * 0.5f;
        float left = panel.x + 34f;
        float right = left + buttonWidth + gap;
        if (GUI.Button(new Rect(left, panel.y + 122f, buttonWidth, 76f), RoleButtonText(PlayerRole.Turtle), buttonStyle))
        {
            StartAsRole(PlayerRole.Turtle);
        }
        if (GUI.Button(new Rect(right, panel.y + 122f, buttonWidth, 76f), RoleButtonText(PlayerRole.Manta), buttonStyle))
        {
            StartAsRole(PlayerRole.Manta);
        }
        if (GUI.Button(new Rect(left, panel.y + 212f, buttonWidth, 76f), RoleButtonText(PlayerRole.Clownfish), buttonStyle))
        {
            StartAsRole(PlayerRole.Clownfish);
        }
        if (GUI.Button(new Rect(right, panel.y + 212f, buttonWidth, 76f), RoleButtonText(PlayerRole.Whale), buttonStyle))
        {
            StartAsRole(PlayerRole.Whale);
        }
        if (GUI.Button(new Rect(panel.x + 32f, panel.y + panel.height - 38f, 94f, 25f), "返回选任务", buttonStyle))
        {
            pendingMission = MissionMode.None;
            menuPage = MenuPage.Mission;
        }
    }

    private void ChooseMission(MissionMode mission)
    {
        pendingMission = mission;
        menuPage = MenuPage.Role;
        showMenu = true;
    }

    private void StartAsRole(PlayerRole role)
    {
        selectedRole = role;
        selectedRoleName = RoleName(role);
        diver?.ConfigurePlayerRole(RoleKey(role));
        MissionMode mission = pendingMission;
        if (mission == MissionMode.Adventure)
        {
            StartAdventure();
        }
        else if (mission == MissionMode.Rescue)
        {
            StartRescue();
        }
    }

    private string RoleButtonText(PlayerRole role)
    {
        if (role == PlayerRole.Turtle && pendingMission == MissionMode.Rescue)
        {
            return "海龟\n均衡 · 扮演豆豆的海龟伙伴";
        }

        return role switch
        {
            PlayerRole.Turtle => "海龟\n均衡 · 稳定转向",
            PlayerRole.Manta => "魔鬼鱼\n宽阔 · 平稳滑行",
            PlayerRole.Clownfish => "小丑鱼\n灵活 · 能钻窄缝",
            PlayerRole.Whale => "鲸鱼\n巨大 · 适合开阔水道",
            _ => "海龟"
        };
    }

    private static string RoleName(PlayerRole role) => role switch
    {
        PlayerRole.Turtle => "海龟",
        PlayerRole.Manta => "魔鬼鱼",
        PlayerRole.Clownfish => "小丑鱼",
        PlayerRole.Whale => "鲸鱼",
        _ => "海龟"
    };

    private static string RoleKey(PlayerRole role) => role switch
    {
        PlayerRole.Turtle => "turtle",
        PlayerRole.Manta => "manta",
        PlayerRole.Clownfish => "clownfish",
        PlayerRole.Whale => "whale",
        _ => "turtle"
    };

    private void StartAdventure()
    {
        ResetMission();
        showMenu = false;
        mode = MissionMode.Adventure;
        missionTitle = $"追着鲸歌找星光 · {selectedRoleName}";
        stage = 0;
        adventureBeacons[0].SetActive(true);
        diver?.StartMissionAt(new Vector3(0f, 4.3f, -7f), new Vector3(0f, 2.3f, 18f));
        ShowDialogue("海龟悠悠", "昨晚的洋流冲散了海底拱门的三盏星光灯。跟着鱼群和鲸歌，把回家的路重新点亮吧。", 7f, "adventure_intro");
    }

    private void ReachAdventureBeacon()
    {
        adventureBeacons[stage].SetActive(false);
        stage++;
        if (stage == 1)
        {
            adventureBeacons[stage].SetActive(true);
            ShowDialogue("小丑鱼点点", "第一盏亮了！鱼群转弯时会一起改变方向。下一盏灯在魔鬼鱼经过的水道。", 6f, "adventure_beacon_1");
        }
        else if (stage == 2)
        {
            adventureBeacons[stage].SetActive(true);
            ShowDialogue("魔鬼鱼阿翼", "跟在我的背后，水流会平稳一些。听见更清楚的鲸歌，就朝那个方向游。", 6f, "adventure_beacon_2");
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
        missionTitle = $"送小海龟去呼吸 · {selectedRoleName}";
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
        objective = selectedRole == PlayerRole.Turtle
            ? "把光标移到前鳍旁的橙色绳结，单击解开\n第 1 / 3 个绳结"
            : "找到海龟前鳍旁的橙色绳结，单击解开\n第 1 / 3 个绳结";
        ShowDialogue("小丑鱼点点", "海龟豆豆被旧鱼线缠住了。它没有受伤，但要先解开三个绳结，才能游到水面呼吸。", 7f, "rescue_intro");
    }

    private void CompleteMission(string who, string line, string reward)
    {
        completedMission = mode;
        mode = MissionMode.Complete;
        missionTitle = completedMission == MissionMode.Adventure ? "探险完成" : "救援完成";
        objective = reward;
        ShowDialogue(who, line, 12f, completedMission == MissionMode.Adventure ? "adventure_complete" : "rescue_complete");
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
        pendingMission = MissionMode.None;
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
        Material glass = MissionMaterialFactory.CreateLit("Star lantern glass");
        glass.color = new Color(0.20f, 0.68f, 0.72f, 1f);
        glass.EnableKeyword("_EMISSION");
        glass.SetColor("_EmissionColor", new Color(0.10f, 0.88f, 1.05f));
        Material agedMetal = MissionMaterialFactory.CreateLit("Star lantern aged metal");
        agedMetal.color = new Color(0.10f, 0.16f, 0.17f, 1f);
        agedMetal.SetFloat("_Metallic", 0.72f);
        agedMetal.SetFloat("_Smoothness", 0.34f);
        foreach (Vector3 position in positions)
        {
            GameObject beacon = new("Star Route Lantern");
            beacon.name = "Star Route Beacon";
            beacon.transform.SetParent(transform);
            beacon.transform.position = position;
            CreateBeaconPart(beacon.transform, PrimitiveType.Cylinder, new Vector3(0f, 0f, 0f), new Vector3(0.46f, 0.56f, 0.46f), glass);
            CreateBeaconPart(beacon.transform, PrimitiveType.Cylinder, new Vector3(0f, -0.68f, 0f), new Vector3(0.58f, 0.10f, 0.58f), agedMetal);
            CreateBeaconPart(beacon.transform, PrimitiveType.Cylinder, new Vector3(0f, 0.68f, 0f), new Vector3(0.54f, 0.10f, 0.54f), agedMetal);
            CreateBeaconPart(beacon.transform, PrimitiveType.Sphere, new Vector3(0f, 0.92f, 0f), new Vector3(0.16f, 0.18f, 0.16f), agedMetal);
            for (int bar = 0; bar < 4; bar++)
            {
                float angle = bar * Mathf.PI * 0.5f;
                CreateBeaconPart(
                    beacon.transform,
                    PrimitiveType.Cylinder,
                    new Vector3(Mathf.Cos(angle) * 0.48f, 0f, Mathf.Sin(angle) * 0.48f),
                    new Vector3(0.035f, 0.62f, 0.035f),
                    agedMetal);
            }
            Light light = beacon.AddComponent<Light>();
            light.color = new Color(0.36f, 0.88f, 1f);
            light.intensity = 1.35f;
            light.range = 8.5f;
            light.shadows = LightShadows.None;
            beacon.AddComponent<MissionBeacon>();
            beacon.SetActive(false);
            adventureBeacons.Add(beacon);
        }
    }

    private static void CreateBeaconPart(Transform parent, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(part.GetComponent<Collider>());
    }

    private string AdventureObjective(int currentStage, float distance)
    {
        string[] prompts =
        {
            "跟随黄色鱼群，找到珊瑚花园的第一盏灯",
            "穿过魔鬼鱼经过的水道，找到第二盏灯",
            "听着鲸歌前往岩石拱门，点亮最后一盏灯"
        };
        int roundedDistance = Mathf.Max(0, Mathf.RoundToInt(distance));
        return $"按住 W 或「前进」，{prompts[Mathf.Clamp(currentStage, 0, prompts.Length - 1)]}\n第 {currentStage + 1} / 3 盏 · 距离 {roundedDistance} 米";
    }

    private void ShowDialogue(string who, string line, float seconds, string clipName)
    {
        speaker = who;
        dialogue = line;
        AudioClip clip = Resources.Load<AudioClip>($"Audio/Dialogues/{clipName}");
        if (clip != null && dialogueSource != null)
        {
            if (UnderwaterAudio.HasUserGesture())
            {
                PlayDialogueClip(clip);
            }
            else
            {
                pendingDialogueClip = clip;
            }
            seconds = Mathf.Max(seconds, clip.length + 0.35f);
        }
        dialogueUntil = Time.unscaledTime + seconds;
    }

    private void PlayDialogueClip(AudioClip clip)
    {
        dialogueSource.Stop();
        dialogueSource.clip = clip;
        dialogueSource.Play();
    }

    private void DrawObjective(Rect area)
    {
        string[] lines = (objective ?? string.Empty).Split('\n');
        float lineHeight = area.height * 0.48f;
        if (lines.Length > 0)
        {
            GUI.Label(new Rect(area.x, area.y, area.width, lineHeight), lines[0], objectiveStyle);
        }
        if (lines.Length > 1)
        {
            GUI.Label(new Rect(area.x, area.y + lineHeight, area.width, lineHeight), lines[1], bodyStyle);
        }
    }

    private void DrawTargetGuide()
    {
        if (!TryGetCurrentTarget(out Vector3 target)) return;
        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 screen = camera.WorldToScreenPoint(target);
        if (screen.z < 0f)
        {
            screen.x = Screen.width - screen.x;
            screen.y = Screen.height - screen.y;
        }

        float scale = UiScale();
        float width = 108f * scale;
        float height = 42f * scale;
        float x = Mathf.Clamp(screen.x - width * 0.5f, 14f * scale, Screen.width - width - 14f * scale);
        float y = Mathf.Clamp(Screen.height - screen.y - height * 0.5f, 218f * scale, Screen.height - height - 84f * scale);
        Rect marker = new(x, y, width, height);
        GUI.DrawTexture(marker, buttonTexture, ScaleMode.StretchToFill);
        int distance = Mathf.Max(0, Mathf.RoundToInt(Vector3.Distance(camera.transform.position, target)));
        GUI.Label(marker, $"目标  {distance} 米", buttonStyle);
    }

    private bool TryGetCurrentTarget(out Vector3 target)
    {
        if (mode == MissionMode.Adventure && stage >= 0 && stage < adventureBeacons.Count)
        {
            target = adventureBeacons[stage].transform.position;
            return true;
        }
        if (mode == MissionMode.Rescue && stage == 0 && fishingLine != null && fishingLine.TryGetNextKnotPosition(out target))
        {
            return true;
        }
        if (mode == MissionMode.Rescue && stage == 1 && rescueTurtle != null)
        {
            target = rescueTurtle.transform.position + Vector3.up * 6.2f;
            return true;
        }
        target = default;
        return false;
    }

    private void EnsureStyles()
    {
        float scale = UiScale();
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            fontSize = Mathf.RoundToInt(22f * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            wordWrap = true
        };
        titleStyle.normal.textColor = new Color(0.82f, 0.98f, 1f);

        bodyStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            fontSize = Mathf.RoundToInt(17f * scale),
            alignment = TextAnchor.UpperLeft,
            wordWrap = true
        };
        bodyStyle.normal.textColor = new Color(0.90f, 0.97f, 0.98f);

        objectiveStyle ??= new GUIStyle(bodyStyle)
        {
            fontSize = Mathf.RoundToInt(18f * scale),
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.UpperLeft
        };
        objectiveStyle.normal.textColor = new Color(0.58f, 0.96f, 0.92f);

        buttonStyle ??= new GUIStyle(GUI.skin.button)
        {
            font = chineseFont,
            fontSize = Mathf.RoundToInt(15f * scale),
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            padding = new RectOffset(10, 10, 5, 5)
        };
        buttonStyle.normal.background = buttonTexture;
        buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.background = buttonHoverTexture;
        buttonStyle.normal.textColor = new Color(0.88f, 0.98f, 1f);
        buttonStyle.hover.textColor = Color.white;
        buttonStyle.active.textColor = Color.white;
    }

    private static float UiScale() => Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 900f), 0.82f, 1.35f);

    private static Rect GetMenuRect()
    {
        float scale = UiScale();
        float width = Mathf.Min(620f * scale, Screen.width - 40f);
        float height = Mathf.Min(430f * scale, Screen.height - 30f);
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    private static Rect GetMissionRect()
    {
        float scale = UiScale();
        float width = Mathf.Min(540f * scale, Screen.width * 0.54f);
        return new Rect(Screen.width - width - 22f * scale, 68f * scale, width, 176f * scale);
    }

    private static Rect GetMissionButtonRect()
    {
        float scale = UiScale();
        return new Rect(Screen.width - 146f * scale, 68f * scale, 124f * scale, 40f * scale);
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
        lineMaterial = MissionMaterialFactory.CreateLit("Old fishing line material");
        lineMaterial.color = new Color(0.44f, 0.55f, 0.50f, 1f);
        knotMaterial = MissionMaterialFactory.CreateLit("Fishing line knot material");
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

    public bool TryGetNextKnotPosition(out Vector3 position)
    {
        if (NextKnot >= 0 && NextKnot < knots.Count && knots[NextKnot] != null)
        {
            position = knots[NextKnot].transform.position;
            return true;
        }

        position = default;
        return false;
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

internal static class MissionMaterialFactory
{
    public static Material CreateLit(string materialName)
    {
        Material template = Resources.Load<Material>("Materials/UnderwaterLit");
        if (template == null)
        {
            throw new MissingReferenceException("Missing Resources/Materials/UnderwaterLit material.");
        }
        return new Material(template) { name = materialName };
    }
}
