using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using static ObjectPooler;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag_;
        public GameObject prefab_;
        public int sizeOfPool_;
    }

    #region Singleton - Object Pooler
    public static ObjectPooler objectPoolerInstance;

    private void Awake()
    {
        if(objectPoolerInstance != null)
        {
            Destroy(this);
        }
        else
        {
            objectPoolerInstance = this;
        }
    }

    #endregion


    public List<Pool> poolList_1;
    public Dictionary<string, Queue<GameObject>> bulletImpactDict_1;

    // Start is called before the first frame update
    void Start()
    {
        bulletImpactDict_1 = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in poolList_1)
        {
            Queue<GameObject> bulletImpactObjectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.sizeOfPool_; i++)
            {
                GameObject bulletImpactObject = Instantiate(pool.prefab_);
                bulletImpactObject.SetActive(false);
                bulletImpactObjectPool.Enqueue(bulletImpactObject);
            }

            bulletImpactDict_1.Add(pool.tag_, bulletImpactObjectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if(!bulletImpactDict_1.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with Tag " + tag + " dosen't exist!");
            return null;
        }

        GameObject objectToSpawn = bulletImpactDict_1[tag].Dequeue();
        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        IPooledObject_1 pooledObject = objectToSpawn.GetComponent<IPooledObject_1>();

        if (pooledObject != null)
        {
            pooledObject.Shoot();
        }

        bulletImpactDict_1[tag].Enqueue(objectToSpawn);
        return objectToSpawn;
    }

}

