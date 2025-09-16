using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PoolItem
{
    public GameObject prefab;
    public int initialSize = 10;
    public bool canExpand = true;
    public int maxSize = 100;
}

public class ObjectPoolManager : MonoBehaviour
{
    [Header("=== POOL CONFIGURATION ===")]
    [SerializeField] private List<PoolItem> poolItems;

    private Dictionary<string, Queue<GameObject>> pools;
    private Dictionary<string, PoolItem> poolConfigs;

    private static ObjectPoolManager instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePools();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void InitializePools()
    {
        pools = new Dictionary<string, Queue<GameObject>>();
        poolConfigs = new Dictionary<string, PoolItem>();

        foreach (var item in poolItems)
        {
            CreatePool(item);
        }
    }

    void CreatePool(PoolItem item)
    {
        string key = item.prefab.name;
        pools[key] = new Queue<GameObject>();
        poolConfigs[key] = item;

        Transform container = new GameObject($"Pool_{key}").transform;
        container.SetParent(transform);

        for (int i = 0; i < item.initialSize; i++)
        {
            GameObject obj = Instantiate(item.prefab, container);
            obj.SetActive(false);
            pools[key].Enqueue(obj);
        }
    }

    public static GameObject Get(string prefabName, Vector3 position, Quaternion rotation)
    {
        if (instance.pools.ContainsKey(prefabName))
        {
            Queue<GameObject> pool = instance.pools[prefabName];

            if (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;
                obj.SetActive(true);
                return obj;
            }
            else if (instance.poolConfigs[prefabName].canExpand)
            {
                // Create new instance if pool can expand
                GameObject obj = Instantiate(
                    instance.poolConfigs[prefabName].prefab,
                    position,
                    rotation
                );
                return obj;
            }
        }

        Debug.LogWarning($"Pool for {prefabName} not found!");
        return null;
    }

    public static void Return(GameObject obj)
    {
        obj.SetActive(false);

        string key = obj.name.Replace("(Clone)", "").Trim();
        if (instance.pools.ContainsKey(key))
        {
            instance.pools[key].Enqueue(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
}