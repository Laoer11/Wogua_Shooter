using UnityEngine;

/// <summary> 
/// 背景三层视差滚动：天空静止、云层慢速、草地/泥土全速 
/// 挂载点：BackgroundRoot
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("天空层")]
    [SerializeField] private SpriteRenderer sky;

    [Header( "地面回绕组(草A、草B、泥A、泥B)" )]
    [ SerializeField ] private Transform[] tiles;

    [Header( "云回收组" )]
    [SerializeField] private Transform[] clouds;
    [SerializeField] private float cloudYMin = 0.5f;
    [SerializeField] private float cloudYMax = 2.2f;
    [SerializeField] private float cloudSpeedMin = 0.25f;
    [SerializeField] private float cloudSpeedMax = 0.45f;

    private const float TILE_WIDTH = 13.86f;
    private const float WRAP_DISTANCE = 27.72f;
    private const float DESIGN_ASPECT = 1386f / 640f;

    private float[] cloudSpeeds;
    private float[] cloudHalfWidths;
    private float camHalfWidth;

    private void Start()
    {
        Camera cam = Camera.main;
        camHalfWidth = cam.orthographicSize * cam.aspect;

        // 宽屏适配，横向拉伸天空铺满
        if(sky != null)
        {
            float scaleX = Mathf.Max(1f, cam.aspect / DESIGN_ASPECT);
            sky.transform.localScale = new Vector3(scaleX, 1f, 1f);
        }

        // 云初始化:随机视差系数、记录半宽、屏内随机分布
        cloudSpeeds = new float[clouds.Length];
        cloudHalfWidths = new float[clouds.Length];
        for(int i = 0; i < clouds.Length; i++)
        {
            cloudSpeeds[i] = Random.Range(cloudSpeedMin, cloudSpeedMax);
            cloudHalfWidths[i] = clouds[i].GetComponent<SpriteRenderer>().bounds.extents.x;
            clouds[i].position = new Vector3(
                Random.Range(-camHalfWidth, camHalfWidth),
                Random.Range(cloudYMin, cloudYMax),
                0f);
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float speed = GameManager.Instance.GameSpeed;

        // 地面移动
        for ( int i = 0 ; i < tiles.Length; i++)
        {
            Vector3 p = tiles[i].position;
            p.x -= speed * dt; if (p.x <= -(camHalfWidth + TILE_WIDTH * 0.5f ))
                p.x += WRAP_DISTANCE;
            tiles[i].position = p;
        }

        // 云移动
        for(int i = 0; i < clouds.Length; i++)
        {
            Vector3 p = clouds[i].position;
            p.x -= speed * cloudSpeeds[i] * dt;
            if(p.x + cloudHalfWidths[i] < -camHalfWidth)
            {
                p.x = camHalfWidth + cloudHalfWidths[i] + 0.5f;
                p.y = Random.Range(cloudYMin, cloudYMax);
                cloudSpeeds[i] = Random.Range(cloudSpeedMin, cloudSpeedMax);
            }
            clouds[i].position = p;
        }
    }
}
