using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.TextCore.Text;
using Unity.VisualScripting;
using System.Linq;

public class PlayerController_02 : MonoBehaviourPunCallbacks, IPooledObject_1
{
    //Variables
    [SerializeField] private Camera cam_;
    [SerializeField] private float mouseSensitivity_ = 1f;
    [SerializeField] private bool invertLook_ = false;
    [SerializeField] public Transform viewPoint_;
    [SerializeField] private CharacterController characterController_;
    [SerializeField] private float runSpeed_ = 8f;
    [SerializeField] private float walkSpeed_ = 5f;
    [SerializeField] private float jumpForce_ = 12f;
    [SerializeField] private float gravityMod_ = 2.5f;
    public Transform groundCheckPoint_;
    public bool isGrounded_;
    public LayerMask groundLayers_;
    [SerializeField] private GameObject bulletImpact_;
    [SerializeField] public float shotCounter_;

    private Vector2 mouseInput_;
    private float verticalRotation_;
    private Vector3 moveDirection_;
    private float activeMoveSpeed_;
    private Vector3 movement_;

    [SerializeField] private float holdTime_ = 0f;
    [SerializeField] private float maxHeat_ = 10f;
    [SerializeField] private float coolRate_ = 4f;
    [SerializeField] private float overHeatCoolRate_ = 4f;
    [SerializeField] private bool overHeated_;
    public float heatCounter_;
    public float minHoldTime_ = 0.25f;
    public Gun[] allGuns_;
    public int selectedGun_;
    public float muzzleDisplayTime_;
    private float muzzleCounter_;

    [SerializeField] private GameObject playerHitImpact_;
    public PhotonView photonView_;
    public int maxHealth_ = 150;
    [SerializeField] private int currHealth_;
    [SerializeField] private Animator animator_;
    [SerializeField] private GameObject playerModel_;
    [SerializeField] private Transform modelGunPoint_;
    [SerializeField] private Transform gunHolder_;

    public Material[] allSkins_;
    public float adsSpeed_ = 5f;

    public AudioSource footStepSlow_;
    public AudioSource footStepFast_;

    private void Awake()
    {
        photonView_ = GetComponent<PhotonView>();
        viewPoint_ = this.gameObject.GetComponentInChildren<Transform>().Find("View Point");
        characterController_ = this.gameObject.GetComponent<CharacterController>();
        groundCheckPoint_ = this.gameObject.GetComponentInChildren<Transform>().Find("Ground Check Point");
        cam_ = Camera.main;
        currHealth_ = maxHealth_;
        animator_ = this.gameObject.GetComponentInChildren<Transform>().Find("Player Character").Find("Root").GetComponent<Animator>();

        #region Old Code to SpawnPlayer
        //newTrans_ = PlayerSpawner.playerSpawnerInstance_.GetSpawnPointInRandom();
        //this.transform.position = newTrans_.transform.position;
        //this.transform.rotation = newTrans_.transform.rotation;

        //PhotonNetwork.Instantiate(playerSpawner_.playerToSpawn_.gameObject.name, playerSpawner_.spawnPoint.position, playerSpawner_.spawnPoint.rotation);
        #endregion
    }

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        if (photonView_.IsMine)
        {
            UI_Controller.uiControllerInstance_.weaponTempSlider_.maxValue = maxHeat_;
            UI_Controller.uiControllerInstance_.healthSlider_.maxValue = maxHealth_;
            UI_Controller.uiControllerInstance_.healthSlider_.value = currHealth_;
            Debug.Log(playerModel_);
            playerModel_.SetActive(false);
        }
        else
        {
            gunHolder_.parent = modelGunPoint_;
            gunHolder_.localPosition = Vector3.zero;
            gunHolder_.localRotation = Quaternion.identity;
        }

        //SwitchGun();
        photonView_.RPC("SetGun", RpcTarget.All, selectedGun_);

