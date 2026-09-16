using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 自动射击 + 武器系统：豌豆(默认)/机枪(高频)/玉米(爆炸弹)，拾气泡切换，10s回默认
/// 武器计时用 GameManager.GameTime 差值（暂停/GameOver 天然冻结）
/// 挂载点：Player
/// </summary>
public class PlayerShooter : MonoBehaviour
{
    public enum WeaponType { Pea, MachineGun, Corn }

    [Header("射击参数")]
    [SerializeField] private float peaFireInterval = 0.8f;
    [SerializeField] private float machineGunFireInterval = 0.2f;
    [SerializeField] private float weaponDuration = 10f;
    [SerializeField] private int prewarmCount = 10;

    [Header("引用")]
    [SerializeField] private GameObject peaBulletPrefab;   // 拖 Bullet prefab
    [SerializeField] private GameObject cornBulletPrefab;  // 拖 CornBullet prefab
    [SerializeField] private GameObject explosionPrefab;   // 拖 Explosion prefab
    [SerializeField] private Transform muzzle;
    [SerializeField] private Transform bulletRoot;

    [Header("嘴部三形态")]
    [SerializeField] private SpriteRenderer mouth;        // 拖 Body/Mouth
    [SerializeField] private Sprite peaMouth;             // 主角_嘴
    [SerializeField] private Sprite machineGunMouth;      // 机枪嘴
    [SerializeField] private Sprite cornMouth;            // 玉米嘴

    [Header("武器 HUD")]
    [SerializeField] private Image weaponIcon;            // 拖 HUD/WeaponIcon
    [SerializeField] private Sprite machineGunIcon;       // 机枪.png
    [SerializeField] private Sprite cornIcon;             // 玉米.png
    [SerializeField] private Image timeFill;              // 拖 HUD/WeaponTimeBg/Fill
    [SerializeField] private TMP_Text timeText;           // 拖 HUD/WeaponTimeText

    private float fireTimer;
    private WeaponType current = WeaponType.Pea;
    private float weaponEndTime = float.NegativeInfinity;

    private void Start()
    {
        GameManager.OnStateChanged += HandleStateChanged;
        if (GameManager.Instance.State == GameManager.GameState.Playing)
            Prewarm();
        fireTimer = 0f;
    }

    private void OnDestroy()
    {
        GameManager.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.Playing)
        {
            fireTimer = 0f;
            SwitchWeapon(WeaponType.Pea); // 重开后嘴部/武器回默认（验收 7.5）
            Prewarm();
        }
    }

    private void Prewarm()
    {
        ObjectPool.Prewarm(peaBulletPrefab, prewarmCount);
        if (cornBulletPrefab != null) ObjectPool.Prewarm(cornBulletPrefab, 4);
        if (explosionPrefab != null) ObjectPool.Prewarm(explosionPrefab, 3);
    }

    private void Update()
    {
        if (GameManager.Instance.State != GameManager.GameState.Playing) return;

        if (current != WeaponType.Pea && GameManager.Instance.GameTime >= weaponEndTime)
            SwitchWeapon(WeaponType.Pea);

        RefreshWeaponHud();

        fireTimer += Time.deltaTime;
        float interval = current == WeaponType.MachineGun ? machineGunFireInterval : peaFireInterval;
        if (fireTimer < interval) return;
        fireTimer = 0f;
        Fire();
    }

    private void Fire()
    {
        GameObject prefab = current == WeaponType.Corn ? cornBulletPrefab : peaBulletPrefab;
        GameObject go = ObjectPool.Get(prefab);
        go.transform.SetParent(bulletRoot, true);
        go.transform.position = muzzle.position;
    }

    /// <summary>武器切换：气泡拾取/到时回默认 都走这里</summary>
    public void SwitchWeapon(WeaponType type)
    {
        current = type;
        fireTimer = 0f; // 换武器瞬间连发不叠弹（验收 8.5）

        switch (type)
        {
            case WeaponType.MachineGun:
                mouth.sprite = machineGunMouth;
                weaponEndTime = GameManager.Instance.GameTime + weaponDuration;
                if (weaponIcon != null) weaponIcon.sprite = machineGunIcon;
                break;
            case WeaponType.Corn:
                mouth.sprite = cornMouth;
                weaponEndTime = GameManager.Instance.GameTime + weaponDuration;
                if (weaponIcon != null) weaponIcon.sprite = cornIcon;
                break;
            default:
                mouth.sprite = peaMouth;
                weaponEndTime = float.NegativeInfinity;
                break;
        }
    }

    private void RefreshWeaponHud()
    {
        if (weaponIcon == null) return;

        bool active = current != WeaponType.Pea;
        weaponIcon.gameObject.SetActive(active);
        if (timeFill != null) timeFill.transform.parent.gameObject.SetActive(active);
        if (timeText != null) timeText.gameObject.SetActive(active);
        if (!active) return;

        float remain = weaponEndTime - GameManager.Instance.GameTime;
        if (timeFill != null) timeFill.fillAmount = Mathf.Clamp01(remain / weaponDuration);
        if (timeText != null) timeText.text = Mathf.Ceil(remain).ToString();
    }
}