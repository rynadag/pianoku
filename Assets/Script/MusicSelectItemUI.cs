using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MusicSelectItemUI : MonoBehaviour
{
    [Header("Text")]
    [SerializeField] private TMP_Text musicNameText;
    [SerializeField] private TMP_Text composerText;
    [SerializeField] private TMP_Text difficultyText;

    [Header("Buttons")]
    [SerializeField] private Button playTutorialButton;
    [SerializeField] private Button playMusicButton;

    private MusicData musicData;
    private Action<MusicData> onPlayTutorial;
    private Action<MusicData> onPlayMusic;

    private void Awake()
    {
        AnimatedMenuButton.Ensure(playTutorialButton);
        AnimatedMenuButton.Ensure(playMusicButton);
    }

    private void OnDestroy()
    {
        ClearButtonListeners();
    }

    public void Initialize(MusicData music, Action<MusicData> tutorialCallback, Action<MusicData> playCallback)
    {
        ClearButtonListeners();

        musicData = music;
        onPlayTutorial = tutorialCallback;
        onPlayMusic = playCallback;

        if (musicNameText != null)
        {
            musicNameText.text = musicData != null ? musicData.MusicName : "Missing Music";
        }

        if (composerText != null)
        {
            composerText.text = musicData != null ? musicData.ComposerName : "-";
        }

        if (difficultyText != null)
        {
            difficultyText.text = musicData != null ? musicData.DifficultyLabel : "-";
        }

        if (playTutorialButton != null)
        {
            playTutorialButton.interactable = musicData != null && musicData.HasTutorialVideo;
            playTutorialButton.onClick.AddListener(HandlePlayTutorialClicked);
        }

        if (playMusicButton != null)
        {
            playMusicButton.interactable = musicData != null;
            playMusicButton.onClick.AddListener(HandlePlayMusicClicked);
        }
    }

    private void HandlePlayTutorialClicked()
    {
        if (musicData != null)
        {
            onPlayTutorial?.Invoke(musicData);
        }
    }

    private void HandlePlayMusicClicked()
    {
        if (musicData != null)
        {
            onPlayMusic?.Invoke(musicData);
        }
    }

    private void ClearButtonListeners()
    {
        if (playTutorialButton != null)
        {
            playTutorialButton.onClick.RemoveListener(HandlePlayTutorialClicked);
        }

        if (playMusicButton != null)
        {
            playMusicButton.onClick.RemoveListener(HandlePlayMusicClicked);
        }
    }
}
