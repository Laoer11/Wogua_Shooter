using System.Collections.Generic;
using UnityEngine;

/// <summary>池化对象接口：生成/归还时由池自动调用</summary>
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}

/// <summary>
/// 通用对象池：子弹/敌人/气泡/粒子共用，运行期零 Instantiate/Destroy
/// </summary>
public static class ObjectPool
{
    private static readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
    private static readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

    public static void Prewarm(GameObject prefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject go = Object.Instantiate(prefab);
            go.SetActive(false);
            instanceToPrefab[go] = prefab;
            GetQueue(prefab).Enqueue(go);
        }
    }

    public static GameObject Get(GameObject prefab)
    {
        Queue<GameObject> queue = GetQueue(prefab);
        GameObject go = queue.Count > 0 ? queue.Dequeue() : Object.Instantiate(prefab);
        instanceToPrefab[go] = prefab;
        go.SetActive(true);
        go.GetComponent<IPoolable>()?.OnSpawn();
        return go;
    }

    public static void Release(GameObject instance)
    {
        if (!instanceToPrefab.TryGetValue(instance, out GameObject prefab)) return;
        instance.GetComponent<IPoolable>()?.OnDespawn();
        instance.SetActive(false);
        GetQueue(prefab).Enqueue(instance);
    }

    /// <summary>重开清场：销毁所有池化实例（含场上仍激活的），先回调 OnDespawn 杀 tween 防孤儿，Restart 调用</summary>
    public static void ClearAll()
    {
        foreach (GameObject go in instanceToPrefab.Keys)
        {
            go.GetComponent<IPoolable>()?.OnDespawn();
            Object.Destroy(go);
        }
        pools.Clear();
        instanceToPrefab.Clear();
    }

    private static Queue<GameObject> GetQueue(GameObject prefab)
    {
        if (!pools.TryGetValue(prefab, out Queue<GameObject> q))
        {
            q = new Queue<GameObject>();
            pools[prefab] = q;
        }
        return q;
    }
}
