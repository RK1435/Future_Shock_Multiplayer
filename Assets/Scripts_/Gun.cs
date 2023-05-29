using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gun : MonoBehaviour
{
    public bool isAutomatic_;
    public float timeBetweenShots_ = 0.1f;
    public float heatPerShot_ = 1f;
    public GameObject muzzleFlash_;
    public int shotDamage_;
    public float adsZoom_;
    public AudioSource shotSound_;
}
