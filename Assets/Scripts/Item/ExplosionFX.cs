using UnityEngine;
using DG.Tweening;

/// <summary>
/// 爆炸白圈占位特效（换粒子+震屏）：放大+淡出后自动回池
/// 挂载点：Explosion prefab 根（白色 Circle SpriteRenderer）
/// </summary>
public class ExplosionFX : MonoBehaviour, IPoolable
{
    [SerializeField] private float duration = 0.25f;

    private SpriteRenderer sr;
    private float radius = 1.6f;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    /// <summary>由 Bullet.Explode() 调用，按爆炸半径适配特效尺寸</summary>
    public void Setup(float radius)
    {
        this.radius = radius;
    }

    public void OnSpawn()
    {
        sr.DOKill();
        transform.DOKill();
        Color c = Color.white; c.a = 1f; sr.color = c;

        transform.localScale = Vector3.one * (radius * 0.5f);
        transform.DOScale(radius * 2.2f, duration).SetEase(Ease.OutQuad);
        sr.DOFade(0f, duration).SetEase(Ease.InQuad)
            .OnComplete(() => ObjectPool.Release(gameObject));
    }

    public void OnDespawn()
    {
        sr.DOKill();
        transform.DOKill();
    }
}