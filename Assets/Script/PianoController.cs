using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(AudioSource))]
public class PianoController : MonoBehaviour
{
    private const int WhiteKeyCount = 14;
    private const int BlackKeyCount = 10;
    private const int TotalKeyCount = WhiteKeyCount + BlackKeyCount;
    private static readonly int UrpBaseColorProperty = Shader.PropertyToID("_BaseColor");

    private static readonly string[] WhiteKeyLabels =
    {
        "C4", "D4", "E4", "F4", "G4", "A4", "B4",
        "C5", "D5", "E5", "F5", "G5", "A5", "B5"
    };

    private static readonly string[] BlackKeyLabels =
    {
        "C#4", "D#4", "F#4", "G#4", "A#4",
        "C#5", "D#5", "F#5", "G#5", "A#5"
    };

    private static readonly string[] WhiteAudioClipNames =
    {
        "C", "D", "E", "F", "G", "A", "B",
        "C1", "D1", "E1", "F1", "G1", "A1", "B1"
    };

    private static readonly string[] BlackAudioClipNames =
    {
        "C#", "D#", "F#", "G#", "A#",
        "C#1", "D#1", "F#1", "G#1", "A#1"
    };

#if ENABLE_INPUT_SYSTEM
    private static readonly Key[] WhiteKeyboardKeys =
    {
        Key.Q, Key.W, Key.E, Key.R, Key.T, Key.Y, Key.U,
        Key.Z, Key.X, Key.C, Key.V, Key.B, Key.N, Key.M
    };

    private static readonly Key[] BlackKeyboardKeys =
    {
        Key.Digit2, Key.Digit3, Key.Digit5, Key.Digit6, Key.Digit7,
        Key.S, Key.D, Key.G, Key.H, Key.J
    };
#else
    private static readonly KeyCode[] WhiteKeyboardKeys =
    {
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T, KeyCode.Y, KeyCode.U,
        KeyCode.Z, KeyCode.X, KeyCode.C, KeyCode.V, KeyCode.B, KeyCode.N, KeyCode.M
    };

    private static readonly KeyCode[] BlackKeyboardKeys =
    {
        KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7,
        KeyCode.S, KeyCode.D, KeyCode.G, KeyCode.H, KeyCode.J
    };
#endif

    public enum PianoPhase
    {
        Practice,
        Play
    }

    private enum NoteState
    {
        Pending,
        Holding,
        Perfect,
        Late,
        Missed
    }

    [Header("Phase")]
    [SerializeField] private PianoPhase startingPhase = PianoPhase.Practice;
    [FormerlySerializedAs("resetScoreWhenPlayStarts")]
    [SerializeField] private bool resetAccuracyWhenPlayStarts = true;

    [Header("UI References")]
    [SerializeField] private RectTransform noteArea;
    [SerializeField] private RectTransform summonOffsetLine;
    [FormerlySerializedAs("hitOffsetLine")]
    [SerializeField] private RectTransform pressOffsetLine;
    [SerializeField] private RectTransform vanishOffsetLine;
    [SerializeField] private Button[] whiteKeyButtons = new Button[WhiteKeyCount];
    [SerializeField] private Button[] blackKeyButtons = new Button[BlackKeyCount];
    [SerializeField] private TMP_Text phaseText;
    [FormerlySerializedAs("scoreText")]
    [SerializeField] private TMP_Text accuracyText;
    [SerializeField] private TMP_Text hitText;
    [SerializeField] private TMP_Text missText;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Line Renderer")]
    [SerializeField] private Transform lineRendererRoot;
    [SerializeField] private LineRenderer linePrefab;
    [SerializeField] private Material lineMaterial;
    [Tooltip("Khusus URP Unlit textured material: buat material instance per line lalu ubah _BaseColor.")]
    [SerializeField] private bool tintUrpUnlitBaseColor = true;
    [Tooltip("Kosongkan untuk memakai Default. Isi kalau kamu mau line masuk ke Sorting Layer khusus.")]
    [SerializeField] private string lineRendererSortingLayerName;
    [Tooltip("Offset lokal dari Note Area. Pakai nilai kecil saja; urutan utama sebaiknya lewat Sorting Order/Canvas Order.")]
    [SerializeField] private float lineRendererZOffset = -0.01f;
    [Tooltip("Sorting Order line. Atur supaya line berada di atas UI biasa tapi di bawah UI keys.")]
    [SerializeField] private int lineRendererSortingOrder = 5;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] whiteKeyClips = new AudioClip[WhiteKeyCount];
    [SerializeField] private AudioClip[] blackKeyClips = new AudioClip[BlackKeyCount];
    [SerializeField, Range(0f, 1f)] private float keyVolume = 1f;
    [SerializeField] private bool playAudioOnKeyPress = true;
#if UNITY_EDITOR
    [SerializeField] private bool autoAssignAudioFromAssetsAudio = true;
#endif

    [Header("Timing")]
    [Tooltip("Dipakai kalau Summon Offset belum di-assign. Nilai dihitung dari bawah Note Area.")]
    [SerializeField] private float fallbackSummonOffsetY = 36f;
    [Tooltip("Dipakai kalau Press Offset belum di-assign. Nilai dihitung dari bawah Note Area.")]
    [SerializeField] private float fallbackPressOffsetY = 96f;
    [Tooltip("Dipakai kalau Vanish Offset belum di-assign. Nilai dihitung dari bawah Note Area.")]
    [SerializeField] private float fallbackVanishOffsetY = 520f;
    [SerializeField] private float perfectWindow = 28f;
    [SerializeField] private float lateWindow = 76f;

    [Header("Keyboard")]
    [SerializeField] private bool enableKeyboardInput = true;

    [Header("Practice Visual")]
    [SerializeField] private float practiceLineHeight = 170f;
    [SerializeField] private float practiceLineSpeed = 520f;
    [SerializeField] private Color practiceLineColor = new Color(0.18f, 0.72f, 1f, 0.9f);

    [Header("Play Visual")]
    [SerializeField] private bool useSongJsonForPlay = true;
    [SerializeField] private TextAsset playSongJson;
    [SerializeField] private bool loopSongJson;
    [SerializeField] private float songStartDelay = 0.5f;
