using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

namespace me.loganfuller.multiplayerfps
{
    public class Sway : MonoBehaviour
    {
        #region Variables

        public float intensity;
        public float smoothing;
        public bool isMine;

        private Quaternion originRotation;

        #endregion

        #region MonoBehaviour Callbacks

        private void Start()
        {
            originRotation = transform.localRotation;
        }

        private void Update()
        {
            UpdateSway();
        }

        #endregion

        #region Private Methods

        private void UpdateSway()
        {
            // Controls
            float xMouse = Input.GetAxis("Mouse X");
            float yMouse = Input.GetAxis("Mouse Y");

            if(!isMine)
            {
                xMouse = 0;
                yMouse = 0;
            }

            // Calculate target rotation
            Quaternion xAdjustment = Quaternion.AngleAxis(-intensity * xMouse, Vector3.up);
            Quaternion yAdjustment = Quaternion.AngleAxis(intensity * yMouse, Vector3.right);
            Quaternion targetRotation = originRotation * xAdjustment * yAdjustment;

            // Rotate the gun smoothly towards the target rotation
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * smoothing);
        }

        #endregion
    }
}