        playerModel_.GetComponent<Renderer>().material = allSkins_[photonView_.Owner.ActorNumber % allSkins_.Length];
    }

    // Update is called once per frame
    void Update()
    {
        if (photonView_.IsMine)
        {
            MouseInput();
            PlayerRotation();
            PlayerMovement();
            MouseUnlock();
        }
    }

    private void LateUpdate()
    {
        if (photonView_.IsMine)
        {
            if (MatchManager.matchMgrInstance_.gameState_ == MatchManager.GameState.Playing)
            {
                cam_.transform.position = viewPoint_.position;
                cam_.transform.rotation = viewPoint_.rotation;
            }
            else
            {
                cam_.transform.position = MatchManager.matchMgrInstance_.mapCamPoint_.position;
                cam_.transform.rotation = MatchManager.matchMgrInstance_.mapCamPoint_.rotation;
            }
        }
    }

    public void MouseInput()
    {
        mouseInput_ = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity_;
    }

    public void PlayerRotation()
    {
        this.transform.rotation = Quaternion.Euler(this.gameObject.transform.rotation.eulerAngles.x, this.gameObject.transform.rotation.eulerAngles.y + mouseInput_.x, this.gameObject.transform.rotation.eulerAngles.z);
        ViewPointRotation();
    }

    public void ViewPointRotation()
    {
        verticalRotation_ += mouseInput_.y;
        verticalRotation_ = Mathf.Clamp(verticalRotation_, -60f, 60f);

        if (invertLook_)
        {
            viewPoint_.rotation = Quaternion.Euler(verticalRotation_, viewPoint_.rotation.eulerAngles.y, viewPoint_.rotation.eulerAngles.z);
        }
        else
        {
            viewPoint_.rotation = Quaternion.Euler(-verticalRotation_, viewPoint_.rotation.eulerAngles.y, viewPoint_.rotation.eulerAngles.z);
        }
    }

    public void MouseUnlock()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
        }
        else if (Cursor.lockState == CursorLockMode.None)
        {
            if (Input.GetMouseButtonDown(0) && !UI_Controller.uiControllerInstance_.optionsScreen_.activeInHierarchy)
            {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    public void PlayerMovement()
    {
        moveDirection_ = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

        if (Input.GetKey(KeyCode.LeftShift))
        {
            activeMoveSpeed_ = runSpeed_;

            if(!footStepFast_.isPlaying && moveDirection_ != Vector3.zero)
            {
                footStepFast_.Play();
                footStepSlow_.Stop();
            }
        }
        else
        {
            activeMoveSpeed_ = walkSpeed_;

            if (!footStepSlow_.isPlaying && moveDirection_ != Vector3.zero)
            {
                footStepFast_.Stop();
                footStepSlow_.Play();
            }
        }

        if(moveDirection_ == Vector3.zero || !isGrounded_)
        {
            footStepFast_.Stop();
            footStepSlow_.Stop();
        }

        float yVelocity = movement_.y;

        movement_ = ((transform.forward * moveDirection_.z) + (transform.right * moveDirection_.x)).normalized * activeMoveSpeed_;

        movement_.y = yVelocity;

        if (characterController_.isGrounded)
        {
            movement_.y = 0f;
        }

        isGrounded_ = Physics.Raycast(groundCheckPoint_.position, Vector3.down, 0.25f, groundLayers_);

        if (Input.GetButtonDown("Jump") && isGrounded_)
        {
            movement_.y = jumpForce_;
        }

        movement_.y += Physics.gravity.y * Time.deltaTime * gravityMod_;

        characterController_.Move(movement_ * Time.deltaTime);

        if (allGuns_[selectedGun_].muzzleFlash_.activeInHierarchy)
        {
            muzzleCounter_ -= Time.deltaTime;

            if (muzzleCounter_ <= 0)
            {
                allGuns_[selectedGun_].muzzleFlash_.SetActive(false);
            }
        }

        animator_.SetBool("grounded", isGrounded_);
        animator_.SetFloat("speed", moveDirection_.magnitude);

        CheckMouseClick();
        WeaponChange();

        if (Input.GetMouseButton(1))
        {
            cam_.fieldOfView = Mathf.Lerp(cam_.fieldOfView, allGuns_[selectedGun_].adsZoom_, adsSpeed_ * Time.deltaTime);
        }
        else
        {
            cam_.fieldOfView = Mathf.Lerp(cam_.fieldOfView, 60f, adsSpeed_ * Time.deltaTime);
        }
    }

    //Mouse Click need to check for the Rapid Shots and Single shot
    public void CheckMouseClick()
    {
        if (!overHeated_)
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnClick();
                holdTime_ = 0;
            }

            if (Input.GetMouseButton(0) && !Input.GetMouseButtonUp(0) && allGuns_[selectedGun_].isAutomatic_ == true)
            {
                holdTime_ += 0.3f + Time.deltaTime;
            }

            if (holdTime_ >= minHoldTime_)
            {
                OnHold();
                holdTime_ = 0;
                return;
            }

            heatCounter_ -= coolRate_ * Time.deltaTime;
        }
        else
        {
            heatCounter_ -= overHeatCoolRate_ * Time.deltaTime;
            if (heatCounter_ <= 0)
            {
                overHeated_ = false;

                UI_Controller.uiControllerInstance_.overHeatedMessage_.gameObject.SetActive(false);
            }
        }

        if (heatCounter_ < 0)
        {
            heatCounter_ = 0f;
        }

        UI_Controller.uiControllerInstance_.weaponTempSlider_.value = heatCounter_;
    }

    public void OnClick()
    {
        Shoot();
    }

    public void OnHold()
    {
        Debug.LogWarning("The SHOT COUNTER VALUE BEFORE UPDATING " + shotCounter_);

        shotCounter_ -= Time.deltaTime;
        if (shotCounter_ <= 0)
        {
            Shoot();
        }
    }

    public void Shoot()
    {
        Ray ray = cam_.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        ray.origin = cam_.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            //Debug.Log("We hit " + hit.collider.gameObject.name);

            if (hit.collider.gameObject.tag == "Player")
            {
                Debug.Log("Hit " + hit.collider.gameObject.GetPhotonView().Owner.NickName);
                PhotonNetwork.Instantiate(playerHitImpact_.name, hit.point, Quaternion.identity);
                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView_.Owner.NickName, allGuns_[selectedGun_].shotDamage_, PhotonNetwork.LocalPlayer.ActorNumber);
            }
            else
            {
                ObjectPooler.objectPoolerInstance.SpawnFromPool("Bullet Impact", hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));
            }
        }

        shotCounter_ = allGuns_[selectedGun_].timeBetweenShots_;

        heatCounter_ += allGuns_[selectedGun_].heatPerShot_;

        if (heatCounter_ >= maxHeat_)
        {
            heatCounter_ = maxHeat_;
            overHeated_ = true;
            UI_Controller.uiControllerInstance_.overHeatedMessage_.gameObject.SetActive(true);
        }

        allGuns_[selectedGun_].muzzleFlash_.SetActive(true);
        muzzleCounter_ = muzzleDisplayTime_;

        allGuns_[selectedGun_].shotSound_.Stop();
        allGuns_[selectedGun_].shotSound_.Play();
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount, int actor)
    {
        TakeDamage(damager, damageAmount, actor);
    }

    public void TakeDamage(string damager, int damageAmount, int actor)
    {
        if(photonView_.IsMine)
        {
            currHealth_ -= damageAmount;

            if (currHealth_ <= 0)
            {
                currHealth_ = 0;
                PlayerSpawner.playerSpawnerInstance_.Die(damager);
                MatchManager.matchMgrInstance_.UpdateStatsSend(actor, 0, 1);
            }

            UI_Controller.uiControllerInstance_.healthSlider_.value = currHealth_;
        }
    }

    public void WeaponChange()
    {
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
        {
            selectedGun_++;

            if (selectedGun_ >= allGuns_.Length)
            {
                selectedGun_ = 0;
            }

            //SwitchGun();
            photonView_.RPC("SetGun", RpcTarget.All, selectedGun_);
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
        {
            selectedGun_--;

            if (selectedGun_ < 0)
            {
                selectedGun_ = allGuns_.Length - 1;
            }

            //SwitchGun();
            photonView_.RPC("SetGun", RpcTarget.All, selectedGun_);
        }

        for (int i = 0; i < allGuns_.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                selectedGun_ = i;
                //SwitchGun();
                photonView_.RPC("SetGun", RpcTarget.All, selectedGun_);
            }
        }
    }

    public void SwitchGun()
    {
        foreach (Gun gun in allGuns_)
        {
            gun.gameObject.SetActive(false);
        }

        //Debug.Log(allGuns_[selectedGun_] + "This is my gun.");
        allGuns_[selectedGun_].gameObject.SetActive(true);

        allGuns_[selectedGun_].muzzleFlash_.SetActive(false);
    }

    [PunRPC]
    public void SetGun(int gunToSwitch)
    {
        if(gunToSwitch < allGuns_.Length)
        {
            selectedGun_ = gunToSwitch;
            SwitchGun();
        }
    }

}
