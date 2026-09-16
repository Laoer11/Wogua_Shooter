using System;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 主角控制：纯代码积分跳跃（不用物理模拟）、二连跳、落地钳制、
/// idle 小蹦跳（仅影响显示不影响判定）、阴影起跳缩小/落地恢复
/// 挂载点：Player（X 永远固定 -2.8，只有 Body 的局部 Y 在动）
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("调参区：跳跃手感")]
    [SerializeField] private float jumpForce = 9f;        // 一段跳初速
    [SerializeField] private float doubleJumpForce = 8f;  // 二段跳初速
    [SerializeField] private float gravity = -30f;       // 重力，负值，偏快下落
    [SerializeField] private int maxJumpCount = 2;       // 最大连跳次数
    [SerializeField] private float jumpBufferTime = 0.12f; // 输入缓冲：落地前这么久内按的空格，落地瞬间自动起跳

    [Header("idle 小蹦跳")]
    [SerializeField] private float idleAmplitude = 0.025f; // 蹦跳幅度
    [SerializeField] private float idleFrequency = 1.2f;   // 每秒蹦几下

    [Header("引用")]
    [SerializeField] private Transform body;             // 拖 Body
    [SerializeField] private SpriteRenderer shadow;      // 拖 Shadow

    [Header("地面参数（调到脚底刚好贴草面）")]
    [SerializeField] private float groundY = -0.8f;

    [Header("死亡演出")]
    [SerializeField] private float deathBounce = 6.5f;  // 死亡弹起飞速
    [SerializeField] private float deathVx = -2.5f;     // 死亡向后水平速度
    [SerializeField] private float deathSpinDegrees = -540f; // 死亡翻滚总角度

    /// <summary>敌人碰撞触发，接 GameManager</summary>
    public event Action OnDied;

    private SpriteRenderer bodySR;
    private float vy;
    private int jumpCount;
    private bool grounded = true;
    private float idleTimer;
    private Vector3 shadowOriginalScale;
    private Vector3 bodyInitLocalPos;
    private bool isDead;
    private float deadVx;
    private float lastTapTime = -999f; // 最近一次跳跃按键时间（输入缓冲用）

    // 死亡变灰的目标色（提前构造，避免运行期反复 new）
    private static readonly Color k_grayColor = new(0.55f, 0.55f, 0.55f, 1f);

    private void Awake()
    {
        bodySR = body.GetComponent<SpriteRenderer>();
        if (shadow != null)
            shadowOriginalScale = shadow.transform.localScale;
        bodyInitLocalPos = body.localPosition;
    }

    private void Update()
    {
        // ESC / P 键暂停/继续（编辑器调试 + 键盘玩家用；只在 Playing/Paused 间切换）
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (GameManager.Instance.State == GameManager.GameState.Playing) GameManager.Instance.Pause();
            else if (GameManager.Instance.State == GameManager.GameState.Paused) GameManager.Instance.Resume();
        }

        HandleInput();
        ApplyPhysics();
    }

    private void HandleInput()
    {
        if (GameManager.Instance.State != GameManager.GameState.Playing) return;

        // 空格跳跃（编辑器/键盘）
        bool tapped = Input.GetKeyDown(KeyCode.Space);
        // 触屏点按通道保留（真机 APK 用）
        if (!tapped && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            // 防UI穿透：点在UI上的那次点击不触发跳跃
            if (EventSystem.current != null
                && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            tapped = true;
        }

        if (tapped)
        {
            lastTapTime = Time.time; // 按键即记录：起跳失败也保留，落地瞬间补偿（输入缓冲）
            TryJump();
        }
    }

    /// <summary>尝试起跳：跳跃次数没用完立即起跳，否则等落地由缓冲补偿</summary>
    private void TryJump()
    {
        if (jumpCount >= maxJumpCount) return;

        bool isDoubleJump = jumpCount > 0;
        vy = isDoubleJump ? doubleJumpForce : jumpForce;
        jumpCount++;
        grounded = false;
        idleTimer = 0f;
        TweenShadow(0.6f, 0.5f); // 升空：阴影缩小+变淡，制造空间感

        // 起跳 stretch：纵向拉长
        TweenBodyScale(0.9f, 1.15f);

        // 二段跳空翻 360°（生命感）
        if (isDoubleJump)
            body.DORotate(new Vector3(0f, 0f, -360f), 0.5f, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad);
    }

    private void ApplyPhysics()
    {
        // 死亡物理：抛物线弹飞（不受落地钳制，落出屏幕即演出完成）
        if (isDead)
        {
            vy += gravity * 0.7f * Time.deltaTime;
            Vector3 p = body.localPosition;
            p.y += vy * Time.deltaTime;
            p.x += deadVx * Time.deltaTime;
            body.localPosition = p;
            return;
        }

        if (!grounded)
        {
            vy += gravity * Time.deltaTime;
            Vector3 p = body.localPosition;
            p.y += vy * Time.deltaTime;

            // 落地钳制：直接夹回 groundY，快速下落也不穿透
            if (p.y <= groundY)
            {
                p.y = groundY;
                vy = 0f;
                jumpCount = 0;
                grounded = true;
                idleTimer = 0f;
                TweenShadow(1f, 1f); // 落地：阴影恢复

                // 落地 squash：压扁 + 旋转归零（打断未完成的空翻）+ 弹性回弹
                TweenBodyScale(1.2f, 0.8f);
                body.DORotate(Vector3.zero, 0.1f);

                // 输入缓冲补偿：落地前 jumpBufferTime 秒内按过空格 → 落地瞬间自动起跳
                if (Time.time - lastTapTime <= jumpBufferTime)
                {
                    lastTapTime = -999f; // 消费掉，防止一次按键跳两下
                    TryJump();
                }
            }
            body.localPosition = p;
        }
        else
        {
            // idle 小蹦跳：正弦叠加在 groundY 上，只影响显示不影响碰撞判定
            idleTimer += Time.deltaTime;
            Vector3 p = body.localPosition;
            p.y = groundY + idleAmplitude * Mathf.Sin(2f * Mathf.PI * idleFrequency * idleTimer);
            body.localPosition = p;

            // 阴影联动：身体蹦得越高阴影越小（轻微，制造接地感）
            if (shadow != null && idleAmplitude > 0.0001f)
            {
                float height01 = Mathf.Clamp01((p.y - groundY) / idleAmplitude);
                shadow.transform.localScale = shadowOriginalScale * (1f - 0.08f * height01);
            }
        }
    }

    private void TweenShadow(float scale, float alpha)
    {
        shadow.transform.DOKill();
        shadow.DOKill();
        shadow.transform.DOScale(shadowOriginalScale * scale, 0.15f);
        shadow.DOFade(alpha, 0.15f);
    }

    /// <summary>身体 squash & stretch：先快速变形，再弹性回弹回 1:1（动效）</summary>
    private void TweenBodyScale(float sx, float sy)
    {
        body.DOKill();
        body.DOScale(new Vector3(sx, sy, 1f), 0.08f)
            .OnComplete(() => body.DOScale(Vector3.one, 0.16f).SetEase(Ease.OutBack));
    }

    /// <summary>清场复位：由 GameManager.Restart() 调用</summary>
    public void ResetPlayer()
    {
        vy = 0f;
        jumpCount = 0;
        grounded = true;
        idleTimer = 0f;
        isDead = false;
        deadVx = 0f;
        lastTapTime = -999f; // 清输入缓冲，防重开后瞬跳

        // 死亡弹飞会改 X，必须整体复位回初始局部位形（旋转/缩放/颜色一并归零）
        body.DOKill();
        body.localPosition = new Vector3(bodyInitLocalPos.x, groundY, bodyInitLocalPos.z);
        body.localRotation = Quaternion.identity;
        body.localScale = Vector3.one;

        if (bodySR != null)
        {
            bodySR.DOKill();
            bodySR.color = Color.white; // 死亡变灰后复位
        }

        if (shadow != null)
        {
            shadow.transform.DOKill();
            shadow.DOKill();
            shadow.transform.localScale = shadowOriginalScale;
            Color c = shadow.color; c.a = 1f; shadow.color = c;
        }
    }

    /// <summary>死亡入口：弹飞 + 向后翻滚 + 变灰（死亡演出）</summary>
    public void Die()
    {
        if (isDead) return; // 同帧碰多只敌人时防重复触发
        isDead = true;

        Debug.Log("[Player] 碰敌死亡触发");

        vy = deathBounce;
        deadVx = deathVx;

        // 翻滚：先杀掉空翻/落地旋转的残余补间
        body.DOKill();
        body.DORotate(new Vector3(0f, 0f, deathSpinDegrees), 0.9f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad);

        // 变灰
        if (bodySR != null)
        {
            bodySR.DOKill();
            bodySR.DOColor(k_grayColor, 0.15f);
        }

        OnDied?.Invoke();
    }
}