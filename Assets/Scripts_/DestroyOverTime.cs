using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyOverTime : MonoBehaviour
{
    public float lifeTime_ = 1.5f;


    // Start is called before the first frame update
    void Start()
    {
        Destroy(this.gameObject, lifeTime_);
    }

}
