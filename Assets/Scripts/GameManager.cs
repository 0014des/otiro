using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("UIパネル")]
    public GameObject startPanel;
    public GameObject gamePlayPanel;
    public GameObject pausePanel;
    public GameObject gameOverPanel;

    [Header("UI要素")]
    public UnityEngine.UI.Text scoreText;
    public UnityEngine.UI.Text gameOverText;
    public UnityEngine.UI.Text restartText;
    public UnityEngine.UI.Text highScoreText;
    public UnityEngine.UI.Text invincibleStatusText;

    [Header("スマホ用ボタンUI (ボタンモード)")]
    public GameObject mobileControls;
    public GameObject leftButton;
    public GameObject rightButton;

    [Header("スマホ用ジョイスティックUI (スティックモード)")]
    public GameObject joystickContainer;
    public VirtualJoystick virtualJoystick;

    [Header("スマホ用ドラッグUI (ドラッグモード)")]
    public GameObject dragInputArea;
    public DragInputPanel dragInputPanel;

    [Header("ガラスUI適用対象")]
    public Image startPanelBg;
    public Image pausePanelBg;
    public Image gameOverPanelBg;
    public Image[] glassButtons;
    public Image[] glassMobileButtons;

    [Header("参照")]
    public PlayerController player;
    public Spawner spawner;
    public AudioSource bgmSource;

    private float score = 0f;
    private bool isGameOver = false;
    private bool isGameStarted = false;
    private bool isPaused = false;
    private int highScore = 0;

    public enum MobileInputMode
    {
        None,
        Buttons,
        Joystick,
        Drag
    }
    private MobileInputMode mobileInputMode = MobileInputMode.None;

    void Start()
    {
        // ==========================================
        // 分身（プレイヤー重複）バグに対する防御処理
        // ==========================================
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (allPlayers.Length > 1)
        {
            Debug.LogWarning($"[GameManager] シーン内に重複したプレイヤーを {allPlayers.Length - 1} 個検出しました。古いプレイヤーを自動クリーンアップします。");
            // GameManagerが直接参照しているPlayerを本物とし、それ以外を削除
            foreach (var p in allPlayers)
            {
                if (p != player && p != null)
                {
                    DestroyImmediate(p.gameObject);
                }
            }
        }

        highScore = PlayerPrefs.GetInt("HighScore", 0);
        Time.timeScale = 0f;

        if (startPanel != null) startPanel.SetActive(true);
        if (gamePlayPanel != null) gamePlayPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (mobileControls != null) mobileControls.SetActive(false);
        if (joystickContainer != null) joystickContainer.SetActive(false);
        if (dragInputArea != null) dragInputArea.SetActive(false);

        UpdateInvincibleText();

        if (bgmSource != null) bgmSource.Stop();

        SetupMobileButtonEvents();
        ApplyGlassStyle();
    }

    private void ApplyGlassStyle()
    {
        // 超透明・リキッドガラススプライト
        Sprite panelDarkSprite = GlassUI.CreateGlassPanelSprite(new Color(0.04f, 0.04f, 0.08f), 0.25f, 40); // 0.6 -> 0.25へ透明化
        Sprite panelRedSprite = GlassUI.CreateGlassPanelSprite(new Color(0.4f, 0.05f, 0.05f), 0.22f, 40);

        Sprite buttonSprite = GlassUI.CreateGlassButtonSprite(new Color(1f, 1f, 1f), 0.06f, 26); // ボタンも極限まで透明化

        if (startPanelBg != null)
        {
            startPanelBg.sprite = panelDarkSprite;
            startPanelBg.type = Image.Type.Sliced;
            startPanelBg.color = Color.white;
        }
        if (pausePanelBg != null)
        {
            pausePanelBg.sprite = panelDarkSprite;
            pausePanelBg.type = Image.Type.Sliced;
            pausePanelBg.color = Color.white;
        }
        if (gameOverPanelBg != null)
        {
            gameOverPanelBg.sprite = panelRedSprite;
            gameOverPanelBg.type = Image.Type.Sliced;
            gameOverPanelBg.color = Color.white;
        }

        ColorBlock glassCB = GlassUI.CreateGlassButtonColors();
        if (glassButtons != null)
        {
            foreach (Image btnImg in glassButtons)
            {
                if (btnImg != null)
                {
                    btnImg.sprite = buttonSprite;
                    btnImg.type = Image.Type.Sliced;
                    btnImg.color = Color.white;
                    Button btn = btnImg.GetComponent<Button>();
                    if (btn != null) btn.colors = glassCB;
                }
            }
        }

        // スマホ用ボタン（すりガラス・サイバーネオン調）
        Sprite mobileBtnSprite = GlassUI.CreateGlassButtonSprite(new Color(0.4f, 0.7f, 1f), 0.08f, 32);
        if (glassMobileButtons != null)
        {
            foreach (Image btnImg in glassMobileButtons)
            {
                if (btnImg != null)
                {
                    btnImg.sprite = mobileBtnSprite;
                    btnImg.type = Image.Type.Sliced;
                    btnImg.color = Color.white;
                }
            }
        }
    }

    void Update()
    {
        if (!isGameStarted) return;

        if (!isGameOver)
        {
            if (!isPaused)
            {
                score += Time.deltaTime * 10f;
                if (scoreText != null)
                    scoreText.text = Mathf.FloorToInt(score).ToString(); // 数字のみでシンプルかつ洗練された表示へ

                // スマホ操作の入力処理
                if (mobileInputMode == MobileInputMode.Joystick && virtualJoystick != null && player != null)
                {
                    float joyX = virtualJoystick.GetInputAxis().x;
                    player.SetBtnInput(joyX);
                }
                else if (mobileInputMode == MobileInputMode.Drag && dragInputPanel != null && player != null)
                {
                    float dragDeltaX = dragInputPanel.GetDeltaX();
                    player.SetDragInput(dragDeltaX);
                }
            }

            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame))
            {
                TogglePause();
            }
        }
        else
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.rKey.wasPressedThisFrame)
            {
                RetryGame();
            }
        }
    }

    private void SetupMobileButtonEvents()
    {
        if (leftButton != null)
        {
            EventTrigger trigger = leftButton.GetComponent<EventTrigger>();
            if (trigger == null) trigger = leftButton.AddComponent<EventTrigger>();
            trigger.triggers.Clear(); // 重複登録の完全リセット
            
            EventTrigger.Entry downEntry = new EventTrigger.Entry();
            downEntry.eventID = EventTriggerType.PointerDown;
            downEntry.callback.AddListener((data) => { OnPointerDownLeft(); });
            trigger.triggers.Add(downEntry);

            EventTrigger.Entry upEntry = new EventTrigger.Entry();
            upEntry.eventID = EventTriggerType.PointerUp;
            upEntry.callback.AddListener((data) => { OnPointerUpLeft(); });
            trigger.triggers.Add(upEntry);
        }

        if (rightButton != null)
        {
            EventTrigger trigger = rightButton.GetComponent<EventTrigger>();
            if (trigger == null) trigger = rightButton.AddComponent<EventTrigger>();
            trigger.triggers.Clear(); // 重複登録の完全リセット
            
            EventTrigger.Entry downEntry = new EventTrigger.Entry();
            downEntry.eventID = EventTriggerType.PointerDown;
            downEntry.callback.AddListener((data) => { OnPointerDownRight(); });
            trigger.triggers.Add(downEntry);

            EventTrigger.Entry upEntry = new EventTrigger.Entry();
            upEntry.eventID = EventTriggerType.PointerUp;
            upEntry.callback.AddListener((data) => { OnPointerUpRight(); });
            trigger.triggers.Add(upEntry);
        }
    }

    public void SelectKeyboardMode()
    {
        mobileInputMode = MobileInputMode.None;
        if (mobileControls != null) mobileControls.SetActive(false);
        if (joystickContainer != null) joystickContainer.SetActive(false);
        if (dragInputArea != null) dragInputArea.SetActive(false);
        StartGame();
    }

    public void SelectMobileButtonsMode()
    {
        mobileInputMode = MobileInputMode.Buttons;
        if (mobileControls != null) mobileControls.SetActive(true);
        if (joystickContainer != null) joystickContainer.SetActive(false);
        if (dragInputArea != null) dragInputArea.SetActive(false);
        StartGame();
    }

    public void SelectMobileJoystickMode()
    {
        mobileInputMode = MobileInputMode.Joystick;
        if (mobileControls != null) mobileControls.SetActive(false);
        if (joystickContainer != null) joystickContainer.SetActive(true);
        if (dragInputArea != null) dragInputArea.SetActive(false);
        StartGame();
    }

    public void SelectMobileDragMode()
    {
        mobileInputMode = MobileInputMode.Drag;
        if (mobileControls != null) mobileControls.SetActive(false);
        if (joystickContainer != null) joystickContainer.SetActive(false);
        if (dragInputArea != null) dragInputArea.SetActive(true);
        StartGame();
    }

    // 後方互換性のためのエイリアス
    public void SelectMobileMode()
    {
        SelectMobileButtonsMode();
    }

    private void StartGame()
    {
        isGameStarted = true;
        Time.timeScale = 1f;

        if (startPanel != null) startPanel.SetActive(false);
        if (gamePlayPanel != null) gamePlayPanel.SetActive(true);

        if (bgmSource != null) bgmSource.Play();
    }

    public void TogglePause()
    {
        if (isGameOver || !isGameStarted) return;

        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            if (pausePanel != null) pausePanel.SetActive(true);
            if (bgmSource != null) bgmSource.Pause();
        }
        else
        {
            Time.timeScale = 1f;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (bgmSource != null) bgmSource.UnPause();
        }
    }

    public void ToggleInvincible()
    {
        if (player != null)
        {
            player.isInvincible = !player.isInvincible;
            UpdateInvincibleText();
        }
    }

    private void UpdateInvincibleText()
    {
        if (invincibleStatusText != null && player != null)
        {
            invincibleStatusText.text = player.isInvincible ? "無敵モード: ON" : "無敵モード: OFF";
            invincibleStatusText.color = player.isInvincible
                ? new Color(0.3f, 1f, 0.5f, 0.8f)
                : new Color(1f, 0.3f, 0.3f, 0.4f);
        }
    }

    public void GameOver()
    {
        if (isGameOver) return;

        isGameOver = true;
        Time.timeScale = 0f;

        if (player != null) player.SetDead();
        if (spawner != null) spawner.StopSpawning();
        if (bgmSource != null) bgmSource.Stop();

        int finalScore = Mathf.FloorToInt(score);
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        if (gamePlayPanel != null) gamePlayPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (gameOverText != null)
            gameOverText.text = "ゲームオーバー";

        if (restartText != null)
            restartText.text = "スコア: " + finalScore.ToString();

        if (highScoreText != null)
            highScoreText.text = "ハイスコア: " + highScore.ToString();
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnPointerDownLeft()
    {
        if (player != null && !player.IsDead()) player.SetBtnInput(-1f);
    }

    public void OnPointerUpLeft()
    {
        if (player != null) player.SetBtnInput(0f);
    }

    public void OnPointerDownRight()
    {
        if (player != null && !player.IsDead()) player.SetBtnInput(1f);
    }

    public void OnPointerUpRight()
    {
        if (player != null) player.SetBtnInput(0f);
    }
}
