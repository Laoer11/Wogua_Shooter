using UnityEngine;

/// <summary>
/// 子弹：直线右飞、三帧轮播、出屏自动回池
/// 豌豆/玉米共用（玉米加爆炸参数）
/// </summary>
public class Bullet : MonoBehaviour, IPoolable
{
    [Header("飞行参数")]
    [SerializeField] private float speed = 8f;
    [SerializeField] private float killX = 8.5f; // 超过此 x 坐标回池（覆盖宽屏）

    [Header("玉米爆炸")]
    [SerializeField] private bool isExplosive = false;
    [SerializeField] private float damageRadius = 1.6f;
    [SerializeField] private float explodeAfter = 4f;    // 飞够该时长自爆（兜底）
    [SerializeField] private GameObject explosionPrefab; // 拖 Explosion prefab

    [Header("三帧轮播")]
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float frameInterval = 0.08f;

    private SpriteRenderer sr;
    private float frameTimer;
    private int frameIndex;
    private float lifeTimer;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        if (isExplosive && lifeTimer >= explodeAfter)
        {
            Explode();
            return;
        }

        transform.position += Vector3.right * (speed * Time.deltaTime);

        frameTimer += Time.deltaTime;
        if (frameTimer >= frameInterval)
        {
            frameTimer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            sr.sprite = frames[frameIndex];
        }

        if (transform.position.x > killX)
            ObjectPool.Release(gameObject);
    }

    /// <summary>敌人碰撞入口：豌豆直接回池，玉米触发爆炸</summary>
    public void Hit()
    {
        if (isExplosive) Explode();
        else ObjectPool.Release(gameObject);
    }

    private void Explode()
    {
        Vector3 center = transform.position;

        // 先结算伤害并统计击杀数（倒序遍历，TakeHit 内部会 Release）
        int killed = 0;
        for (int i = Enemy.Alive.Count - 1; i >= 0; i--)
        {
            Enemy e = Enemy.Alive[i];
            if (e == null) continue;
            if (Vector2.Distance(center, e.transform.position) <= damageRadius)
            {
                e.TakeHit();
                killed++;
            }
        }

        // 震屏：普通爆炸轻震，团灭（≥2 只）加重
        if (CameraShake.Instance != null)
            CameraShake.Instance.Shake(killed >= 2 ? 0.28f : 0.2f, 0.3f);

        // 白色闪光圈扩散
        if (explosionPrefab != null)
        {
            GameObject fx = ObjectPool.Get(explosionPrefab);
            fx.transform.position = center;
            fx.GetComponent<ExplosionFX>()?.Setup(damageRadius);
        }

        ObjectPool.Release(gameObject);
    }

    public void OnSpawn()
    {
        // 动态右边界：屏幕右缘的世界 X。子弹只活到屏幕边，
        // 敌人只在进入玩家视野后才可能被击中（修复屏外击杀）
        Camera cam = Camera.main;
        if (cam != null)
            killX = cam.ViewportToWorldPoint(new Vector3(1f, 0f, 0f)).x;

        lifeTimer = 0f;
        frameTimer = 0f;
        frameIndex = 0;
        if (frames != null && frames.Length > 0)
            sr.sprite = frames[0];
    }

    public void OnDespawn() { }
}