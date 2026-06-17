using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject musicSelectPanel;
    [SerializeField] private GameObject practicePanel;
    [SerializeField] private GameObject gameplayRoot;

    [Header("Main Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button practiceButton;
    [SerializeField] private Button quitButton;

    [Header("Practice")]
    [SerializeField] private Button practiceBackButton;

    [Header("Back Buttons")]
    [Tooltip("Isi semua button back yang dipakai di luar main menu. Boleh pakai button yang sama di beberapa panel.")]
    [SerializeField] private Button[] backButtons;
    [SerializeField] private bool hideBackButtonsOnMainMenu = true;

    [Header("References")]
    [SerializeField] private MusicSelectManager musicSelectManager;
    [SerializeField] private PianoController pianoController;

    [Header("Behaviour")]
    [SerializeField] private bool showMainMenuOnStart = true;
    [SerializeField] private bool disableMainButtonsOutsideMainMenu = true;
    [SerializeField] private bool hideGameplayRootOnMainMenu = true;

    private void Awake()
    {
        if (mainMenuPanel == null)
        {
            mainMenuPanel = gameObject;
        }

        WireButtons();
    }

    private void Start()
    {
        if (showMainMenuOnStart)
        {
            ShowMainMenu();
        }
    }

    private void OnDestroy()
    {
        UnwireButtons();
    }

    public void ShowMainMenu()
    {
        SetPanelActive(mainMenuPanel, true);
        SetPanelActive(musicSelectPanel, false);
        SetPanelActive(practicePanel, false);
        if (hideGameplayRootOnMainMenu)
        {
            SetPanelActive(gameplayRoot, false);
        }

        if (musicSelectManager != null)
        {
            musicSelectManager.HideScorePanel();
            musicSelectManager.Hide();
        }

        SetMainButtonsInteractable(true);
        SetBackButtonsOutsideMainMenu(false);
    }

    public void ShowMusicSelect()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(practicePanel, false);
        SetPanelActive(musicSelectPanel, true);
        if (hideGameplayRootOnMainMenu)
        {
            SetPanelActive(gameplayRoot, false);
        }
        SetMainButtonsInteractable(false);
        SetBackButtonsOutsideMainMenu(true);

        if (musicSelectManager != null)
        {
            musicSelectManager.HideScorePanel();
            musicSelectManager.Show(this);
        }
    }

    public void ShowPractice()
    {
        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(musicSelectPanel, false);
        SetPanelActive(practicePanel, true);
        SetMainButtonsInteractable(false);
        SetBackButtonsOutsideMainMenu(true);

        if (musicSelectManager != null)
        {
            musicSelectManager.HideScorePanel();
            musicSelectManager.Hide();
        }

        if (gameplayRoot != null)
        {
            gameplayRoot.SetActive(true);
        }

        if (pianoController != null)
        {
            pianoController.StartPracticePhase();
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void WireButtons()
    {
        AnimatedMenuButton.Ensure(playButton);
        AnimatedMenuButton.Ensure(practiceButton);
        AnimatedMenuButton.Ensure(quitButton);
        AnimatedMenuButton.Ensure(practiceBackButton);
        EnsureBackButtonAnimations();

        if (playButton != null)
        {
            playButton.onClick.AddListener(ShowMusicSelect);
        }

        if (practiceButton != null)
        {
            practiceButton.onClick.AddListener(ShowPractice);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(QuitGame);
        }

        if (practiceBackButton != null)
        {
            WireBackButton(practiceBackButton);
        }

        if (backButtons != null)
        {
            for (int i = 0; i < backButtons.Length; i++)
            {
                WireBackButton(backButtons[i]);
            }
        }
    }

    private void UnwireButtons()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveListener(ShowMusicSelect);
        }

        if (practiceButton != null)
        {
            practiceButton.onClick.RemoveListener(ShowPractice);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(QuitGame);
        }

        if (practiceBackButton != null)
        {
            practiceBackButton.onClick.RemoveListener(ShowMainMenu);
        }

        if (backButtons != null)
        {
            for (int i = 0; i < backButtons.Length; i++)
            {
                if (backButtons[i] != null)
                {
                    backButtons[i].onClick.RemoveListener(ShowMainMenu);
                }
            }
        }
    }

    private void SetMainButtonsInteractable(bool interactable)
    {
        if (!disableMainButtonsOutsideMainMenu && !interactable)
        {
            return;
        }

        SetButtonInteractable(playButton, interactable);
        SetButtonInteractable(practiceButton, interactable);
        SetButtonInteractable(quitButton, interactable);
    }

    private static void SetButtonInteractable(Button button, bool interactable)
    {
        if (button != null)
        {
            button.interactable = interactable;
        }
    }

    private void EnsureBackButtonAnimations()
    {
        if (backButtons == null)
        {
            return;
        }

        for (int i = 0; i < backButtons.Length; i++)
        {
            AnimatedMenuButton.Ensure(backButtons[i]);
        }
    }

    private void WireBackButton(Button button)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(ShowMainMenu);
        button.onClick.AddListener(ShowMainMenu);
    }

    private void SetBackButtonsOutsideMainMenu(bool outsideMainMenu)
    {
        SetBackButtonState(practiceBackButton, outsideMainMenu);

        if (backButtons == null)
        {
            return;
        }

        for (int i = 0; i < backButtons.Length; i++)
        {
            SetBackButtonState(backButtons[i], outsideMainMenu);
        }
    }

    private void SetBackButtonState(Button button, bool outsideMainMenu)
    {
        if (button == null)
        {
            return;
        }

        button.interactable = outsideMainMenu;
        if (hideBackButtonsOnMainMenu)
        {
            button.gameObject.SetActive(outsideMainMenu);
        }
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }
}
