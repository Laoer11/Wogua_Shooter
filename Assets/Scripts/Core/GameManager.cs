using System;
using UnityEngine;

/// <summary>
/// 全局流程管理：状态机（Start/Playing/Paused/GameOver）、计分、
/// 全局速度（改难度曲线）、自累计 GameTime、重开清场
/// 挂载点：场景空物体 GameManager
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Start, Playing, Paused, GameOver }

    /// <summary>状态切换广播，各系统自行启停</summary>
    public static event Action<GameState> OnStateChanged;
    /// <summary>分数变化广播，UI 订阅刷新</summary>
    public static event Action<int> OnScoreChanged;

    [Header("难度曲线")]
    public const float BASE_SPEED = 3.0f;   // 初始世界速度
    public const float SPEED_ACCEL = 0.012f; // 每秒加多少
    public const float MAX_SPEED = 6.5f;    // 封顶
    public const float SPAWN_BASE = 2.2f;   // 敌人生成间隔初始
    public const float SPAWN_MIN = 0.9f;    // 敌人生成间隔最小
    public const float SPAWN_NARROW_TIME = 240f; // 间隔在此时长内线性收窄到最小
    public const float GROUP_UP_TIME = 90f;      // 此时刻后成组规模 1~2 → 2~3

    [Header("引用")]
    [SerializeField] private PlayerController player;

    public GameState State { get; private set; } = GameState.Start;

    /// <summary>世界速度：3.0 + 0.012×GameTime，封顶 6.5</summary>
    public float GameSpeed => Mathf.Min(MAX_SPEED, BASE_SPEED + SPEED_ACCEL * GameTime);
    /// <summary>难度进度 0→1（240s 满），气泡频率等系统自行映射</summary>
    public float Difficulty01 => Mathf.Clamp01(GameTime / SPAWN_NARROW_TIME);
    /// <summary>当前敌人生成间隔：2.2 → 0.9 线性收窄</summary>
    public float CurrentSpawnInterval => Mathf.Lerp(SPAWN_BASE, SPAWN_MIN, Difficulty01);
    /// <summary>成组规模上限：90s 前 1~2 只，之后 2~3 只</summary>
    public int MaxGroupSize => GameTime < GROUP_UP_TIME ? 2 : 3;

    public float GameTime { get; private set; }
    public int Score { get; private set; }

    /// <summary>暂停：Playing→Paused，timeScale=0；回放时 Resume()</summary>
    public void Pause()
    {
        if (State != GameState.Playing) return;
        Time.timeScale = 0f;
        SetState(GameState.Paused);
    }

    /// <summary>继续：Paused→Playing，timeScale 回 1</summary>
    public void Resume()
    {
        if (State != GameState.Paused) return;
        Time.timeScale = 1f;
        SetState(GameState.Playing);
    }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        Enemy.OnEnemyKilled += AddScore;
        if (player != null) player.OnDied += HandlePlayerDied;
    }

    private void OnDisable()
    {
        Enemy.OnEnemyKilled -= AddScore;
        if (player != null) player.OnDied -= HandlePlayerDied;
    }

    private void Start()
    {
        SetState(GameState.Playing);
    }

    private void Update()
    {
        if (State == GameState.Playing)
        {
            GameTime += Time.deltaTime;
        }
    }

    public void AddScore(int value)
    {
        if (State != GameState.Playing) return;
        Score += value;
        OnScoreChanged?.Invoke(Score);
    }

    private void HandlePlayerDied()
    {
        if (State != GameState.Playing) return;
        SetState(GameState.GameOver);
    }

    /// <summary>重开：清空所有池 → 归零 → 玩家复位 → 回 Playing（可从 Paused 直接重开）</summary>
    public void Restart()
    {
        Time.timeScale = 1f; // 暂停中点重开时先解冻，否则清场后全场冻住
        ObjectPool.ClearAll();
        Score = 0;
        GameTime = 0f;
        OnScoreChanged?.Invoke(Score);
        if (player != null) player.ResetPlayer();
        SetState(GameState.Playing);
    }

    private void SetState(GameState newState)
    {
        if (State == newState) return;
        State = newState;
        OnStateChanged?.Invoke(newState);
    }
}
