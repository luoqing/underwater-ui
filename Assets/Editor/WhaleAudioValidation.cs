using System;
using System.Reflection;
using UnityEngine;

public static class WhaleAudioValidation
{
    private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

    public static void ValidateMotion()
    {
        foreach (float step in new[] { 1f / 30f, 0.1f })
        {
            GameObject probe = new("Whale habitat regression probe");
            try
            {
                probe.transform.position = new Vector3(20f, 20f, 100f);
                HeroWhaleDrift whale = probe.AddComponent<HeroWhaleDrift>();
                Call(whale, "Start");
                float furthest = 0f;
                float travelled = 0f;
                MethodInfo advance = typeof(HeroWhaleDrift).GetMethod("SwimStep", Private);
                for (int frame = 0; frame < Mathf.RoundToInt(600f / step); frame++)
                {
                    Vector3 before = probe.transform.position;
                    advance.Invoke(whale, new object[] { frame * step, step });
                    travelled += Vector3.Distance(before, probe.transform.position);
                    furthest = Mathf.Max(furthest, Vector3.ProjectOnPlane(probe.transform.position - whale.basePosition, Vector3.up).magnitude);
                }
                Require(furthest < 50f && travelled > 1500f, "Whale escaped its encounter area or stopped swimming.");
                Debug.Log($"UNDERWATER_WHALE_HABITAT_VALIDATED dt={step:F4}; 600s; radius={furthest:F2}m; travel={travelled:F1}m");
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }
    }

    public static void ValidateInPlayMode(OceanMissionDirector director)
    {
        bool voice = UnderwaterAudio.VoiceEnabled;
        bool background = UnderwaterAudio.BackgroundEnabled;
        bool hadVoice = PlayerPrefs.HasKey("Underwater.VoiceEnabled");
        bool hadBackground = PlayerPrefs.HasKey("Underwater.BackgroundEnabled");
        bool subtitles = Read<bool>(director, "showSubtitles");
        UnderwaterAudio audio = UnityEngine.Object.FindFirstObjectByType<UnderwaterAudio>();
        Require(audio != null, "World audio is missing.");
        AudioClip clip = Resources.Load<AudioClip>("Audio/Dialogues/adventure_intro");
        Require(clip != null, "Narration test needs the actual intro clip.");
        try
        {
            foreach (bool voiceOn in new[] { true, false })
            foreach (bool backgroundOn in new[] { true, false })
            {
                UnderwaterAudio.SetVoiceEnabled(voiceOn);
                UnderwaterAudio.SetBackgroundEnabled(backgroundOn);
                Require(Read<AudioSource>(director, "dialogueSource").mute == !voiceOn, "Voice setting is not applied immediately.");
                foreach (string channel in new[] { "ambienceSource", "movementSource", "whaleSource" })
                    Require(Read<AudioSource>(audio, channel).mute == !backgroundOn, "Background setting leaked into the wrong channel: " + channel);
                Require(PlayerPrefs.GetInt("Underwater.VoiceEnabled") == (voiceOn ? 1 : 0)
                    && PlayerPrefs.GetInt("Underwater.BackgroundEnabled") == (backgroundOn ? 1 : 0), "Audio choices were not saved independently.");
            }
            UnderwaterAudio.SetVoiceEnabled(true);
            Set(director, "pendingDialogueClip", clip);
            Set(director, "showSubtitles", true);
            Call(director, "ShowDialogue", "海龟悠悠", "跟着标记游", 30f, string.Empty);
            Set(director, "pendingDialogueClip", clip);
            UnderwaterAudio.SetVoiceEnabled(false);
            Require(Read<AudioClip>(director, "pendingDialogueClip") == null
                && !Read<AudioSource>(director, "dialogueSource").isPlaying && director.SubtitlesVisible,
                "Turning voice off leaves queued speech or hides subtitles.");
            Call(director, "ShowDialogue", "海龟悠悠", "跟着标记游", 30f, "adventure_intro");
            Require(Read<AudioClip>(director, "pendingDialogueClip") == null, "Muted narration queued itself for a later gesture.");
            UnderwaterAudio.SetVoiceEnabled(true);
            Require(Read<AudioClip>(director, "pendingDialogueClip") == null, "Enabling voice resurrects stale narration.");

            Call(director, "StartAdventure");
            int stage = Read<int>(director, "stage");
            Vector3 cameraPosition = Camera.main.transform.position;
            director.ToggleWhaleGuide();
            Require(director.IsFindingWhale && !director.IsMenuOpen && director.IsMissionActive
                && Read<int>(director, "stage") == stage && Camera.main.transform.position == cameraPosition,
                "Finding whale teleports the player or resets the mission.");
            HeroWhaleDrift whale = UnityEngine.Object.FindFirstObjectByType<HeroWhaleDrift>();
            object[] target = { Vector3.zero };
            Require((bool)Call(director, "TryGetCurrentTarget", target) && (Vector3)target[0] == whale.transform.position,
                "Whale marker does not track the actual animal.");
            Vector3 origin = whale.transform.position;
            try
            {
                whale.transform.position += Vector3.right * 6f;
                Call(director, "TryGetCurrentTarget", target);
                Require((Vector3)target[0] == whale.transform.position, "Whale marker stays at a stale spawn position.");
            }
            finally { whale.transform.position = origin; }
            director.ToggleWhaleGuide();
            Require(!director.IsFindingWhale && director.IsMissionActive && Read<int>(director, "stage") == stage,
                "Cancelling whale navigation loses the active mission.");
            director.ToggleWhaleGuide();
            Call(director, "ResetMission");
            Require(!director.IsFindingWhale, "Starting another mission leaves a stale whale objective.");
            Debug.Log("UNDERWATER_WHALE_AUDIO_VALIDATED live whale target; no teleport/task reset; independent audio channels; immediate mute; no stale speech; independent subtitles; saved choices");
        }
        finally
        {
            UnderwaterAudio.SetVoiceEnabled(voice);
            UnderwaterAudio.SetBackgroundEnabled(background);
            if (!hadVoice) PlayerPrefs.DeleteKey("Underwater.VoiceEnabled");
            if (!hadBackground) PlayerPrefs.DeleteKey("Underwater.BackgroundEnabled");
            PlayerPrefs.Save();
            Set(director, "showSubtitles", subtitles);
        }
    }

    private static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Private).Invoke(target, args);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
