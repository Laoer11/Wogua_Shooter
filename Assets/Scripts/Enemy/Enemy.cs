using System;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 敌人：随世界速度左移、idle 动画、受击反馈、死亡演出、出屏回收
/// 怪物1（地面）：不挂浮动部件 → 身体呼吸缩放
/// 怪物2（空中）：挂 Head/Body → 相位差上下浮动，阴影独立贴地
/// </summary>
public class Enemy : MonoBehaviour, IPoolable
{
    [Header("基础")]
    [SerializeField] private int scoreValue = 100;
    [SerializeField] private float recycleX = -8.5f;
    [SerializeField] private float moveFactor = 1f;

    [Header("受击反馈")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.35f, 0.35f, 1f);

    [Header("死亡演出")]
    [SerializeField] private float hitKnockback = 0.3f;   // 受击击退距离（顺子弹方向）
    [SerializeField] private GameObject hitFXPrefab;     // 拖 HitFX prefab（死亡小粒子）

    [Header("怪物1 呼吸模式")]
    [SerializeField] private Transform breathBody; // 拖 Body，留空且无浮动部件则无 idle

    [Header("怪物2 浮动模式（留空则走呼吸模式）")]
    [SerializeField] private Transform[] floatParts; // 拖 Head、Body
    [SerializeField] private float floatAmp = 0.06f;
    [SerializeField] private float floatDuration = 1.4f;
    [SerializeField] private float floatPhaseStep = 0.3f; // 头身相位差

    [Header("空中怪影子投影（浮动模式专用，地面怪留空）")]
    [SerializeField] private Transform shadow;
    [SerializeField] private float shadowGroundY = -1.3f;  // 影子世界Y（贴草面）
    [SerializeField] private float heightMin = -0.9f;      // 与 spawner 的 Air Y Min 保持一致
    [SerializeField] private float heightMax = 0.9f;       // 与 spawner 的 Air Y Max 保持一致
    [SerializeField] private float shadowScaleLow = 1f;    // 最低时影子最大
    [SerializeField] private float shadowScaleHigh = 0.55f;// 最高时影子最小

    /// <summary>击杀计分事件，由 GameManager 订阅</summary>
    public static event Action<int> OnEnemyKilled;

    /// <summary>容活敌注册表，玉米爆炸遍历用（OnEnable 注册/OnDisable 注销）</summary>
    public static readonly List<Enemy> Alive = new();

    private SpriteRenderer[] renderers;
    private Vector3[] partInitPos;
    private bool dead;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        if (floatParts != null)
        {
            partInitPos = new Vector3[floatParts.Length];
            for (int i = 0; i < floatParts.Length; i++)
                partInitPos[i] = floatParts[i].localPosition;
        }
    }

    private void OnEnable() => Alive.Add(this);
    private void OnDisable() => Alive.Remove(this);

    private void Update()
    {
        if (dead) return;
        transform.position += Vector3.left * (GameManager.Instance.GameSpeed * moveFactor * Time.deltaTime);
        if (transform.position.x < recycleX)
            ObjectPool.Release(gameObject);

        if (shadow != null)
        {
            Vector3 sp = shadow.position;
            sp.x = transform.position.x;
            sp.y = shadowGroundY;
            shadow.position = sp;
            float t = Mathf.InverseLerp(heightMin, heightMax, transform.position.y);
            float s = Mathf.Lerp(shadowScaleLow, shadowScaleHigh, t);
            shadow.localScale = new Vector3(s, s, 1f);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (dead) return;

        Bullet bullet = other.GetComponent<Bullet>();
        if (bullet != null)
        {
            bullet.Hit();
            TakeHit(); // 玉米爆炸中被杀的敌人会被 dead 标记拦住，不会重复计分
        }
        else
        {
            PlayerController pc = other.GetComponentInParent<PlayerController>();
            if (pc != null) pc.Die();
        }
    }

    public void TakeHit()
    {
        if (dead) return;
        dead = true;
        OnEnemyKilled?.Invoke(scoreValue);

        // 死亡小粒子：池化取用，位置 = 敌人中心
        if (hitFXPrefab != null)
        {
            GameObject fx = ObjectPool.Get(hitFXPrefab);
            fx.transform.position = transform.position;
        }

        // 受击反馈：红闪两下 → 无缝衔接淡出（同一条补间链，避免颜色打架）
        foreach (SpriteRenderer sr in renderers)
        {
            sr.DOKill();
            sr.DOColor(hitFlashColor, 0.05f).SetLoops(2, LoopType.Yoyo)
                .OnComplete(() => sr.DOFade(0f, 0.15f));
        }

        // 击退：顺子弹冲力向右推开（与 DOScale 通道不同可并存）
        transform.DOKill();
        transform.DOMoveX(transform.position.x + hitKnockback, 0.15f).SetEase(Ease.OutQuad);

        // 死亡演出：缩放弹出消失 → 回池
        transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).SetDelay(0.1f)
            .OnComplete(() => ObjectPool.Release(gameObject));
    }

    public void OnSpawn()
    {
        dead = false;
        transform.localScale = Vector3.one;
        foreach (SpriteRenderer sr in renderers)
        {
            sr.DOKill();
            sr.color = Color.white;
        }
        StartIdle();
    }

    public void OnDespawn()
    {
        transform.DOKill();
        if (renderers != null)
            foreach (SpriteRenderer sr in renderers) sr.DOKill();
        if (floatParts != null)
            foreach (Transform p in floatParts) p.DOKill();
        if (breathBody != null) breathBody.DOKill();
    }

    private void StartIdle()
    {
        if (floatParts != null && floatParts.Length > 0)
        {
            for (int i = 0; i < floatParts.Length; i++)
            {
                Transform p = floatParts[i];
                p.localPosition = partInitPos[i];
                p.DOLocalMoveY(partInitPos[i].y + floatAmp, floatDuration)
                    .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                    .SetDelay(i * floatPhaseStep);
            }
        }
        else if (breathBody != null)
        {
            breathBody.DOKill();
            breathBody.DOScale(1.05f, 1.2f)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }
}