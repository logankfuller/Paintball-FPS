using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

namespace me.loganfuller.multiplayerfps
{
    public class Weapon : MonoBehaviourPunCallbacks
    {
        #region Variables

        public bool isAiming;

        public LayerMask canBeShot;

        public Gun[] loadout;
        public Transform weaponParent;

        public GameObject paintSplatPrefab;

        private float currentCooldown;
        private int currentIndex;
        private GameObject currentWeapon;

        private bool isReloading;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            if(photonView.IsMine)
            {
                foreach (Gun gun in loadout)
                {
                    gun.Initialize();
                }
            }
            Equip(0);
        }

        void Update()
        {
            // Primary Weapon
            if (photonView.IsMine && Input.GetKeyDown(KeyCode.Alpha1))
            {
                photonView.RPC("Equip", RpcTarget.AllBuffered, 0);
            } 
            else if(photonView.IsMine && Input.GetKeyDown(KeyCode.O))
            {
                photonView.RPC("Equip", RpcTarget.AllBuffered, 1);
            }

            // ADS
            if (currentWeapon != null)
            {
                if(photonView.IsMine)
                {
                    // Aim down sights
                    Aim(Input.GetMouseButton(1));


                    // Then shoot if not on cooldown
                    if(loadout[currentIndex].fireType != 2)
                    {
                        if (Input.GetMouseButtonDown(0) && currentCooldown <= 0 && !isReloading)
                        {
                            if (loadout[currentIndex].FireBullet())
                            {
                                photonView.RPC("Shoot", RpcTarget.All);
                            }
                            else
                            {
                                StartCoroutine(Reload(loadout[currentIndex].reloadSpeed));
                            }
                        }
                    }
                    else
                    {
                        if (Input.GetMouseButton(0) && currentCooldown <= 0 && !isReloading)
                        {
                            if (loadout[currentIndex].FireBullet())
                            {
                                photonView.RPC("Shoot", RpcTarget.All);
                            }
                            else
                            {
                                StartCoroutine(Reload(loadout[currentIndex].reloadSpeed));
                            }
                        }
                    }
                    

                    // Reload
                    if(Input.GetKeyDown(KeyCode.R))
                    {
                        if(isReloading)
                        {
                            return;
                        }
                        photonView.RPC("ReloadRPC", RpcTarget.All);
                    }

                    // Shot Cooldown
                    if (currentCooldown > 0)
                    {
                        currentCooldown -= Time.deltaTime;
                    }
                }
                
                // Weapon Elasticity
                currentWeapon.transform.localPosition = Vector3.Lerp(currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
            }
        }

        #endregion

        #region Private Methods

        [PunRPC]
        private void ReloadRPC()
        {
            StartCoroutine(Reload(loadout[currentIndex].reloadSpeed));
        }

        // TODO Do not share across multiplayer
        IEnumerator Reload(float reloadTime)
        {
            isReloading = true;
            currentWeapon.SetActive(false);

            yield return new WaitForSeconds(reloadTime);

            loadout[currentIndex].Reload();
            currentWeapon.SetActive(true);

            isReloading = false;
        }

        [PunRPC]
        void Equip(int p_ind)
        {
            if (currentWeapon != null)
            {
                if (isReloading)
                {
                    StopCoroutine("Reload");
                }
                Destroy(currentWeapon);
            }

            currentIndex = p_ind;

            GameObject t_newWeapon = Instantiate(loadout[p_ind].prefab, weaponParent.position, weaponParent.rotation, weaponParent) as GameObject;
            t_newWeapon.transform.localPosition = Vector3.zero;
            t_newWeapon.transform.localEulerAngles = Vector3.zero;

            t_newWeapon.GetComponent<Sway>().isMine = photonView.IsMine;

            currentWeapon = t_newWeapon;
        }

        void Aim(bool p_isAiming)
        {
            Transform t_anchor = currentWeapon.transform.Find("Anchor");
            Transform t_state_ads = currentWeapon.transform.Find("States/ADS");
            Transform t_state_hip = currentWeapon.transform.Find("States/Hip");

            isAiming = p_isAiming;

            if (p_isAiming)
            {
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_ads.position, Time.deltaTime * loadout[currentIndex].adsSpeed);
            }
            else
            {
                t_anchor.position = Vector3.Lerp(t_anchor.position, t_state_hip.position, Time.deltaTime * loadout[currentIndex].adsSpeed);
            }
        }

        [PunRPC]
        void Shoot()
        {
            Transform t_spawn = transform.Find("Cameras/Normal Camera");

            // Cooldown
            currentCooldown = loadout[currentIndex].fireRate;

            // Create Bloom effect
            Vector3 t_bloom = t_spawn.position + t_spawn.forward * 1000f;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.up;
            t_bloom += Random.Range(-loadout[currentIndex].bloom, loadout[currentIndex].bloom) * t_spawn.right;
            t_bloom -= t_spawn.position;
            t_bloom.Normalize();

            //raycast
            RaycastHit t_hit = new RaycastHit();
            if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, canBeShot))
            {
                GameObject t_newHole = Instantiate(paintSplatPrefab, t_hit.point + t_hit.normal * 0.001f, Quaternion.identity) as GameObject;
                t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                Destroy(t_newHole, 5f);

                if(photonView.IsMine)
                {
                    if (t_hit.collider.gameObject.layer == 11)
                    {
                        t_hit.collider.transform.root.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All);
                    }
                }
            }

            // Gun FX
            currentWeapon.transform.Rotate(-loadout[currentIndex].recoil, 0, 0);
            currentWeapon.transform.position -= currentWeapon.transform.forward * loadout[currentIndex].kickback;
        }

        [PunRPC]
        private void DealDamage()
        {
            GetComponent<Player>().TakeDamage(loadout[currentIndex].damage);
        }

        #endregion

        #region Public Methods

        public void RefreshAmmo(TMP_Text text)
        {
            int clip = loadout[currentIndex].GetClip();
            int stash = loadout[currentIndex].GetStash();

            text.text = clip.ToString("00") + " / " + stash.ToString("00");
        }

        #endregion
    }
}