#if UNITY_EDITOR
    [SerializeField] private bool autoAssignJingleBellsJson = true;
#endif
    [SerializeField] private bool randomizePlayPattern = true;
    [SerializeField] private int[] playKeyPattern = { 0, 2, 4, 7, 9, 11, 14, 16, 19, 21 };
    [SerializeField] private float playSpawnInterval = 0.85f;
    [Tooltip("Tinggi note tap biasa. Untuk hold note, tinggi line dihitung dari durasi hold x kecepatan turun.")]
    [SerializeField] private float playLineHeight = 180f;
    [SerializeField] private float noteTravelTimeToOffset = 2.35f;
    [SerializeField] private float lineWidthMultiplier = 0.72f;
    [Tooltip("Kalau aktif, Play Mode akan membuat hold note acak berdasarkan chance dan range di bawah.")]
    [SerializeField] private bool randomizeHoldDurations;
    [SerializeField, Range(0f, 1f)] private float randomHoldChance = 0.3f;
    [Tooltip("Durasi hold acak dalam detik.")]
    [SerializeField] private Vector2 randomHoldDurationRange = new Vector2(0.65f, 1.6f);
    [Tooltip("Durasi hold per note pattern dalam detik. 0 = tap, lebih dari 0 = harus di-hold selama durasi itu.")]
    [SerializeField] private float[] playHoldDurations = { 0f, 0.8f, 0f, 1.2f, 0f, 0f, 1f, 0f, 0.6f, 0f };
    [SerializeField] private Color pendingColor = new Color(0.14f, 0.66f, 1f, 0.92f);
    [SerializeField] private Color perfectColor = new Color(0.18f, 0.92f, 0.38f, 0.96f);
    [SerializeField] private Color lateColor = new Color(1f, 0.82f, 0.18f, 0.96f);
    [SerializeField] private Color missColor = new Color(1f, 0.2f, 0.18f, 0.96f);

    [Header("Accuracy")]
    [SerializeField] private bool wrongKeyBreaksCombo = true;

    public PianoPhase CurrentPhase
    {
        get { return currentPhase; }
    }

    public float AccuracyPercent
    {
        get { return GetAccuracyPercent(); }
    }

    public int HitCount
    {
        get { return GetHitCount(); }
    }

    public int Combo
    {
        get { return combo; }
    }

    public int BestCombo
    {
        get { return bestCombo; }
    }

    public int Misses
    {
        get { return misses; }
    }

    private readonly PianoKey[] keyLookup = new PianoKey[TotalKeyCount];
    private readonly List<PianoKey> keys = new List<PianoKey>(TotalKeyCount);
    private readonly List<ButtonBinding> buttonBindings = new List<ButtonBinding>(TotalKeyCount);
    private readonly VisualLine[] activePracticeLines = new VisualLine[TotalKeyCount];
    private readonly List<VisualLine> practiceLines = new List<VisualLine>();
    private readonly List<VisualLine> playLines = new List<VisualLine>();

    private PianoPhase currentPhase;
    private float spawnTimer;
    private SongChart loadedSong;
    private float songTime;
    private int nextSongNoteIndex;
    private bool songEnded;
    private int patternIndex;
    private int combo;
    private int bestCombo;
    private int perfectHits;
    private int lateHits;
    private int misses;

    private sealed class PianoKey
    {
        public int Id;
        public int Index;
        public bool IsBlack;
        public string Label;
        public Button Button;
        public Image Image;
        public RectTransform RectTransform;
        public Color BaseColor;
    }

    private sealed class ButtonBinding
    {
        public PianoKeyPointerHandler Handler;
    }

    private sealed class VisualLine
    {
        public int KeyId;
        public GameObject GameObject;
        public LineRenderer LineRenderer;
        public bool OwnsMaterialInstances;
        public Vector2 AnchoredPosition;
        public float Height;
        public float Width;
        public NoteState State;
        public float HoldDuration;
        public bool StartedLate;
        public bool IsPracticeGrowing;

        public bool IsHold
        {
            get { return HoldDuration > 0.01f; }
        }
    }

    [Serializable]
    private sealed class SongChart
    {
        public string title;
        public float bpm = 120f;
        public SongNote[] notes;
    }

    [Serializable]
    private sealed class SongNote
    {
        public string key;
        public int keyId = -1;
        public float beat;
        public float holdBeats;
        public float holdSeconds;
    }

    private sealed class PianoKeyPointerHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private PianoController controller;
        private int keyId;
        private bool isPressed;

        public void Configure(PianoController owner, int pianoKeyId)
        {
            controller = owner;
            keyId = pianoKeyId;
            isPressed = false;
        }

        public void Clear(PianoController owner)
        {
            if (controller != owner)
            {
                return;
            }

            controller = null;
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (controller == null || isPressed)
            {
                return;
            }

            isPressed = true;
            controller.HandleKeyDown(keyId);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ReleaseKey();
        }

        private void OnDisable()
        {
            ReleaseKey();
        }

        private void ReleaseKey()
        {
            if (controller == null || !isPressed)
            {
                return;
            }

            isPressed = false;
            controller.HandleKeyUp(keyId);
        }
    }

    private void OnValidate()
    {
        ResizeArrays();
#if UNITY_EDITOR
        AutoAssignAudioClipsFromFolder();
        AutoAssignSongJsonFromFolder();
#endif
    }

    private void OnEnable()
    {
        ResizeArrays();
        EnsureAudioSource();
        WireButtons();
        SetPhase(startingPhase, true);
    }

    private void OnDisable()
    {
        UnwireButtons();
        ClearLines();
    }

    private void Update()
    {
        ProcessKeyboardInput();
        MovePracticeLines();

        if (currentPhase != PianoPhase.Play)
        {
            return;
        }

        UpdatePlaySpawner();
        MovePlayLines();
    }

    public void StartPracticePhase()
    {
        SetPhase(PianoPhase.Practice, false);
    }

    public void StartPlayPhase()
    {
        SetPhase(PianoPhase.Play, false);
    }

    public void StartPlayPhase(TextAsset songJson)
    {
        SetPlaySongJson(songJson);
        SetPhase(PianoPhase.Play, false);
    }

    public void StopPlaybackAndClearLines()
    {
        currentPhase = PianoPhase.Practice;
        spawnTimer = 0f;
        songTime = 0f;
        nextSongNoteIndex = 0;
        songEnded = true;
        patternIndex = 0;
        loadedSong = null;
        ClearLines();
        UpdateHud("Stopped");
    }

    public void SetPlaySongJson(TextAsset songJson)
    {
        if (songJson == null)
        {
            return;
        }

        playSongJson = songJson;
        useSongJsonForPlay = true;

        if (currentPhase == PianoPhase.Play)
        {
            PrepareSongPlayback();
        }
    }

    public void ResetAccuracy()
    {
        combo = 0;
        bestCombo = 0;
        perfectHits = 0;
        lateHits = 0;
        misses = 0;
        UpdateHud("Accuracy reset");
    }

    public void ResetScore()
    {
        ResetAccuracy();
    }

    public void PressKey(int keyId)
    {
        TapKey(keyId);
    }

    public void PressWhiteKey(int whiteIndex)
    {
        TapKey(whiteIndex);
    }

    public void PressBlackKey(int blackIndex)
    {
        TapKey(WhiteKeyCount + blackIndex);
    }

    public void PressKeyDown(int keyId)
    {
        HandleKeyDown(keyId);
    }

    public void ReleaseKey(int keyId)
    {
        HandleKeyUp(keyId);
    }

    public void SpawnPlayLineForKey(int keyId)
    {
        SpawnPlayLineForKey(keyId, 0f);
    }

    public void SpawnPlayLineForKey(int keyId, float holdDuration)
    {
        PianoKey key = GetKey(keyId);
        if (key != null)
        {
            SpawnPlayLine(key, holdDuration);
        }
    }

    private void SetPhase(PianoPhase nextPhase, bool initialSetup)
    {
        currentPhase = nextPhase;
        ClearLines();

        if (currentPhase == PianoPhase.Play)
        {
            spawnTimer = 0.35f;
            patternIndex = 0;
            PrepareSongPlayback();

            if (resetAccuracyWhenPlayStarts || initialSetup)
            {
                ResetAccuracy();
            }

            UpdateHud(HasPlayableSong() ? "Play: " + loadedSong.title : "Play phase");
        }
        else
        {
            loadedSong = null;
            UpdateHud("Practice phase");
        }
    }

    private void WireButtons()
    {
        UnwireButtons();
        keys.Clear();

        for (int i = 0; i < keyLookup.Length; i++)
        {
            keyLookup[i] = null;
        }

        for (int i = 0; i < WhiteKeyCount; i++)
        {
            RegisterKey(i, i, false, WhiteKeyLabels[i], whiteKeyButtons[i]);
        }

        for (int i = 0; i < BlackKeyCount; i++)
        {
            RegisterKey(WhiteKeyCount + i, i, true, BlackKeyLabels[i], blackKeyButtons[i]);
        }
    }

    private void RegisterKey(int keyId, int index, bool isBlack, string label, Button button)
    {
        if (button == null)
        {
            return;
        }

        Image image = button.GetComponent<Image>();
        PianoKey key = new PianoKey
        {
            Id = keyId,
            Index = index,
            IsBlack = isBlack,
            Label = label,
            Button = button,
            Image = image,
            RectTransform = button.GetComponent<RectTransform>(),
            BaseColor = image != null ? image.color : Color.white
        };

        keys.Add(key);
        keyLookup[keyId] = key;

        PianoKeyPointerHandler handler = button.GetComponent<PianoKeyPointerHandler>();
        if (handler == null)
        {
            handler = button.gameObject.AddComponent<PianoKeyPointerHandler>();
        }

        handler.Configure(this, keyId);
        buttonBindings.Add(new ButtonBinding { Handler = handler });
    }

    private void UnwireButtons()
    {
        for (int i = 0; i < buttonBindings.Count; i++)
        {
            ButtonBinding binding = buttonBindings[i];
            if (binding.Handler != null)
            {
                binding.Handler.Clear(this);
            }
        }

        buttonBindings.Clear();
    }

    private void TapKey(int keyId)
    {
        HandleKeyDown(keyId);
        HandleKeyUp(keyId);
    }

    private void HandleKeyDown(int keyId)
    {
        PianoKey key = GetKey(keyId);
        if (key == null)
        {
            return;
        }

        PlayKeyAudio(key);
        StartCoroutine(FlashKey(key));

        if (currentPhase == PianoPhase.Practice)
        {
            SpawnPracticeLine(key);
            UpdateHud("Practice: " + key.Label);
            return;
        }

        TryStartPlayLine(key);
    }

    private void HandleKeyUp(int keyId)
    {
        PianoKey key = GetKey(keyId);
        if (key == null)
        {
            return;
        }

        if (currentPhase == PianoPhase.Practice)
        {
            ReleasePracticeLine(key);
            return;
        }

        if (currentPhase == PianoPhase.Play)
        {
            TryReleaseHoldLine(key);
        }
    }

    private void ProcessKeyboardInput()
    {
        if (!enableKeyboardInput)
        {
            return;
        }

        for (int i = 0; i < WhiteKeyboardKeys.Length; i++)
        {
            ProcessKeyboardKey(i, WhiteKeyboardKeys[i]);
        }

        for (int i = 0; i < BlackKeyboardKeys.Length; i++)
        {
            ProcessKeyboardKey(WhiteKeyCount + i, BlackKeyboardKeys[i]);
        }
    }

