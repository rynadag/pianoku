using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class MusicSelectManager : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button backButton;

    [Header("Music List")]
    [SerializeField] private Transform listRoot;
    [SerializeField] private MusicSelectItemUI musicItemPrefab;
    [SerializeField] private MusicData[] musicList;
    [SerializeField] private bool rebuildListOnShow = true;
    [SerializeField] private bool clearExistingListRootChildren = true;

    [Header("Video Tutorial")]
    [SerializeField] private GameObject videoRoot;
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private Button closeVideoButton;
    [SerializeField] private bool loopTutorialVideo;

    [Header("Play Music")]
    [SerializeField] private PianoController pianoController;
    [SerializeField] private GameObject gameplayRoot;
    [SerializeField] private bool hideSelectPanelWhenMusicStarts = true;

    [Header("Score Panel")]
    [SerializeField] private GameObject scorePanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button scoreBackButton;
    [SerializeField] private bool showScorePanelWhenMusicStarts = true;

    private readonly List<MusicSelectItemUI> spawnedItems = new List<MusicSelectItemUI>();
    private MainMenuManager mainMenuManager;

    public MusicData SelectedMusic { get; private set; }

    private void Awake()
    {
        if (panelRoot == null)
        {
            panelRoot = gameObject;
        }

        WireButtons();
        HideVideo();
        HideScorePanel();
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    public void Show(MainMenuManager owner)
    {
        mainMenuManager = owner;
        Show();
    }

    public void Show()
    {
        HideScorePanel();

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (rebuildListOnShow)
        {
            RebuildMusicList();
        }
    }

    public void Hide()
    {
        HideVideo();
        HideScorePanel();
        StopPianoPlayback();

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void RebuildMusicList()
    {
        if (listRoot == null || musicItemPrefab == null)
        {
            return;
        }

        ClearSpawnedItems();

        if (musicList == null)
        {
            return;
        }

        for (int i = 0; i < musicList.Length; i++)
        {
            if (musicList[i] == null)
            {
                continue;
            }

            MusicSelectItemUI item = Instantiate(musicItemPrefab, listRoot);
            item.Initialize(musicList[i], PlayTutorialVideo, PlayMusic);
            spawnedItems.Add(item);
        }
    }

    public void BackToMainMenu()
    {
        Hide();
        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(false);
        }

        if (mainMenuManager != null)
        {
            mainMenuManager.ShowMainMenu();
        }
    }

    public void PlayTutorialVideo(MusicData music)
    {
        if (music == null || videoPlayer == null || !music.HasTutorialVideo)
        {
            return;
        }

        SelectedMusic = music;

        if (videoRoot != null)
        {
            videoRoot.SetActive(true);
        }

        videoPlayer.Stop();
        videoPlayer.isLooping = loopTutorialVideo;

        if (music.TutorialVideoClip != null)
        {
            videoPlayer.source = VideoSource.VideoClip;
            videoPlayer.clip = music.TutorialVideoClip;
        }
        else
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = music.TutorialVideoUrl;
        }

        videoPlayer.Play();
    }

    public void HideVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (videoRoot != null)
        {
            videoRoot.SetActive(false);
        }
    }

    public void PlayMusic(MusicData music)
    {
        if (music == null)
        {
            return;
        }

        SelectedMusic = music;
        HideVideo();
        StopPianoPlayback();

        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(true);
        }

        if (showScorePanelWhenMusicStarts)
        {
            ShowScorePanel();
        }

        if (pianoController != null)
        {
            pianoController.StartPlayPhase(music.SongJson);
        }

        if (hideSelectPanelWhenMusicStarts && panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void RetrySelectedMusic()
    {
        if (SelectedMusic == null)
        {
            return;
        }

        HideVideo();
        StopPianoPlayback();

        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(true);
        }

        ShowScorePanel();

        if (pianoController != null)
        {
            pianoController.StartPlayPhase(SelectedMusic.SongJson);
        }

        if (hideSelectPanelWhenMusicStarts && panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void ShowScorePanel()
    {
        if (scorePanel != null)
        {
            scorePanel.SetActive(true);
        }

        if (retryButton != null)
        {
            retryButton.interactable = SelectedMusic != null;
        }

        if (scoreBackButton != null)
        {
            scoreBackButton.interactable = true;
        }
    }

    public void HideScorePanel()
    {
        if (scorePanel != null)
        {
            scorePanel.SetActive(false);
        }
    }

    private void StopPianoPlayback()
    {
        if (pianoController != null)
        {
            pianoController.StopPlaybackAndClearLines();
        }
    }

    private void WireButtons()
    {
        AnimatedMenuButton.Ensure(backButton);
        AnimatedMenuButton.Ensure(closeVideoButton);
        AnimatedMenuButton.Ensure(retryButton);
        AnimatedMenuButton.Ensure(scoreBackButton);

        if (backButton != null)
        {
            WireBackButton(backButton);
        }

        if (closeVideoButton != null)
        {
            closeVideoButton.onClick.AddListener(HideVideo);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetrySelectedMusic);
        }

        if (scoreBackButton != null)
        {
            WireBackButton(scoreBackButton);
        }
    }

    private void UnwireButtons()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(BackToMainMenu);
        }

        if (closeVideoButton != null)
        {
            closeVideoButton.onClick.RemoveListener(HideVideo);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(RetrySelectedMusic);
        }

        if (scoreBackButton != null)
        {
            scoreBackButton.onClick.RemoveListener(BackToMainMenu);
        }
    }

    private void WireBackButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(BackToMainMenu);
        button.onClick.AddListener(BackToMainMenu);
    }

    private void ClearSpawnedItems()
    {
        if (clearExistingListRootChildren && listRoot != null)
        {
            for (int i = listRoot.childCount - 1; i >= 0; i--)
            {
                Destroy(listRoot.GetChild(i).gameObject);
            }

            spawnedItems.Clear();
            return;
        }

        for (int i = spawnedItems.Count - 1; i >= 0; i--)
        {
            if (spawnedItems[i] != null)
            {
                Destroy(spawnedItems[i].gameObject);
            }
        }

        spawnedItems.Clear();
    }
}
