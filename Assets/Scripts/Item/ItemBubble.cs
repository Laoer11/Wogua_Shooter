using UnityEngine;
using DG.Tweening;

/// <summary>
/// 气泡道具：随世界速度左移、正弦浮动、星光闪烁、主角跳跃拾取换武器
/// 挂载点：ItemBubble prefab 根（Collider2D Trigger 必须挂根，否则收不到碰撞消息）
/// </summary>
public class ItemBubble : MonoBehaviour, IPoolable
{
    [Header("移动回收")]
    [SerializeField] private float recycleX = -8.5f;

    [Header("浮动")]
    [SerializeField] private float floatAmp = 0.15f;
    [SerializeField] private float floatDuration = 1.5f;

    [Header("闪光")]
    [SerializeField] private float sparkScaleMin = 0.8f;
    [SerializeField] private float sparkScaleMax = 1.2f;
    [SerializeField] private float sparkSpinSpeed = 30f ; // 度/秒

    [Header("引用")]
    [SerializeField] private SpriteRenderer icon;
    [SerializeField] private Transform[] sparks;

    private PlayerShooter.WeaponType weaponType;
    private float baseY;
    private float animTimer;

    private void Update()
    {
        if (GameManager.Instance.State != GameManager.GameState.Playing) return;

        transform.position += Vector3.left * (GameManager.Instance.GameSpeed * Time.deltaTime);
        if(transform.position.x < recycleX)
        {
            ObjectPool.Release(gameObject);
            return;
        }

        animTimer += Time.deltaTime;
        Vector3 p = transform.localPosition;
        p.y = baseY + floatAmp * Mathf.Sin(2f * Mathf.PI * animTimer / floatDuration);
        transform.localPosition = p;
    }

    public void Setup(PlayerShooter.WeaponType type, Sprite iconSprite)
    {
        weaponType = type;
        if(icon != null) icon.sprite = iconSprite;
        baseY = transform.localPosition.y;
        animTimer = 0f;
    }

    private void OnTriggerEnter2D (Collider2D other)
    {
        if (GameManager.Instance.State != GameManager.GameState.Playing) return;

        PlayerController pc = other.GetComponentInParent<PlayerController>(); if ( pc == null ) return ;
        pc.GetComponent<PlayerShooter>()?.SwitchWeapon(weaponType);
        ObjectPool.Release(gameObject);
    }

    public void OnSpawn () 
    {
        StartSparkAnim();
    } 

    public void OnDespawn () 
    { 
        if (sparks == null ) return; 
        foreach (Transform s in sparks) s.DOKill();
    } 

    private void StartSparkAnim () 
    { 
        if (sparks == null ) return; 
        foreach(Transform s in sparks)
        {
            s.DOKill();
            s.localScale = Vector3.one * sparkScaleMin;
            s.DOScale(sparkScaleMax, 0.6f )
                .SetLoops( -1 , LoopType.Yoyo).SetEase(Ease.InOutSine);
            s.DORotate( new Vector3( 0f , 0f , -360f ), 360f / sparkSpinSpeed, RotateMode.FastBeyond360)
                .SetLoops( -1 , LoopType.Restart).SetEase(Ease.Linear);
        }
    } 
}
