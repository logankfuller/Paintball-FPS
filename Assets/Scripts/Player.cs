using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using UnityEngine.UIElements;
using TMPro;

namespace me.loganfuller.multiplayerfps
{
    public class Player : MonoBehaviourPunCallbacks, IPunObservable
    {
        #region Variables

        public float speed;
        public float sprintModifier;
        public float crouchModifier; // Modify speed while crouching

        public float slideAmount;
        public float crouchAmount;
        public GameObject standingCollider;
        public GameObject crouchingCollider;

        public Camera normalCam;
        public Transform weaponParent;
        public GameObject cameraParent;

        public LayerMask ground;
        public Transform groundDetector;

        public float slideLength;
        public float slideModifier;

        private float baseFOV = 90;
        private float sprintFOV = 100;
        private Vector3 origin;

        public float jumpForce;

        private TMP_Text ammoUI;

        private Rigidbody rig;

        private Vector3 targetWeaponBobPosition;
        private Vector3 weaponParentOrigin;
        private Vector3 weaponParentCurrentPosition;

        private float movementCounter;
        private float idleCounter;

        private Collider[] hitColliders;

        public int maxHealth;
        private int currentHealth;

        private GameManager manager;
        private Weapon weapon;

        private float aimAngle;

        private bool crouched;

        private bool sliding;
        private float slideTime;
        private Vector3 slideDirection;

        #endregion

        #region Photon Callbacks

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if(stream.IsWriting)
            {
                stream.SendNext((int)(weaponParent.transform.localEulerAngles.x * 100f));
            }
            else
            {
                aimAngle = (int)stream.ReceiveNext() / 100f;
            }
        }

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            manager = GameObject.Find("Manager").GetComponent<GameManager>();
            weapon = GetComponent<Weapon>();

            currentHealth = maxHealth;

            cameraParent.SetActive(photonView.IsMine);

            if(!photonView.IsMine)
            {
                gameObject.layer = 11;
                standingCollider.layer = 11;
                crouchingCollider.layer = 11;
            }
            
            baseFOV = normalCam.fieldOfView;
            origin = normalCam.transform.localPosition;

            if(Camera.main)
            {
                Camera.main.enabled = false;
            }
            rig = GetComponent<Rigidbody>();

            if(photonView.IsMine)
            {
                ammoUI = GameObject.Find("HUD/Ammo/Text").GetComponent<TMP_Text>();
            }

