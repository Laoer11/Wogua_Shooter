using UnityEngine;

/// <summary>
/// 敌人生成器：间断性成组（1~3只）从右屏外生成，地面/空中混合
/// 挂载点：EnemyRoot
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private GameObject enemy1Prefab;
    [SerializeField] private GameObject enemy2Prefab;
    [SerializeField] private Transform enemyRoot;

    [Header("生成参数")]
    [SerializeField] private float groupSpacing = 1.2f;
    [SerializeField] private float spawnX = 7.5f;
    [SerializeField] private float groundY = -0.78f;     // 怪物1 脚底贴草：119px半高0.595 + 草顶-1.38，观感不对就微调
    [SerializeField] private float airYMin = -0.9f;      // 怪物2 悬空下限（≈平射子弹线，实测后校准）
    [SerializeField] private float airYMax = 0.9f;       // 怪物2 悬空上限（二段跳最高可射到处）
    [SerializeField] private int prewarmEach = 4;

    private float spawnTimer;

    private void Start()
    {
        GameManager.OnStateChanged += HandleStateChanged;
        if (GameManager.Instance.State == GameManager.GameState.Playing)
            Prewarm();
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
        {
            spawnTimer = 0f;
            Prewarm();
        }
    }

    private void Prewarm()
    {
        ObjectPool.Prewarm(enemy1Prefab, prewarmEach);
        ObjectPool.Prewarm(enemy2Prefab, prewarmEach);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameManager.GameState.Playing) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer < GameManager.Instance.CurrentSpawnInterval) return;
        spawnTimer = 0f;
        SpawnGroup();
    }

    private void SpawnGroup()
    {
        // 成组规模：90s 前 1~2 只，之后 2~3 只（上限来自 GameManager）
        int max = GameManager.Instance.MaxGroupSize;
        int count = Random.Range(max - 1, max + 1);
        for (int i = 0; i < count; i++)
        {
            bool air = Random.value < 0.4f;
            GameObject prefab = air ? enemy2Prefab : enemy1Prefab;
            GameObject go = ObjectPool.Get(prefab);
            go.transform.SetParent(enemyRoot, false);
            go.transform.position = new Vector3(
                spawnX + i * groupSpacing,
                air ? Random.Range(airYMin, airYMax) : groundY,
                0f);
        }
    }
}