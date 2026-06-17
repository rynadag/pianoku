using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "MusicData", menuName = "PianoKu/Music Data")]
public class MusicData : ScriptableObject
{
    public enum MusicDifficulty
    {
        Easy,
        Normal,
        Hard,
        Expert
    }

    [Header("Info")]
    [SerializeField] private string musicName = "New Music";
    [SerializeField] private string composerName = "Unknown";
    [SerializeField] private MusicDifficulty difficulty = MusicDifficulty.Easy;
    [Tooltip("Optional. Isi kalau ingin label difficulty custom, misalnya Easy 1 atau Hard 4.")]
    [SerializeField] private string customDifficultyLabel;

    [Header("Files")]
    [Tooltip("JSON chart yang akan dikirim ke PianoController saat Play Music.")]
    [SerializeField] private TextAsset songJson;
    [Tooltip("VideoClip tutorial yang akan diputar oleh VideoPlayer.")]
    [SerializeField] private VideoClip tutorialVideoClip;
    [Tooltip("Optional. Pakai ini kalau video tutorial berasal dari file path/URL, bukan VideoClip.")]
    [SerializeField] private string tutorialVideoUrl;

    public string MusicName
    {
        get { return string.IsNullOrEmpty(musicName) ? name : musicName; }
    }

    public string ComposerName
    {
        get { return string.IsNullOrEmpty(composerName) ? "Unknown" : composerName; }
    }

    public MusicDifficulty Difficulty
    {
        get { return difficulty; }
    }

    public string DifficultyLabel
    {
        get { return string.IsNullOrEmpty(customDifficultyLabel) ? difficulty.ToString() : customDifficultyLabel; }
    }

    public TextAsset SongJson
    {
        get { return songJson; }
    }

    public VideoClip TutorialVideoClip
    {
        get { return tutorialVideoClip; }
    }

    public string TutorialVideoUrl
    {
        get { return tutorialVideoUrl; }
    }

    public bool HasTutorialVideo
    {
        get { return tutorialVideoClip != null || !string.IsNullOrEmpty(tutorialVideoUrl); }
    }
}
