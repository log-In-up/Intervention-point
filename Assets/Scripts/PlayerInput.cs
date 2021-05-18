using UnityEngine;

namespace InterventionPoint
{
    [DisallowMultipleComponent]
    sealed class PlayerInput : MonoBehaviour
    {
        #region Parameters
        [SerializeField, Tooltip("The name of the virtual button mapped to aim.")]
        private string aim = "Fire2";

        [SerializeField, Tooltip("The name of the virtual button mapped to weapon reload.")]
        private string reload = "Reload";

        [SerializeField, Tooltip("The axis responsible for the movement of the player to the left or right.")]
        private string horizontal = "Horizontal";

        [SerializeField, Tooltip("The name of the virtual button mapped to jump.")]
        private string jump = "Jump";

        [SerializeField, Tooltip("The name of the virtual axis mapped to rotate the camera around the X axis.")]
        private string rotationAxisX = "Mouse X";

        [SerializeField, Tooltip("The name of the virtual axis mapped to rotate the camera around the Y axis.")]
        private string rotationAxisY = "Mouse Y";

        [SerializeField, Tooltip("The button responsible for accelerating the movement of the player.")]
        private string run = "Fire3";

        [SerializeField, Tooltip("The axis responsible for the movement of the player up or down.")]
        private string vertical = "Vertical";

        [SerializeField, Tooltip("The name of the virtual button mapped to fire.")]
        private string fire = "Fire1";
        #endregion

        #region Properties
        public bool Aim => Input.GetButton(aim);

        public bool Reload => Input.GetButton(reload);

        public bool Jump => Input.GetButtonDown(jump);

        public bool Run => Input.GetButton(run);

        public bool Shot => Input.GetButton(fire);

        public float Horizontal => Input.GetAxisRaw(horizontal);

        public float RotateX => Input.GetAxisRaw(rotationAxisX);

        public float RotateY => Input.GetAxisRaw(rotationAxisY);

        public float Vertical => Input.GetAxisRaw(vertical);
        #endregion
    }
}