#if ENABLE_INPUT_SYSTEM
    private void ProcessKeyboardKey(int keyId, Key keyboardKey)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        KeyControl keyControl = keyboard[keyboardKey];
        if (keyControl == null)
        {
            return;
        }

        if (keyControl.wasPressedThisFrame)
        {
            HandleKeyDown(keyId);
        }

        if (keyControl.wasReleasedThisFrame)
        {
            HandleKeyUp(keyId);
        }
    }
#else
    private void ProcessKeyboardKey(int keyId, KeyCode keyboardKey)
    {
        if (Input.GetKeyDown(keyboardKey))
        {
            HandleKeyDown(keyId);
        }

        if (Input.GetKeyUp(keyboardKey))
        {
            HandleKeyUp(keyId);
        }
    }
#endif

    private void SpawnPracticeLine(PianoKey key)
    {
        if (noteArea == null)
        {
            Debug.LogWarning("PianoController needs Note Area assigned before spawning practice lines.", this);
            return;
        }

        if (activePracticeLines[key.Id] != null)
        {
            activePracticeLines[key.Id].IsPracticeGrowing = false;
            activePracticeLines[key.Id] = null;
        }

        VisualLine line = CreateLine(key, practiceLineColor, practiceLineHeight);
        line.IsPracticeGrowing = true;
        line.AnchoredPosition = new Vector2(GetLaneX(key), GetSummonOffsetY() + practiceLineHeight * 0.5f);
        UpdateLineRenderer(line);
        practiceLines.Add(line);
        activePracticeLines[key.Id] = line;
    }

    private void ReleasePracticeLine(PianoKey key)
    {
        VisualLine line = activePracticeLines[key.Id];
        if (line == null)
        {
            return;
        }

        line.IsPracticeGrowing = false;
        activePracticeLines[key.Id] = null;
    }

    private void UpdatePlaySpawner()
    {
        if (noteArea == null || keys.Count == 0)
        {
            return;
        }

        if (HasPlayableSong())
        {
            UpdateSongSpawner();
            return;
        }

        spawnTimer -= Time.deltaTime;
        while (spawnTimer <= 0f)
        {
            int patternSlot;
            PianoKey key = PickNextPlayKey(out patternSlot);
            SpawnPlayLine(key, PickHoldDuration(patternSlot));
            spawnTimer += Mathf.Max(0.08f, playSpawnInterval);
        }
    }

    private void PrepareSongPlayback()
    {
        loadedSong = null;
        songTime = -noteTravelTimeToOffset - Mathf.Max(0f, songStartDelay);
        nextSongNoteIndex = 0;
        songEnded = false;

        if (!useSongJsonForPlay || playSongJson == null)
        {
            return;
        }

        loadedSong = JsonUtility.FromJson<SongChart>(playSongJson.text);
        if (loadedSong == null || loadedSong.notes == null || loadedSong.notes.Length == 0)
        {
            Debug.LogWarning("Song JSON is empty or invalid: " + playSongJson.name, this);
            loadedSong = null;
            return;
        }

        if (loadedSong.bpm <= 0f)
        {
            loadedSong.bpm = 120f;
        }

        if (string.IsNullOrEmpty(loadedSong.title))
        {
            loadedSong.title = playSongJson.name;
        }

        Array.Sort(loadedSong.notes, CompareSongNotes);
    }

    private void UpdateSongSpawner()
    {
        songTime += Time.deltaTime;

        while (nextSongNoteIndex < loadedSong.notes.Length)
        {
            SongNote note = loadedSong.notes[nextSongNoteIndex];
            float noteHitTime = BeatsToSeconds(note.beat);
            if (songTime < noteHitTime - noteTravelTimeToOffset)
            {
                break;
            }

            nextSongNoteIndex++;
            SpawnSongNote(note);
        }

        if (nextSongNoteIndex < loadedSong.notes.Length || playLines.Count > 0)
        {
            return;
        }

        if (loopSongJson)
        {
            PrepareSongPlayback();
            return;
        }

        if (!songEnded)
        {
            songEnded = true;
            UpdateHud("Song complete");
        }
    }

    private void SpawnSongNote(SongNote note)
    {
        int keyId = ResolveSongKeyId(note);
        PianoKey key = GetKey(keyId);
        if (key == null)
        {
            Debug.LogWarning("Song note key is not mapped: " + note.key + " / " + note.keyId, this);
            return;
        }

        SpawnPlayLine(key, GetSongHoldDuration(note));
    }

    private int ResolveSongKeyId(SongNote note)
    {
        if (note.keyId >= 0 && note.keyId < TotalKeyCount)
        {
            return note.keyId;
        }

        if (string.IsNullOrEmpty(note.key))
        {
            return -1;
        }

        for (int i = 0; i < WhiteKeyLabels.Length; i++)
        {
            if (string.Equals(note.key, WhiteKeyLabels[i], StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        for (int i = 0; i < BlackKeyLabels.Length; i++)
        {
            if (string.Equals(note.key, BlackKeyLabels[i], StringComparison.OrdinalIgnoreCase))
            {
                return WhiteKeyCount + i;
            }
        }

        return -1;
    }

    private float GetSongHoldDuration(SongNote note)
    {
        if (note.holdSeconds > 0f)
        {
            return note.holdSeconds;
        }

        if (note.holdBeats > 0f)
        {
            return BeatsToSeconds(note.holdBeats);
        }

        return 0f;
    }

    private float BeatsToSeconds(float beats)
    {
        if (loadedSong == null || loadedSong.bpm <= 0f)
        {
            return beats * 0.5f;
        }

        return beats * 60f / loadedSong.bpm;
    }

    private bool HasPlayableSong()
    {
        return useSongJsonForPlay && loadedSong != null && loadedSong.notes != null && loadedSong.notes.Length > 0;
    }

    private static int CompareSongNotes(SongNote first, SongNote second)
    {
        if (first == null && second == null)
        {
            return 0;
        }

        if (first == null)
        {
            return 1;
        }

        if (second == null)
        {
            return -1;
        }

        return first.beat.CompareTo(second.beat);
    }

    private void SpawnPlayLine(PianoKey key, float holdDuration)
    {
        if (key == null || noteArea == null)
        {
            return;
        }

        holdDuration = Mathf.Max(0f, holdDuration);
        VisualLine line = CreateLine(key, pendingColor, GetPlayLineHeight(holdDuration));
        line.HoldDuration = holdDuration;
        line.AnchoredPosition = new Vector2(GetLaneX(key), GetVanishOffsetY() + line.Height * 0.5f);
        UpdateLineRenderer(line);
        playLines.Add(line);
    }

    private PianoKey PickNextPlayKey(out int patternSlot)
    {
        patternSlot = -1;
        if (keys.Count == 0)
        {
            return null;
        }

        if (randomizePlayPattern || playKeyPattern == null || playKeyPattern.Length == 0)
        {
            return keys[UnityEngine.Random.Range(0, keys.Count)];
        }

        int safety = 0;
        while (safety < playKeyPattern.Length)
        {
            patternSlot = patternIndex;
            int keyId = Mathf.Clamp(playKeyPattern[patternSlot], 0, TotalKeyCount - 1);
            patternIndex = (patternIndex + 1) % playKeyPattern.Length;

            PianoKey key = GetKey(keyId);
            if (key != null)
            {
                return key;
            }

            safety++;
        }

        return keys[UnityEngine.Random.Range(0, keys.Count)];
    }

    private float PickHoldDuration(int patternSlot)
    {
        if (randomizeHoldDurations)
        {
            if (UnityEngine.Random.value > randomHoldChance)
            {
                return 0f;
            }

            float min = Mathf.Min(randomHoldDurationRange.x, randomHoldDurationRange.y);
            float max = Mathf.Max(randomHoldDurationRange.x, randomHoldDurationRange.y);
            return UnityEngine.Random.Range(Mathf.Max(0f, min), Mathf.Max(0f, max));
        }

        if (playHoldDurations != null && patternSlot >= 0 && patternSlot < playHoldDurations.Length)
        {
            return Mathf.Max(0f, playHoldDurations[patternSlot]);
        }

        return 0f;
    }

    private float GetPlayLineHeight(float holdDuration)
    {
        if (holdDuration <= 0.01f)
        {
            return playLineHeight;
        }

        return Mathf.Max(12f, GetPlayLineSpeed() * holdDuration);
    }

    private VisualLine CreateLine(PianoKey key, Color color, float height)
    {
        LineRenderer lineRenderer;
        if (linePrefab != null)
        {
            lineRenderer = Instantiate(linePrefab, GetLineRendererParent());
            lineRenderer.gameObject.SetActive(true);
        }
        else
        {
            GameObject lineObject = new GameObject("Piano Line " + key.Label, typeof(LineRenderer));
            lineObject.transform.SetParent(GetLineRendererParent(), false);
            lineRenderer = lineObject.GetComponent<LineRenderer>();
        }

        float width = GetLaneWidth(key) * lineWidthMultiplier;
        bool ownsMaterialInstances = ConfigureLineRenderer(lineRenderer, color, width);

        VisualLine line = new VisualLine
        {
            KeyId = key.Id,
            GameObject = lineRenderer.gameObject,
            LineRenderer = lineRenderer,
            OwnsMaterialInstances = ownsMaterialInstances,
            Height = height,
            Width = width,
            State = NoteState.Pending
        };

        return line;
    }

    private Transform GetLineRendererParent()
    {
        if (lineRendererRoot != null)
        {
            return lineRendererRoot;
        }

        return noteArea != null ? noteArea : transform;
    }

    private bool ConfigureLineRenderer(LineRenderer lineRenderer, Color color, float width)
    {
        bool ownsMaterialInstances = false;
        float worldWidth = AnchoredWidthToWorldWidth(width);
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = worldWidth;
        lineRenderer.endWidth = worldWidth;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
        lineRenderer.numCapVertices = 0;
        lineRenderer.numCornerVertices = 0;
        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Stretch;
        if (!string.IsNullOrEmpty(lineRendererSortingLayerName))
        {
            lineRenderer.sortingLayerName = lineRendererSortingLayerName;
        }

        lineRenderer.sortingOrder = lineRendererSortingOrder;

        if (lineMaterial != null)
        {
            if (tintUrpUnlitBaseColor)
            {
                lineRenderer.sharedMaterial = new Material(lineMaterial);
                ownsMaterialInstances = true;
            }
            else
            {
                lineRenderer.sharedMaterial = lineMaterial;
            }
        }
        else if (lineRenderer.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                lineRenderer.sharedMaterial = new Material(shader);
                ownsMaterialInstances = true;
            }
        }
        else if (tintUrpUnlitBaseColor)
        {
            ownsMaterialInstances = CreateRuntimeMaterialInstances(lineRenderer);
        }

        ApplyLineColor(lineRenderer, color);
        return ownsMaterialInstances;
    }

    private void SetLineColor(VisualLine line, Color color)
    {
        if (line == null || line.LineRenderer == null)
        {
            return;
        }

        ApplyLineColor(line.LineRenderer, color);
    }

    private void ApplyLineColor(LineRenderer lineRenderer, Color color)
    {
        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.startColor = color;
        lineRenderer.endColor = color;

        if (tintUrpUnlitBaseColor)
        {
            ApplyUrpUnlitBaseColor(lineRenderer, color);
        }
    }

    private void ApplyUrpUnlitBaseColor(LineRenderer lineRenderer, Color color)
    {
        Material[] materials = lineRenderer.sharedMaterials;
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            SetUrpBaseColor(materials[i], color);
        }
    }

    private static void SetUrpBaseColor(Material material, Color color)
    {
        if (material != null && material.HasProperty(UrpBaseColorProperty))
        {
            material.SetColor(UrpBaseColorProperty, color);
        }
    }

    private bool CreateRuntimeMaterialInstances(LineRenderer lineRenderer)
    {
        Material[] sourceMaterials = lineRenderer.sharedMaterials;
        if (sourceMaterials == null || sourceMaterials.Length == 0)
        {
            return false;
        }

        Material[] materialInstances = new Material[sourceMaterials.Length];
        bool hasInstance = false;
        for (int i = 0; i < sourceMaterials.Length; i++)
        {
            if (sourceMaterials[i] == null)
            {
                continue;
            }

            materialInstances[i] = new Material(sourceMaterials[i]);
            hasInstance = true;
        }

        if (hasInstance)
        {
            lineRenderer.sharedMaterials = materialInstances;
        }

        return hasInstance;
    }

    private void UpdateLineRenderer(VisualLine line)
    {
        if (line == null || line.LineRenderer == null || noteArea == null)
        {
            return;
        }

        float halfHeight = line.Height * 0.5f;
        float rawBottomY = line.AnchoredPosition.y - halfHeight;
        float rawTopY = line.AnchoredPosition.y + halfHeight;
        float lowerClipY = Mathf.Min(GetSummonOffsetY(), GetVanishOffsetY());
        float upperClipY = Mathf.Max(GetSummonOffsetY(), GetVanishOffsetY());
        float visibleBottomY = Mathf.Max(rawBottomY, lowerClipY);
        float visibleTopY = Mathf.Min(rawTopY, upperClipY);

        bool isVisible = visibleTopY > visibleBottomY;
        line.LineRenderer.enabled = isVisible;
        if (!isVisible)
        {
            return;
        }

        Vector2 bottom = new Vector2(line.AnchoredPosition.x, visibleBottomY);
        Vector2 top = new Vector2(line.AnchoredPosition.x, visibleTopY);
        float worldWidth = AnchoredWidthToWorldWidth(line.Width);
        line.LineRenderer.startWidth = worldWidth;
        line.LineRenderer.endWidth = worldWidth;
        line.LineRenderer.SetPosition(0, AnchoredToNoteAreaWorld(bottom));
        line.LineRenderer.SetPosition(1, AnchoredToNoteAreaWorld(top));
    }

    private void TryStartPlayLine(PianoKey key)
    {
        VisualLine closestLine = null;
        float closestDelta = float.MaxValue;

        for (int i = 0; i < playLines.Count; i++)
        {
            VisualLine line = playLines[i];
            if (line.KeyId != key.Id || line.State != NoteState.Pending)
            {
                continue;
            }

            float delta = GetTimingDelta(line);
            if (delta > perfectWindow || delta < -lateWindow)
            {
                continue;
            }

            if (Mathf.Abs(delta) < Mathf.Abs(closestDelta))
            {
                closestLine = line;
                closestDelta = delta;
            }
        }

        if (closestLine == null)
        {
            HandleWrongOrEarlyKey(key);
            return;
        }

        if (Mathf.Abs(closestDelta) <= perfectWindow)
        {
            StartOrResolveLine(closestLine, key, false);
            return;
        }

        StartOrResolveLine(closestLine, key, true);
    }

    private void StartOrResolveLine(VisualLine line, PianoKey key, bool startedLate)
    {
        if (line.IsHold)
        {
            line.State = NoteState.Holding;
            line.StartedLate = startedLate;
            SetLineColor(line, startedLate ? lateColor : perfectColor);

            UpdateHud(startedLate ? "Hold late: " + key.Label : "Hold: " + key.Label);
            return;
        }

        ResolveScoredLine(line, startedLate ? NoteState.Late : NoteState.Perfect, key.Label, false);
    }

    private void TryReleaseHoldLine(PianoKey key)
    {
        VisualLine holdLine = null;
        float closestDelta = float.MaxValue;

        for (int i = 0; i < playLines.Count; i++)
        {
            VisualLine line = playLines[i];
            if (line.KeyId != key.Id || line.State != NoteState.Holding)
            {
                continue;
            }

            float delta = GetHoldEndDelta(line);
            if (Mathf.Abs(delta) < Mathf.Abs(closestDelta))
            {
                holdLine = line;
                closestDelta = delta;
            }
        }

        if (holdLine == null)
        {
            return;
        }

        if (closestDelta > perfectWindow)
        {
            ResolveMiss(holdLine, "Released early: " + key.Label);
            return;
        }

        if (closestDelta < -lateWindow)
        {
            ResolveMiss(holdLine, "Released too late: " + key.Label);
            return;
        }

        bool releasedLate = Mathf.Abs(closestDelta) > perfectWindow;
        bool isLate = holdLine.StartedLate || releasedLate;
        ResolveScoredLine(holdLine, isLate ? NoteState.Late : NoteState.Perfect, key.Label, true);
    }

    private void HandleWrongOrEarlyKey(PianoKey key)
    {
        for (int i = 0; i < playLines.Count; i++)
        {
            VisualLine line = playLines[i];
            if (line.KeyId == key.Id && line.State == NoteState.Pending && GetTimingDelta(line) > perfectWindow)
            {
                UpdateHud("Too early: " + key.Label);
                return;
            }
        }

        if (wrongKeyBreaksCombo)
        {
            combo = 0;
        }

        UpdateHud("Wrong key: " + key.Label);
    }

    private void MovePracticeLines()
    {
        if (noteArea == null)
        {
            return;
        }

        for (int i = practiceLines.Count - 1; i >= 0; i--)
        {
            VisualLine line = practiceLines[i];
            if (line.GameObject == null)
            {
                practiceLines.RemoveAt(i);
                continue;
            }

            if (line.IsPracticeGrowing)
            {
                line.Height += practiceLineSpeed * Time.deltaTime;
                line.AnchoredPosition = new Vector2(line.AnchoredPosition.x, GetSummonOffsetY() + line.Height * 0.5f);
            }
            else
            {
                line.AnchoredPosition += Vector2.up * (practiceLineSpeed * Time.deltaTime);
            }

            UpdateLineRenderer(line);

            float bottomY = GetTimingY(line);
            if (!line.IsPracticeGrowing && bottomY >= GetVanishOffsetY())
            {
                DestroyLine(line);
                practiceLines.RemoveAt(i);
            }
        }
    }

    private void MovePlayLines()
    {
        if (noteArea == null)
        {
            return;
        }

        float speed = GetPlayLineSpeed();
        for (int i = playLines.Count - 1; i >= 0; i--)
        {
            VisualLine line = playLines[i];
            if (line.GameObject == null)
            {
                playLines.RemoveAt(i);
                continue;
            }

            line.AnchoredPosition += Vector2.down * (speed * Time.deltaTime);
            UpdateLineRenderer(line);

            if (line.State == NoteState.Pending && GetTimingDelta(line) < -lateWindow)
            {
                ResolveMiss(line, "Miss");
            }
            else if (line.State == NoteState.Holding && GetHoldEndDelta(line) < -lateWindow)
            {
                ResolveMiss(line, "Hold missed");
            }

            if (GetLineTopY(line) <= GetSummonOffsetY())
            {
                DestroyLine(line);
                playLines.RemoveAt(i);
            }
        }
    }

    private void ResolveScoredLine(VisualLine line, NoteState state, string keyLabel, bool wasHold)
    {
        ResolveLine(line, state);
        combo++;
        bestCombo = Mathf.Max(bestCombo, combo);

        if (state == NoteState.Perfect)
        {
            perfectHits++;
            UpdateHud(wasHold ? "Hold perfect: " + keyLabel : "Perfect: " + keyLabel);
            return;
        }

        lateHits++;
        UpdateHud(wasHold ? "Hold late: " + keyLabel : "Late: " + keyLabel);
    }

    private void ResolveMiss(VisualLine line, string feedback)
    {
        ResolveLine(line, NoteState.Missed);
        combo = 0;
        misses++;
        UpdateHud(feedback);
    }

    private void ResolveLine(VisualLine line, NoteState state)
    {
        line.State = state;

        if (line.LineRenderer == null)
        {
            return;
        }

        if (state == NoteState.Perfect)
        {
            SetLineColor(line, perfectColor);
        }
        else if (state == NoteState.Late)
        {
            SetLineColor(line, lateColor);
        }
        else if (state == NoteState.Missed)
        {
            SetLineColor(line, missColor);
        }
    }

    private float GetPlayLineSpeed()
    {
        float travelDistance = Mathf.Abs(GetVanishOffsetY() - GetPressOffsetY());
        return Mathf.Max(40f, travelDistance / Mathf.Max(0.1f, noteTravelTimeToOffset));
    }

    private float GetSummonOffsetY()
    {
        return GetOffsetY(summonOffsetLine, fallbackSummonOffsetY);
    }

    private float GetPressOffsetY()
    {
        return GetOffsetY(pressOffsetLine, fallbackPressOffsetY);
    }

    private float GetVanishOffsetY()
    {
        return GetOffsetY(vanishOffsetLine, fallbackVanishOffsetY);
    }

    private float GetOffsetY(RectTransform offsetLine, float fallbackY)
    {
        if (noteArea == null || offsetLine == null)
        {
            return fallbackY;
        }

        Vector3 worldCenter = offsetLine.TransformPoint(offsetLine.rect.center);
        return WorldToNoteAreaAnchored(worldCenter, new Vector2(0.5f, 0f)).y;
    }

    private float GetTimingY(VisualLine line)
    {
        return line.AnchoredPosition.y - line.Height * 0.5f;
    }

    private float GetLineTopY(VisualLine line)
    {
        return line.AnchoredPosition.y + line.Height * 0.5f;
    }

    private float GetTimingDelta(VisualLine line)
    {
        return GetTimingY(line) - GetPressOffsetY();
    }

    private float GetHoldEndDelta(VisualLine line)
    {
        return GetLineTopY(line) - GetPressOffsetY();
    }

    private float GetLaneX(PianoKey key)
    {
        if (key == null || key.RectTransform == null || noteArea == null)
        {
            return 0f;
        }

        Vector3[] corners = new Vector3[4];
        key.RectTransform.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        return WorldToNoteAreaAnchored(center, new Vector2(0.5f, 0f)).x;
    }

    private float GetLaneWidth(PianoKey key)
    {
        if (key == null || key.RectTransform == null || noteArea == null)
        {
            return 36f;
        }

        Vector3[] corners = new Vector3[4];
        key.RectTransform.GetWorldCorners(corners);
        float left = WorldToNoteAreaAnchored(corners[0], new Vector2(0.5f, 0f)).x;
        float right = WorldToNoteAreaAnchored(corners[2], new Vector2(0.5f, 0f)).x;
        return Mathf.Max(24f, Mathf.Abs(right - left));
    }

    private Vector2 WorldToNoteAreaAnchored(Vector3 worldPosition, Vector2 anchor)
    {
        Vector2 localPosition = noteArea.InverseTransformPoint(worldPosition);
        Rect rect = noteArea.rect;
        return new Vector2(
            localPosition.x - (anchor.x - noteArea.pivot.x) * rect.width,
            localPosition.y - (anchor.y - noteArea.pivot.y) * rect.height
        );
    }

    private Vector3 AnchoredToNoteAreaWorld(Vector2 anchoredPosition)
    {
        Rect rect = noteArea.rect;
        Vector3 localPosition = new Vector3(
            anchoredPosition.x + (0.5f - noteArea.pivot.x) * rect.width,
            anchoredPosition.y + (0f - noteArea.pivot.y) * rect.height,
            lineRendererZOffset
        );

        return noteArea.TransformPoint(localPosition);
    }

    private float AnchoredWidthToWorldWidth(float anchoredWidth)
    {
        if (noteArea == null)
        {
            return anchoredWidth;
        }

        Vector3 left = AnchoredToNoteAreaWorld(Vector2.zero);
        Vector3 right = AnchoredToNoteAreaWorld(new Vector2(anchoredWidth, 0f));
        return Mathf.Max(0.001f, Vector3.Distance(left, right));
    }

    private PianoKey GetKey(int keyId)
    {
        if (keyId < 0 || keyId >= keyLookup.Length)
        {
            return null;
        }

        return keyLookup[keyId];
    }

    private void EnsureAudioSource()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
    }

    private void PlayKeyAudio(PianoKey key)
    {
        if (!playAudioOnKeyPress || key == null)
        {
            return;
        }

        AudioClip clip = GetKeyClip(key);
        if (clip == null)
        {
            Debug.LogWarning("No audio clip assigned for piano key " + key.Label + ".", this);
            return;
        }

        EnsureAudioSource();
        audioSource.PlayOneShot(clip, keyVolume);
    }

    private AudioClip GetKeyClip(PianoKey key)
    {
        if (key.IsBlack)
        {
            if (key.Index >= 0 && key.Index < blackKeyClips.Length)
            {
                return blackKeyClips[key.Index];
            }

            return null;
        }

        if (key.Index >= 0 && key.Index < whiteKeyClips.Length)
        {
            return whiteKeyClips[key.Index];
        }

        return null;
    }

    private IEnumerator FlashKey(PianoKey key)
    {
        if (key.Image == null)
        {
            yield break;
        }

        Color flashColor = key.IsBlack ? new Color(0.24f, 0.42f, 0.72f, 1f) : new Color(0.72f, 0.86f, 1f, 1f);
        key.Image.color = flashColor;
        yield return new WaitForSeconds(0.08f);

        if (key.Image != null)
        {
            key.Image.color = key.BaseColor;
        }
    }

    private void UpdateHud(string feedback)
    {
        if (phaseText != null)
        {
            phaseText.text = currentPhase == PianoPhase.Play ? "Play" : "Practice";
        }

        if (accuracyText != null)
        {
            accuracyText.text = FormatAccuracyText();
        }

        if (hitText != null)
        {
            hitText.text = GetHitCount().ToString();
        }

        if (missText != null)
        {
            missText.text = misses.ToString();
        }

        if (comboText != null)
        {
            comboText.text = combo.ToString();
        }

        if (bestText != null)
        {
            bestText.text = bestCombo.ToString();
        }

        if (feedbackText != null)
        {
            feedbackText.text = feedback;
            feedbackText.color = GetFeedbackColor(feedback);
        }
    }

    private string FormatAccuracyText()
    {
        return GetAccuracyPercent().ToString("0.#") + "%";
    }

    private float GetAccuracyPercent()
    {
        int judgedCount = GetJudgedCount();
        if (judgedCount <= 0)
        {
            return 0f;
        }

        return GetHitCount() * 100f / judgedCount;
    }

    private int GetHitCount()
    {
        return perfectHits + lateHits;
    }

    private int GetJudgedCount()
    {
        return perfectHits + lateHits + misses;
    }

    private Color GetFeedbackColor(string feedback)
    {
        if (feedback.StartsWith("Perfect"))
        {
            return perfectColor;
        }

        if (feedback.StartsWith("Late"))
        {
            return lateColor;
        }

        if (feedback.StartsWith("Miss") || feedback.StartsWith("Wrong"))
        {
            return missColor;
        }

        return Color.white;
    }

    private void ClearLines()
    {
        for (int i = 0; i < activePracticeLines.Length; i++)
        {
            activePracticeLines[i] = null;
        }

        for (int i = practiceLines.Count - 1; i >= 0; i--)
        {
            DestroyLine(practiceLines[i]);
        }

        for (int i = playLines.Count - 1; i >= 0; i--)
        {
            DestroyLine(playLines[i]);
        }

        practiceLines.Clear();
        playLines.Clear();
    }

    private void DestroyLine(VisualLine line)
    {
        if (line == null || line.GameObject == null)
        {
            return;
        }

        DestroyLineMaterialInstances(line);

        if (Application.isPlaying)
        {
            Destroy(line.GameObject);
        }
        else
        {
            DestroyImmediate(line.GameObject);
        }
    }

    private void DestroyLineMaterialInstances(VisualLine line)
    {
        if (line == null || !line.OwnsMaterialInstances || line.LineRenderer == null)
        {
            return;
        }

        Material[] materials = line.LineRenderer.sharedMaterials;
        if (materials == null)
        {
            return;
        }

        for (int i = 0; i < materials.Length; i++)
        {
            if (materials[i] == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(materials[i]);
            }
            else
            {
                DestroyImmediate(materials[i]);
            }
        }

        line.LineRenderer.sharedMaterials = Array.Empty<Material>();
    }

    private void ResizeArrays()
    {
        if (whiteKeyButtons == null || whiteKeyButtons.Length != WhiteKeyCount)
        {
            Array.Resize(ref whiteKeyButtons, WhiteKeyCount);
        }

        if (blackKeyButtons == null || blackKeyButtons.Length != BlackKeyCount)
        {
            Array.Resize(ref blackKeyButtons, BlackKeyCount);
        }

        if (whiteKeyClips == null || whiteKeyClips.Length != WhiteKeyCount)
        {
            Array.Resize(ref whiteKeyClips, WhiteKeyCount);
        }

        if (blackKeyClips == null || blackKeyClips.Length != BlackKeyCount)
        {
            Array.Resize(ref blackKeyClips, BlackKeyCount);
        }
    }

