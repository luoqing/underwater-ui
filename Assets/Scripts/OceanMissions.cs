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
    private GUIStyle menuButtonStyle;
    private GUIStyle markerStyle;
    private GUIStyle subtitleStyle;
    private Texture2D panelTexture;
    private Texture2D accentTexture;
    private Texture2D buttonTexture;
    private Texture2D buttonHoverTexture;
    private GameObject rescueTurtle;
    private Vector3 rescueStartPosition;
    private MarineEncounter rescueEncounter;
    private string rescueDisplayName;
    private OceanCreatureDrift rescueDrift;
    private FishingLineRescue fishingLine;
    private CinematicDive diver;
    private AudioSource dialogueSource;
    private AudioClip pendingDialogueClip;
    private Vector2 menuScroll;
    private bool showSubtitles;

    private const float RescueSurfaceHeight = UnderwaterWorld.WaterSurfaceHeight - 0.3f;

    public bool IsMissionActive => mode == MissionMode.Adventure || mode == MissionMode.Rescue;
    public bool IsMenuOpen => showMenu;
    public bool SubtitlesVisible => showSubtitles && Time.unscaledTime < dialogueUntil && !string.IsNullOrEmpty(dialogue);
    private string CurrentObjective => mode == MissionMode.None
        ? UnderwaterHud.Instance?.FreeRoamObjective ?? "四处游游，认识海洋伙伴" : objective;
    public string ActionHint => mode != MissionMode.Rescue ? string.Empty
        : stage == 0 ? "点一下标记处的橙色绳结"
        : diver != null && rescueEncounter != null && diver.GuidedEncounter == rescueEncounter
            ? diver.IsGuidingAnimal ? "向上拖，带豆豆靠近水面" : "按住豆豆，向上拖"
            : "双击豆豆，选中它";

    private void Awake()
    {
        Instance = this;
        chineseFont = Resources.Load<Font>("Fonts/BlueLanternUI");
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
            objective = AdventureObjective(stage);
            if (distance < 4.2f)
            {
                ReachAdventureBeacon();
            }
        }
        else if (mode == MissionMode.Rescue && stage == 1 && rescueTurtle != null)
        {
            float height = rescueTurtle.transform.position.y;
            objective = "带豆豆浮到水面";
            if (height >= RescueSurfaceHeight)
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
        if (showMenu) return true;
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
            objective = $"点一下豆豆身上的橙色绳结（{fishingLine.NextKnot + 1}/3）";
            bool firstKnot = remaining == 2;
            ShowDialogue(
                "小丑鱼点点",
                firstKnot ? "第一处松开了！鱼线还绕在龟壳边缘。" : "只剩最后一个结，在礁石这一侧。",
                3.6f,
                firstKnot ? "rescue_knot_1" : "rescue_knot_2");
            return;
        }

        stage = 1;
        rescueDrift?.SetEntangled(false);
        if (rescueDrift != null) rescueDrift.enabled = true;
        objective = "带豆豆浮到水面";
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

        if (GUI.Button(GetMissionButtonRect(), "菜单", menuButtonStyle))
        {
            OpenMenu();
            return;
        }

        float scale = UiScale();
        Rect goal = GetObjectiveRect();
        GUI.DrawTexture(new Rect(24f * scale, goal.y + 12f * scale, 6f * scale, 6f * scale), accentTexture);
        UnderwaterHud.DrawOverlayLabel(goal, CurrentObjective, objectiveStyle);

        if (SubtitlesVisible)
        {
            UnderwaterHud.DrawOverlayLabel(GetDialogueRect(), $"{speaker}：{dialogue}", subtitleStyle);
        }
        DrawTargetGuide();
    }

    private void OpenMenu()
    {
        menuPage = MenuPage.Mission;
        menuScroll = Vector2.zero;
        showMenu = true;
        diver?.SetHudMove(Vector3.zero);
    }

    private void DrawMissionMenu()
    {
        if (menuPage == MenuPage.Role)
        {
            DrawRoleMenu();
            return;
        }

        Rect panel = GetMenuRect();
        float scale = UiScale();
        GUI.DrawTexture(panel, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f * scale), accentTexture, ScaleMode.StretchToFill);
        menuScroll = GUI.BeginScrollView(panel, menuScroll, new Rect(0f, 0f, panel.width - 18f, 430f * scale));
        float width = panel.width - 18f;
        GUI.Label(new Rect(28f * scale, 22f * scale, width - 56f * scale, 42f * scale), "选择今天的海底任务", titleStyle);
        GUI.Label(new Rect(32f * scale, 68f * scale, width - 64f * scale, 64f * scale), "选一个任务，再选你的海洋伙伴。", bodyStyle);

        if (GUI.Button(new Rect(34f * scale, 140f * scale, width - 68f * scale, 66f * scale), "探险：追着鲸歌找星光", buttonStyle))
        {
            ChooseMission(MissionMode.Adventure);
        }
        if (GUI.Button(new Rect(34f * scale, 218f * scale, width - 68f * scale, 66f * scale), "救援：送小海龟去呼吸", buttonStyle))
        {
            ChooseMission(MissionMode.Rescue);
        }
        showSubtitles = GUI.Toggle(new Rect(34f * scale, 312f * scale, width - 68f * scale, 38f * scale), showSubtitles, showSubtitles ? "对白字幕：开" : "对白字幕：关", menuButtonStyle);
        if (mode != MissionMode.None && GUI.Button(new Rect(34f * scale, 382f * scale, 124f * scale, 32f * scale), "退出任务", buttonStyle))
        {
            ResetMission();
            showMenu = false;
        }
        if (GUI.Button(new Rect(width - 152f * scale, 382f * scale, 124f * scale, 32f * scale), IsMissionActive ? "继续任务" : "继续漫游", buttonStyle))
        {
            showMenu = false;
        }
        GUI.EndScrollView();
    }

    private void DrawRoleMenu()
    {
        Rect panel = GetMenuRect();
        float scale = UiScale();
        GUI.DrawTexture(panel, panelTexture, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 5f * scale), accentTexture, ScaleMode.StretchToFill);
        menuScroll = GUI.BeginScrollView(panel, menuScroll, new Rect(0f, 0f, panel.width - 18f, 430f * scale));
        float width = panel.width - 18f;
        GUI.Label(new Rect(28f * scale, 20f * scale, width - 56f * scale, 38f * scale), "选择你要扮演的海洋伙伴", titleStyle);

        string missionName = pendingMission == MissionMode.Rescue ? "救援：送小海龟去呼吸" : "探险：追着鲸歌找星光";
        GUI.Label(new Rect(30f * scale, 66f * scale, width - 60f * scale, 56f * scale), $"当前任务：{missionName}\n体型会决定你能穿过多窄的缝隙。", bodyStyle);

        float gap = 12f * scale;
        float buttonWidth = (width - 68f * scale - gap) * 0.5f;
        float left = 34f * scale;
        float right = left + buttonWidth + gap;
        if (GUI.Button(new Rect(left, 134f * scale, buttonWidth, 76f * scale), RoleButtonText(PlayerRole.Turtle), buttonStyle))
        {
            StartAsRole(PlayerRole.Turtle);
        }
        if (GUI.Button(new Rect(right, 134f * scale, buttonWidth, 76f * scale), RoleButtonText(PlayerRole.Manta), buttonStyle))
        {
            StartAsRole(PlayerRole.Manta);
        }
        if (GUI.Button(new Rect(left, 224f * scale, buttonWidth, 76f * scale), RoleButtonText(PlayerRole.Clownfish), buttonStyle))
        {
            StartAsRole(PlayerRole.Clownfish);
        }
        if (GUI.Button(new Rect(right, 224f * scale, buttonWidth, 76f * scale), RoleButtonText(PlayerRole.Whale), buttonStyle))
        {
            StartAsRole(PlayerRole.Whale);
        }
        if (GUI.Button(new Rect(32f * scale, 382f * scale, 124f * scale, 32f * scale), "返回选任务", buttonStyle))
        {
            pendingMission = MissionMode.None;
            menuPage = MenuPage.Mission;
        }
        GUI.EndScrollView();
    }

    private void ChooseMission(MissionMode mission)
    {
        pendingMission = mission;
        menuPage = MenuPage.Role;
        menuScroll = Vector2.zero;
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
        stage = 0;
        adventureBeacons[0].SetActive(true);
        objective = AdventureObjective(stage);
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
        stage = 0;

        if (rescueTurtle == null)
        {
            objective = "暂时找不到救援海龟，请返回任务菜单重试";
            return;
        }

        rescueDrift = rescueTurtle.GetComponent<OceanCreatureDrift>();
        Vector3 rescuePosition = rescueStartPosition;
        rescuePosition.y = UnderwaterWorld.WaterSurfaceHeight - 6f;
        rescueTurtle.transform.position = rescuePosition;
        if (rescueEncounter != null) rescueEncounter.displayName = "海龟豆豆";
        rescueDrift?.SetEntangled(true);
        fishingLine = rescueTurtle.AddComponent<FishingLineRescue>();
        fishingLine.Initialize(this);
        Vector3 turtlePosition = rescueTurtle.transform.position;
        diver?.StartMissionAt(turtlePosition + new Vector3(-5.5f, 1.5f, -8f), turtlePosition);
        objective = "点一下豆豆前鳍旁的橙色绳结（1/3）";
        ShowDialogue("小丑鱼点点", "海龟豆豆被旧鱼线缠住了。它没有受伤，但要先解开三个绳结，才能游到水面呼吸。", 7f, "rescue_intro");
    }

    private void CompleteMission(string who, string line, string reward)
    {
        completedMission = mode;
        mode = MissionMode.Complete;
        objective = completedMission == MissionMode.Adventure ? "三盏星光灯都亮了，探险完成！" : "豆豆换好气了，救援完成！";
        UnderwaterHud.Instance?.ShowStatus(reward);
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
        if (rescueDrift != null)
        {
            rescueDrift.SetEntangled(false);
            rescueDrift.enabled = true;
        }
        rescueDrift = null;
        if (rescueEncounter != null) rescueEncounter.displayName = rescueDisplayName;
        mode = MissionMode.None;
        pendingMission = MissionMode.None;
        stage = 0;
        objective = string.Empty;
        dialogue = string.Empty;
        speaker = string.Empty;
        dialogueUntil = 0f;
        pendingDialogueClip = null;
        if (dialogueSource != null)
        {
            dialogueSource.Stop();
            dialogueSource.clip = null;
        }
        menuScroll = Vector2.zero;
    }

    private void FindRescueTurtle()
    {
        foreach (OceanCreatureDrift creature in FindObjectsByType<OceanCreatureDrift>(FindObjectsSortMode.None))
        {
            if (!creature.useMantaAxes && creature.name.Contains("Companion"))
            {
                rescueTurtle = creature.gameObject;
                rescueStartPosition = creature.transform.position;
                rescueEncounter = creature.GetComponent<MarineEncounter>();
                if (rescueEncounter != null) rescueDisplayName = rescueEncounter.displayName;
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

    private string AdventureObjective(int currentStage)
    {
        string[] prompts =
        {
            "游向珊瑚花园，靠近星光灯（1/3）",
            "游向魔鬼鱼水道，靠近星光灯（2/3）",
            "游向岩石拱门，靠近星光灯（3/3）"
        };
        return prompts[Mathf.Clamp(currentStage, 0, prompts.Length - 1)];
    }

    private void ShowDialogue(string who, string line, float seconds, string clipName)
    {
        pendingDialogueClip = null;
        if (dialogueSource != null) dialogueSource.Stop();
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

    private void DrawTargetGuide()
    {
        if (!TryGetCurrentTarget(out Vector3 target)) return;
        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 screen = camera.WorldToScreenPoint(target);
        Vector3 localTarget = camera.transform.InverseTransformPoint(target);
        string direction = string.Empty;
        if (screen.z < 0f) direction = localTarget.x < 0f ? "向左转" : "向右转";
        else if (screen.x < 0f) direction = "向左转";
        else if (screen.x > Screen.width) direction = "向右转";
        else if (screen.y < 0f) direction = "向下看";
        else if (screen.y > Screen.height) direction = "向上看";
        if (screen.z < 0f)
        {
            screen.x = Screen.width - screen.x;
            screen.y = Screen.height - screen.y;
        }

        float scale = UiScale();
        int distance = Mathf.Max(0, Mathf.RoundToInt(Vector3.Distance(camera.transform.position, target)));
        if (mode == MissionMode.Rescue && stage == 1)
            distance = Mathf.CeilToInt(Mathf.Max(0f, RescueSurfaceHeight - rescueTurtle.transform.position.y));
        string label = mode == MissionMode.Adventure ? "星光灯" : stage == 1 ? "水面" : "绳结";
        string text = string.IsNullOrEmpty(direction) ? $"{label} · {distance} 米" : $"{direction} · {distance} 米";
        float width = markerStyle.CalcSize(new GUIContent(text)).x + 12f * scale;
        float height = 28f * scale;
        float top = GetObjectiveRect().yMax + 16f * scale;
        float x = Mathf.Clamp(screen.x - width * 0.5f, 14f * scale, Screen.width - width - 14f * scale);
        float bottom = TravelControlsTop() - height - 12f * scale;
        float y = Mathf.Clamp(Screen.height - screen.y - height - 18f * scale, top, Mathf.Max(top, bottom));
        Rect marker = new(x, y, width, height);
        Rect discoveryPanel = UnderwaterHud.GetDiscoveryRect();
        if (marker.Overlaps(discoveryPanel)) marker.x = Mathf.Max(14f * scale, discoveryPanel.x - width - 8f * scale);
        if (SubtitlesVisible && marker.Overlaps(GetDialogueRect())) marker.y = GetDialogueRect().y - height - 8f * scale;
        UnderwaterHud.DrawOverlayLabel(marker, text, markerStyle);
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
            Vector3 turtlePosition = rescueTurtle.transform.position;
            target = new Vector3(turtlePosition.x, RescueSurfaceHeight, turtlePosition.z);
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
            fontSize = Mathf.RoundToInt(22f * scale),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperLeft
        };
        objectiveStyle.normal.textColor = new Color(0.97f, 0.97f, 0.91f);

        menuButtonStyle ??= new GUIStyle(GUI.skin.label)
        {
            font = chineseFont,
            alignment = TextAnchor.MiddleCenter
        };
        menuButtonStyle.normal.textColor = new Color(0.9f, 0.97f, 0.98f);
        menuButtonStyle.hover.background = buttonHoverTexture;
        menuButtonStyle.onNormal.textColor = new Color(0.45f, 0.95f, 0.90f);
        markerStyle ??= new GUIStyle(menuButtonStyle);
        subtitleStyle ??= new GUIStyle(bodyStyle) { alignment = TextAnchor.MiddleCenter };

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
        titleStyle.fontSize = Mathf.RoundToInt(22f * scale);
        bodyStyle.fontSize = Mathf.RoundToInt(17f * scale);
        objectiveStyle.fontSize = Mathf.RoundToInt(22f * scale);
        buttonStyle.fontSize = Mathf.RoundToInt(15f * scale);
        menuButtonStyle.fontSize = Mathf.RoundToInt(15f * scale);
        markerStyle.fontSize = Mathf.RoundToInt(14f * scale);
        subtitleStyle.fontSize = Mathf.RoundToInt(15f * scale);
    }

    private static float UiScale() => Mathf.Clamp(Mathf.Min(Screen.width / 1440f, Screen.height / 900f), 0.82f, 1.35f);

    private static Rect GetMenuRect()
    {
        float scale = UiScale();
        float width = Mathf.Min(620f * scale, Screen.width - 40f);
        float height = Mathf.Min(430f * scale, Screen.height - 30f);
        return new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
    }

    private Rect GetObjectiveRect()
    {
        float scale = UiScale();
        float width = Screen.width - 240f * scale;
        float height = objectiveStyle == null ? 56f * scale : Mathf.Max(32f * scale, objectiveStyle.CalcHeight(new GUIContent(CurrentObjective), width));
        return new Rect(42f * scale, 22f * scale, width, height);
    }

    private Rect GetDialogueRect()
    {
        float scale = UiScale();
        Rect[] controls = UnderwaterHud.GetTravelControlRects();
        float left = controls[3].xMax + 12f;
        float right = controls[4].xMin - 12f;
        float width = Mathf.Min(760f * scale, right - left);
        float height = subtitleStyle.CalcHeight(new GUIContent($"{speaker}：{dialogue}"), width);
        float x = left + (right - left - width) * 0.5f;
        return new Rect(x, Screen.height - 80f - height, width, height);
    }

    private static float TravelControlsTop()
    {
        return UnderwaterHud.GetTravelControlRects()[4].y;
    }

    private static Rect GetMissionButtonRect()
    {
        float scale = UiScale();
        return new Rect(Screen.width - 80f * scale, 20f * scale, 56f * scale, 34f * scale);
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
        lineMaterial = new Material(Resources.Load<Material>("Materials/TransparentGlow"))
        {
            name = "Thin translucent fishing line"
        };
        lineMaterial.SetColor("_BaseColor", new Color(0.62f, 0.70f, 0.65f, 0.62f));
        lineMaterial.SetFloat("_EmissionStrength", 0.85f);
        lineMaterial.SetFloat("_Speed", 0f);
        knotMaterial = MissionMaterialFactory.CreateLit("Fishing line knot material");
        knotMaterial.color = new Color(0.88f, 0.39f, 0.11f);
        knotMaterial.SetFloat("_Smoothness", 0.24f);
        knotMaterial.EnableKeyword("_EMISSION");
        knotMaterial.SetColor("_EmissionColor", new Color(0.34f, 0.09f, 0.012f));

        for (int i = 0; i < KnotCount; i++)
        {
            GameObject lineObject = new($"Old fishing line loop {i + 1}");
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 64;
            line.numCapVertices = 4;
            line.numCornerVertices = 3;
            line.sharedMaterial = lineMaterial;
            loops.Add(line);

            GameObject knot = new($"Fishing line knot {i + 1}");
            knot.AddComponent<SphereCollider>().radius = 1.15f;
            CreateKnotStrands(knot.transform);
            MissionInteractable interactable = knot.AddComponent<MissionInteractable>();
            interactable.director = director;
            interactable.knotIndex = i;
            interactable.displayName = $"旧鱼线结 {i + 1} / {KnotCount}";
            knots.Add(interactable);
        }
        UpdateGeometry();
    }

    private void CreateKnotStrands(Transform parent)
    {
        const int points = 96;
        for (int strand = 0; strand < 3; strand++)
        {
            GameObject strandObject = new($"Twisted rope strand {strand + 1}");
            strandObject.transform.SetParent(parent, false);
            LineRenderer rope = strandObject.AddComponent<LineRenderer>();
            rope.useWorldSpace = false;
            rope.loop = true;
            rope.positionCount = points;
            rope.startWidth = rope.endWidth = 0.065f;
            rope.numCornerVertices = 2;
            rope.generateLightingData = true;
            rope.sharedMaterial = knotMaterial;
            for (int point = 0; point < points; point++)
            {
                float t = point * Mathf.PI * 2f / points;
                Vector3 center = new Vector3(Mathf.Sin(t) + 2f * Mathf.Sin(2f * t), Mathf.Cos(t) - 2f * Mathf.Cos(2f * t), -Mathf.Sin(3f * t)) * 0.25f;
                Vector3 tangent = new(Mathf.Cos(t) + 4f * Mathf.Cos(2f * t), -Mathf.Sin(t) + 4f * Mathf.Sin(2f * t), -3f * Mathf.Cos(3f * t));
                tangent.Normalize();
                Vector3 normal = Vector3.Cross(tangent, Vector3.forward).normalized;
                Vector3 binormal = Vector3.Cross(tangent, normal);
                float twist = t * 12f + strand * Mathf.PI * 2f / 3f;
                rope.SetPosition(point, center + (normal * Mathf.Cos(twist) + binormal * Mathf.Sin(twist)) * 0.055f);
            }
        }
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
            line.startWidth = size * 0.0035f;
            line.endWidth = size * 0.0035f;
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
            knots[loopIndex].transform.rotation = transform.rotation;
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
