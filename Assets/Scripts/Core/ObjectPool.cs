using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 단일 타입 오브젝트 풀. Instantiate/Destroy 대신 재활용합니다.
/// </summary>
public class ObjectPool<T> where T : MonoBehaviour
{
    private readonly T             prefab;
    private readonly Transform     parent;
    private readonly Queue<T>      pool = new Queue<T>();

    public ObjectPool(T prefab, Transform parent, int initialSize = 10)
    {
        this.prefab = prefab;
        this.parent = parent;
        for (int i = 0; i < initialSize; i++)
            pool.Enqueue(CreateNew());
    }

    public T Get(Vector3 position, Quaternion rotation)
    {
        T obj = pool.Count > 0 ? pool.Dequeue() : CreateNew();
        obj.transform.SetPositionAndRotation(position, rotation);
        obj.gameObject.SetActive(true);
        return obj;
    }

    public void Return(T obj)
    {
        obj.gameObject.SetActive(false);
        obj.transform.SetParent(parent);
        pool.Enqueue(obj);
    }

    private T CreateNew()
    {
        T obj = Object.Instantiate(prefab, parent);
        obj.gameObject.SetActive(false);
        return obj;
    }
}
