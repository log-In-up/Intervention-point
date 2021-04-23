using UnityEngine;
using static UnityEngine.Mathf;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
sealed class PlayerController : MonoBehaviour
{
    #region Parameters
    [SerializeField] private GameObject arms = null, playerEyes = null;
    [SerializeField] private float walkingSpeed = 1.0f, runningSpeed = 1.3f;
    [SerializeField] private PlayerInput input;

    private const bool freezeRotation = true;
    private const float zero = 0.0f;

    private Animator armsAnimator = null;
    private Camera playerCamera = null;
    private CapsuleCollider capsuleCollider = null;
    private Rigidbody rigidbody3D = null;
    #endregion

    #region MonoBehaviour API
    private void Awake()
    {
        armsAnimator = arms.GetComponent<Animator>();
        playerCamera = playerEyes.GetComponent<Camera>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        rigidbody3D = GetComponent<Rigidbody>();

        rigidbody3D.freezeRotation = freezeRotation;
    }

    private void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        Movement();
        Rotation();
    }
    #endregion

    #region Custom methods
    private void Movement()
    {
        Vector3 direction = new Vector3(input.Horizontal, zero, input.Vertical).normalized;
        Vector3 velocity = direction * (input.Run ? runningSpeed : walkingSpeed) * Time.deltaTime;

        transform.Translate(velocity.x, zero, velocity.z);
    }

    private void Rotation()
    {
        
    }
    #endregion

    #region Inner classes
    [System.Serializable]
    class PlayerInput
    {
        #region Parameters
        [SerializeField, Tooltip("The name of the virtual axis mapped to rotate the camera around the X axis.")]
        private string rotationAxisX = "Mouse X";

        [SerializeField,Tooltip("The name of the virtual axis mapped to rotate the camera around the Y axis.")]
        private string rotationAxisY = "Mouse Y";

        [SerializeField, Tooltip("The axis responsible for the movement of the player to the left or right.")]
        private string horizontal = "Horizontal";

        [SerializeField, Tooltip("The axis responsible for the movement of the player up or down.")]
        private string vertical = "Vertical";

        [SerializeField, Tooltip("The button responsible for accelerating the movement of the player.")]
        private string run = "Fire3";

        #endregion

        #region Properties
        /// <summary>
        /// The button responsible for accelerating the movement of the player.
        /// </summary>
        public bool Run => Input.GetButton(run);

        /// <summary>
        /// The axis responsible for the movement of the player to the left or right.
        /// </summary>
        public float Horizontal => Input.GetAxisRaw(horizontal);

        /// <summary>
        /// The axis responsible for the movement of the player up or down.
        /// </summary>
        public float Vertical => Input.GetAxisRaw(vertical);

        /// <summary>
        /// The name of the virtual axis mapped to rotate the camera around the X axis.
        /// </summary>
        public float RotateX => Input.GetAxisRaw(rotationAxisX);

        /// <summary>
        /// The name of the virtual axis mapped to rotate the camera around the Y axis.
        /// </summary>
        public float RotateY => Input.GetAxisRaw(rotationAxisY);
        #endregion
    }
    #endregion
}
