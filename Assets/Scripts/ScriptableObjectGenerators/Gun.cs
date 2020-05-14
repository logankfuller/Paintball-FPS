using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace me.loganfuller.multiplayerfps
{
    [CreateAssetMenu(fileName = "New Gun", menuName = "Gun")]
    public class Gun : ScriptableObject
    {
        public string gunName;
        public int damage;
        public float projectileSpeed;
        public int totalAmmo;
        public int clipSize;
        public float fireRate;
        public float bloom;
        public float recoil;
        public float kickback;
        public float reloadSpeed;
        public float adsSpeed;
        public int fireType; // 0 = semi-auto, 1 = burst, 2 = full-auto
        public GameObject prefab;

        private int currentClip; // current clip
        private int stashAmmo; // current ammo

        public void Initialize()
        {
            stashAmmo = totalAmmo;
            currentClip = clipSize;
        }

        public bool FireBullet()
        {
            if(currentClip > 0)
            {
                currentClip -= 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reload()
        {
            if (currentClip == clipSize) return;
            if(stashAmmo != 0 && currentClip <= clipSize)
            {
                stashAmmo += currentClip;
                currentClip = Mathf.Min(clipSize, stashAmmo);
                stashAmmo -= currentClip;
            } 
        }

        public int GetStash()
        {
            return stashAmmo;
        }

        public int GetClip()
        {
            return currentClip;
        }
    }
}
