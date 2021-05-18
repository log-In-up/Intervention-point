using UnityEngine;


namespace InterventionPoint
{
    [DisallowMultipleComponent]
    sealed class PlayerAnimatorParameters : MonoBehaviour
    {
        #region Parameters
        [Header("Parameters")]
        [SerializeField, Tooltip("This parameter of the animator is responsible for aiming.")]
        internal string aim = "Aim";

        [SerializeField, Tooltip("This parameter of the animator is responsible for running.")]
        internal string run = "Run";

        [SerializeField, Tooltip("This parameter of the animator is responsible for walking.")]
        internal string walk = "Walk";

        [Header("Animation titles")]
        [SerializeField, Tooltip("The name of the state of the animator responsible for shooting without aiming.")]
        internal string fire = "Fire";

        [SerializeField, Tooltip("The name of the state of the animator responsible for aiming firing.")]
        internal string aimFire = "Aim Fire";

        [SerializeField, Tooltip("Name of the state of the animator responsible for reload out of ammo")]
        internal string reloadOutOfAmmo = "Reload Out Of Ammo";

        [SerializeField, Tooltip("Name of the state of the animator responsible for reload ammo left")]
        internal string reloadAmmoLeft = "Reload Ammo Left";
        #endregion
    }
}
