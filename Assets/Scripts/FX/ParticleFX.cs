using UnityEngine;

/// <summary>
/// 粒子特效池化包装：OnSpawn 播放，releaseDelay 后自动回池
/// 挂载点：HitFX prefab 根（ParticleSystem）
/// </summary>
public class ParticleFX : MonoBehaviour, IPoolable
{
    [SerializeField] private float releaseDelay = 0.6f; // 大于粒子最长生命周期

    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    public void OnSpawn()
    {
        ps.Clear();
        ps.Play();
        CancelInvoke();
        Invoke(nameof(Release), releaseDelay);
    }

    public void OnDespawn()
    {
        CancelInvoke();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    private void Release()
    {
        ObjectPool.Release(gameObject);
    }
}