            weaponParentOrigin = weaponParent.localPosition;
            weaponParentCurrentPosition = weaponParentOrigin;
        }

        private void Update()
        {
            if(!photonView.IsMine)
            {
                RefreshMultiplayerState();
                return;
            }

            // Axles
            float t_hmove = Input.GetAxisRaw("Horizontal");
            float t_vmove = Input.GetAxisRaw("Vertical");

            // Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift);
            bool jump = Input.GetKey(KeyCode.Space);
            bool crouch = Input.GetKeyDown(KeyCode.LeftControl);

            // States
            hitColliders = Physics.OverlapSphere(groundDetector.position, 0.15f, ground);
            bool isGrounded = hitColliders.Length > 0;
            bool isJumping = jump && isGrounded;
            bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded && !weapon.isAiming;
            bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;

            // Crouching
            if(isCrouching)
            {
                photonView.RPC("SetCrouch", RpcTarget.All, !crouched);
            }

            if (Input.GetKeyDown(KeyCode.U)) TakeDamage(500);

            // Camera Stuff
            if(sliding)
            {
                normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, sprintFOV * 1.25f, Time.deltaTime * 8f);
                normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * slideAmount, Time.deltaTime * 6f);
            }
            else
            {
                if (isSprinting)
                {
                    normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, sprintFOV, Time.deltaTime * 8f);
                }
                else
                {
                    normalCam.fieldOfView = Mathf.Lerp(normalCam.fieldOfView, baseFOV, Time.deltaTime * 8f);
                }

                if(crouched)
                {
                    normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin + Vector3.down * crouchAmount, Time.deltaTime * 10f);
                }
                else
                {
                    normalCam.transform.localPosition = Vector3.Lerp(normalCam.transform.localPosition, origin, Time.deltaTime * 6f);
                }
            }

            // Update UI
            weapon.RefreshAmmo(ammoUI);

            // Headbob
            if(sliding)
            {
                Headbob(movementCounter, 0.15f, 0.075f);
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
            }
            else if (t_hmove == 0 && t_vmove == 0) 
            {
                Headbob(idleCounter, 0.025f, 0.025f);
                idleCounter += Time.deltaTime;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 2f);
            }
            else if(!isSprinting && !crouched) 
            {
                Headbob(movementCounter, 0.035f, 0.035f);
                movementCounter += Time.deltaTime * 3f;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
            }
            else if(crouched)
            {
                Headbob(movementCounter, 0.02f, 0.02f);
                movementCounter += Time.deltaTime * 1.75f;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 6f);
            }
            else
            {
                Headbob(movementCounter, 0.15f, 0.075f);
                movementCounter += Time.deltaTime * 7f;
                weaponParent.localPosition = Vector3.Lerp(weaponParent.localPosition, targetWeaponBobPosition, Time.deltaTime * 10f);
            }   
        }

        void FixedUpdate()
        {
            if (!photonView.IsMine)
            {
                return;
            }

            // Axles
            float t_hmove = Input.GetAxisRaw("Horizontal");
            float t_vmove = Input.GetAxisRaw("Vertical");

            // Controls
            bool sprint = Input.GetKey(KeyCode.LeftShift);
            bool jump = Input.GetKey(KeyCode.Space);
            bool slide = Input.GetKey(KeyCode.C);

            // States
            hitColliders = Physics.OverlapSphere(groundDetector.position, 0.1f, ground);
            bool isGrounded = hitColliders.Length > 0;
            bool isJumping = jump && isGrounded;
            bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded && !weapon.isAiming;
            bool isSliding = isSprinting && slide && !sliding;

            // Jumping
            if (isJumping)
            {
                if (crouched) photonView.RPC("SetCrouch", RpcTarget.All, false);
                rig.AddForce(Vector3.up * jumpForce);
            }

            // Movement
            Vector3 t_direction = Vector3.zero;
            float t_adjustedSpeed = speed;

            if (!sliding)
            {
                t_direction = new Vector3(t_hmove, 0, t_vmove);
                t_direction.Normalize();
                t_direction = transform.TransformDirection(t_direction);

                // Sprinting
                if (isSprinting)
                {
                    if(crouched)
                    {
                        photonView.RPC("SetCrouch", RpcTarget.All, false);
                    }
                    t_adjustedSpeed *= sprintModifier;
                }
                else if(crouched)
                {
                    t_adjustedSpeed *= crouchModifier;
                }
            }
            else
            {
                t_direction = slideDirection;
                t_adjustedSpeed *= slideModifier;
                slideTime -= Time.deltaTime;
                if(slideTime <= 0)
                {
                    sliding = false;
                    weaponParentCurrentPosition -= Vector3.down * (slideAmount - crouchAmount);
                }
            }

            Vector3 t_targetVelocity = t_direction * t_adjustedSpeed * Time.deltaTime;
            t_targetVelocity.y = rig.velocity.y;
            rig.velocity = t_targetVelocity;


            // Sliding
            if (isSliding)
            {
                sliding = true;
                slideDirection = t_direction;
                slideTime = slideLength;

                // Adjust camera
                weaponParentCurrentPosition += Vector3.down * (slideAmount - crouchAmount);

                if(!crouched)
                {
                    photonView.RPC("SetCrouch", RpcTarget.All, true);
                }
            }
        }
        #endregion

        #region Private Methods

        [PunRPC]
        void SetCrouch(bool state)
        {
            if(crouched == state)
            {
                return;
            }

            crouched = state;

            if(crouched)
            {
                standingCollider.SetActive(false);
                crouchingCollider.SetActive(true);
                weaponParentCurrentPosition += Vector3.down * crouchAmount;
            }
            else
            {
                standingCollider.SetActive(true);
                crouchingCollider.SetActive(false);
                weaponParentCurrentPosition -= Vector3.down * crouchAmount;
            }
        }

        void RefreshMultiplayerState()
        {
            float cacheEulY = weaponParent.localEulerAngles.y;

            Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(aimAngle, Vector3.right);
            weaponParent.rotation = Quaternion.Slerp(weaponParent.rotation, targetRotation, Time.deltaTime * 8f);

            Vector3 finalRotation = weaponParent.localEulerAngles;
            finalRotation.y = cacheEulY;

            weaponParent.localEulerAngles = finalRotation;
        }

        void Headbob(float z, float xIntensity, float yIntensity)
        {
            float aimAdjust = 1f;
            if(weapon.isAiming)
            {
                aimAdjust = 0.1f;
            }
            targetWeaponBobPosition = weaponParentCurrentPosition + new Vector3(Mathf.Cos(z) * xIntensity * aimAdjust, Mathf.Sin(z * 2) * yIntensity * aimAdjust, 0);
        }

        #endregion

        #region Public Methods

        public void TakeDamage(int damage)
        {
            if (photonView.IsMine)
            {
                currentHealth -= damage;
                Debug.LogError(photonView.gameObject.GetPhotonView() + " | " + photonView.IsMine + " | " + currentHealth);
                if (currentHealth <= 0)
                {
                    manager.Spawn();
                    PhotonNetwork.Destroy(gameObject);
                }
            }
        }

        #endregion
    }
}
