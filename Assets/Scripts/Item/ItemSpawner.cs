using UnityEngine;

/// <summary>
/// 气泡生成器：每6~9s一枚，两档高度（低单跳/高二连跳），机枪/玉米随机
/// 挂载点：场景空物体 ItemRoot
/// </summary>

public class ItemSpawner : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private Transform itemRoot;
    [SerializeField] private Sprite machineGunIcon; // 机枪
    [SerializeField] private Sprite cornIcon;       // 玉米

    [Header("生成参数")]
    [SerializeField] private float intervalMin = 6f;   // 难度0时下限（难度满→8）
    [SerializeField] private float intervalMax = 9f;   // 难度0时上限（难度满→11）
    [SerializeField] private float spawnX = 7.5f;
    [SerializeField] private float lowY = -0.28f;
    [SerializeField] private float highY = 0.82f;
    [SerializeField] private float highChance = 0.5f;
    [SerializeField] private int prewarmCount = 3;

    private float spawnTimer;

    /// <summary>难度上升后气泡略降频：6~9 → 8~11（随 Difficulty01 插值）</summary>
    private float EffectiveIntervalMin => Mathf.Lerp(intervalMin, intervalMin + 2f, GameManager.Instance.Difficulty01);
    private float EffectiveIntervalMax => Mathf.Lerp(intervalMax, intervalMax + 2f, GameManager.Instance.Difficulty01);

    private void Start()
    {
        GameManager.OnStateChanged += HandleStateChanged;
        if(GameManager.Instance.State == GameManager.GameState.Playing)
            Prewarm();
        spawnTimer = Random.Range(EffectiveIntervalMin, EffectiveIntervalMax);
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        if(state == GameManager.GameState.Playing)
        {
            spawnTimer = Random.Range(EffectiveIntervalMin, EffectiveIntervalMax);
            Prewarm();
        }
    }

    private void Prewarm()
    {
        ObjectPool.Prewarm(bubblePrefab, prewarmCount);
    }

    private void Update()
    {
        if(GameManager.Instance.State != GameManager.GameState.Playing) return;

        spawnTimer -= Time.deltaTime;
        if(spawnTimer > 0f) return;
        spawnTimer = Random.Range(EffectiveIntervalMin, EffectiveIntervalMax);
        Spawn();
    }

    private void Spawn () 
    {
        PlayerShooter.WeaponType type = Random.value < 0.5f 
        ? PlayerShooter.WeaponType.MachineGun
            : PlayerShooter.WeaponType.Corn;

        GameObject go = ObjectPool.Get(bubblePrefab);
        go.transform.SetParent(itemRoot, false );
        go.transform.position = new Vector3(
            spawnX,
            Random. value < highChance ? highY : lowY, 0f );
        go.GetComponent<ItemBubble>()?.Setup(
            type,
            type == PlayerShooter.WeaponType.MachineGun ? machineGunIcon : cornIcon);
    }

}
