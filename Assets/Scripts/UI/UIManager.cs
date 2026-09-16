using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

/// <summary>
/// UI 管理器：HUD 分数刷新 + 死亡结算面板淡入淡出 + 重开按钮
/// 只订阅 GameManager 事件，不主动轮询
/// 挂载点：GameCanvas 下空物体 UIManager
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private CanvasGroup gameOverPanel;
    [SerializeField] private Button restartButton;

    [Header("暂停面板")]
    [SerializeField] private CanvasGroup pausePanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button pauseRestartButton;

    [Header("动画参数")]
    [SerializeField] private float fadeDuration = 0.4f;

    private void Start()
    {
        GameManager.OnScoreChanged += UpdateScore;
        GameManager.OnStateChanged += HandleStateChanged;
        restartButton.onClick.AddListener(HandleRestartClicked);
        continueButton.onClick.AddListener(HandleContinueClicked);
        pauseRestartButton.onClick.AddListener(HandleRestartClicked);

        gameOverPanel.alpha = 0f;
        gameOverPanel.interactable = false;
        gameOverPanel.blocksRaycasts = false;
        gameOverPanel.gameObject.SetActive(false);

        pausePanel.alpha = 0f;
        pausePanel.interactable = false;
        pausePanel.blocksRaycasts = false;
        pausePanel.gameObject.SetActive(false);
        UpdateScore(GameManager.Instance.Score);
    }

    private void OnDestroy()
    {
        GameManager.OnScoreChanged -= UpdateScore;
        GameManager.OnStateChanged -= HandleStateChanged;
        if (restartButton != null)
            restartButton.onClick.RemoveListener(HandleRestartClicked);
        if (continueButton != null)
            continueButton.onClick.RemoveListener(HandleContinueClicked);
        if (pauseRestartButton != null)
            pauseRestartButton.onClick.RemoveListener(HandleRestartClicked);
    }

    private void UpdateScore(int score)
    {
        scoreText.text = score.ToString();
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.GameOver)
            ShowPanel();
        else if (state == GameManager.GameState.Paused)
            ShowPausePanel();
        else if (state == GameManager.GameState.Playing)
        {
            HidePanel();
            HidePausePanel();
        }
    }

    private void ShowPausePanel()
    {
        pausePanel.DOKill();
        pausePanel.gameObject.SetActive(true);
        pausePanel.interactable = true;
        pausePanel.blocksRaycasts = true;
        // 关键：SetUpdate(true) 走 unscaled time，timeScale=0 时动画照常播
        pausePanel.DOFade(1f, fadeDuration).SetUpdate(true);
    }

    private void HidePausePanel()
    {
        pausePanel.DOKill();
        pausePanel.interactable = false;
        pausePanel.blocksRaycasts = false;
        pausePanel.DOFade(0f, fadeDuration).SetUpdate(true)
            .OnComplete(() => pausePanel.gameObject.SetActive(false));
    }

    private void HandleContinueClicked()
    {
        GameManager.Instance.Resume();
    }

    private void ShowPanel()
    {
        finalScoreText.text = GameManager.Instance.Score.ToString();
        gameOverPanel.gameObject.SetActive(true);
        gameOverPanel.DOKill();
        gameOverPanel.interactable = true;
        gameOverPanel.blocksRaycasts = true;
        gameOverPanel.DOFade(1f, fadeDuration);
    }

    private void HidePanel()
    {
        gameOverPanel.DOKill();
        gameOverPanel.interactable = false;
        gameOverPanel.blocksRaycasts = false;
        gameOverPanel.DOFade(0f, fadeDuration)
            .OnComplete(() => gameOverPanel.gameObject.SetActive(false));
    }

    private void HandleRestartClicked()
    {
        GameManager.Instance.Restart();
    }
}
