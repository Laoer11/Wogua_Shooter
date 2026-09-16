using UnityEngine;
using DG.Tweening;

/// <summary>
/// 相机震屏：爆炸/团灭时调用 Shake(amp, dur)
/// 挂载点：Main Camera（相机永不移动，震后自动回原位）
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originPos;
    private Tween activeTween;

    private void Awake()
    {
        Instance = this;
        originPos = transform.position;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>震屏：amp 震幅（世界单位）、dur 时长；新震动会打断旧震动</summary>
    public void Shake(float amplitude = 0.2f, float duration = 0.3f)
    {
        if (activeTween != null && activeTween.IsActive()) activeTween.Kill();
        transform.position = originPos;
        activeTween = transform.DOShakePosition(duration, amplitude, 10, 90, false, true);
    }
}