#if UNITY_EDITOR
    private void AutoAssignAudioClipsFromFolder()
    {
        if (!autoAssignAudioFromAssetsAudio)
        {
            return;
        }

        string[] clipGuids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" });
        if (clipGuids.Length == 0)
        {
            return;
        }

        Dictionary<string, AudioClip> clipsByName = new Dictionary<string, AudioClip>();
        for (int i = 0; i < clipGuids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(clipGuids[i]);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip != null)
            {
                clipsByName[clip.name] = clip;
            }
        }

        AssignNamedClips(whiteKeyClips, WhiteAudioClipNames, clipsByName);
        AssignNamedClips(blackKeyClips, BlackAudioClipNames, clipsByName);
    }

    private void AssignNamedClips(AudioClip[] target, string[] clipNames, Dictionary<string, AudioClip> clipsByName)
    {
        int count = Mathf.Min(target.Length, clipNames.Length);
        for (int i = 0; i < count; i++)
        {
            AudioClip clip;
            if (clipsByName.TryGetValue(clipNames[i], out clip))
            {
                target[i] = clip;
            }
        }
    }

    private void AutoAssignSongJsonFromFolder()
    {
        if (!autoAssignJingleBellsJson || playSongJson != null)
        {
            return;
        }

        playSongJson = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Songs/JingleBellsSimple.json");
    }
#endif
}
