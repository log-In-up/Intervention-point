using UnityEngine;
using static UnityEngine.Mathf;
using static UnityEngine.Physics;
using static UnityEngine.Quaternion;

[RequireComponent(typeof(CapsuleCollider), typeof(Rigidbody))]
sealed class PlayerController : MonoBehaviour
{
    #region Parameters
    [Header("Game objects")]
    [SerializeField, Tooltip("A game object that contains the Animator component and is the visual part of the player.")]
    private GameObject arms = null;
    [SerializeField, Tooltip("A game object that contains a Camera component.")] private GameObject playerEyes = null;
    [SerializeField] private Transform groundCheck = null;

    [Header("Look settings")]
    [SerializeField, Tooltip("Maximum vertical angle.")] private float maxVerticalAngle = 90.0f;
    [SerializeField, Tooltip("The minimum vertical angle.")] private float minVerticalAngle = -90.0f;
    [SerializeField, Tooltip("Mouse sensitivity.")] private float mouseSensitivity = 7.0f;
    [SerializeField, Tooltip("Rotation smoothing time (in seconds).")] private float smoothRotation = 0.05f;

    [Header("Movement settings")]
    [SerializeField, Tooltip("Motion smoothing time (in seconds).")] private float movementSmoothness = 0.125f;
    [SerializeField, Tooltip("Player running speed (in m/s).")] private float runningSpeed = 4.0f;
    [SerializeField, Tooltip("Player walking speed (in m/s).")] private float walkingSpeed = 3.0f;

    [Header("Other")]
    [SerializeField, Tooltip("Layer mask for interacting with the surface.")] private LayerMask whatIsSurface;
    [SerializeField, Tooltip("The amount of force applied to the player when jumping (in Newtons).")]
    private float jumpForce = 5.0f;

    [Header("Input manager settings"), SerializeField] private PlayerInput input;

    private const bool freezeRotation = true, hided = false;
    private const float angleLimitation = 0.01f, circle = 360.0f, unfoldedCorner = 180.0f;
    private const int zero = 0;

    private bool isGrounded;
    private float groundCheckRadius;

    private Animator armsAnimator = null;
    private Camera playerCamera = null;
    private CapsuleCollider capsuleCollider = null;
    private Rigidbody rigidbody3D = null;

    private SmoothRotation rotationX = null, rotationY = null;
    private SmoothVelocity velocityX = null, velocityZ = null;
    #endregion

    #region Properties
    private float RotationAxisX => input.RotateX * mouseSensitivity;

    private float RotationAxisY => input.RotateY * mouseSensitivity;
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
        groundCheckRadius = capsuleCollider.radius;

        rotationX = new SmoothRotation(zero);
        rotationY = new SmoothRotation(zero);
        velocityX = new SmoothVelocity();
        velocityZ = new SmoothVelocity();

        Cursor.visible = hided;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        isGrounded = CheckSphere(groundCheck.position, groundCheckRadius, whatIsSurface);
    }

    private void Update()
    {
        Movement();
        Rotation();

        if (input.Jump && isGrounded)
        {
            Jump();
        }
    }
    #endregion

    #region Custom methods
    /// <summary>
    /// Constrains the vertical angle between <see cref="minVerticalAngle"/> and <see cref="maxVerticalAngle"/>.
    /// </summary>
    /// <param name="axisY">Vertical axis.</param>
    /// <returns>Returns the clamped vertical angle between <see cref="minVerticalAngle"/> and <see cref="maxVerticalAngle"/>.</returns>
    private float LimitVerticalRotation(float axisY)
    {
        float currentAngle = NormalizeAngle(arms.transform.eulerAngles.x);

        float minimumY = minVerticalAngle + currentAngle;
        float maximumY = maxVerticalAngle + currentAngle;

        return Clamp(axisY, minimumY + angleLimitation, maximumY - angleLimitation);
    }

    /// <summary>
    /// Normalizes the angle between <see cref="minVerticalAngle"/> and <see cref="maxVerticalAngle"/>.
    /// </summary>
    /// <param name="angle">Gets the angle in degrees.</param>
    /// <returns>Returns the angle in degrees.</returns>
    private float NormalizeAngle(float angle)
    {
        while (angle > unfoldedCorner)
        {
            angle -= circle;
        }

        while (angle <= -unfoldedCorner)
        {
            angle += circle;
        }

        return angle;
    }

    /// <summary>
    /// The method responsible for the player's jump.
    /// </summary>
    private void Jump()
    {
        rigidbody3D.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    /// <summary>
    /// The method responsible for the movement of the player.
    /// </summary>
    private void Movement()
    {
        Vector3 direction = new Vector3(input.Horizontal, zero, input.Vertical).normalized;
        Vector3 velocity = direction * (input.Run ? runningSpeed : walkingSpeed);

        Vector3 smoothedSpeed = new Vector3(velocityX.SpeedDamping(velocity.x, movementSmoothness), zero,
                velocityZ.SpeedDamping(velocity.z, movementSmoothness)) * Time.deltaTime;

        transform.Translate(smoothedSpeed.x, zero, smoothedSpeed.z);
    }

    /// <summary>
    /// The method responsible for rotating the player.
    /// </summary>
    private void Rotation()
    {
        float axisX = rotationX.DampRotationAngle(RotationAxisX, smoothRotation);
        float axisY = rotationY.DampRotationAngle(RotationAxisY, smoothRotation);

        float limitedY = LimitVerticalRotation(axisY);
        rotationY.CurrentAngle = limitedY;

        Vector3 worldVector = arms.transform.InverseTransformDirection(Vector3.up);
        Quaternion playerRotation = arms.transform.rotation * AngleAxis(axisX, worldVector) * AngleAxis(limitedY, Vector3.left);

        transform.eulerAngles = new Vector3(zero, playerRotation.eulerAngles.y, zero);
        arms.transform.rotation = playerRotation;
    }
    #endregion

    #region Nested classes
    [System.Serializable]
    private class PlayerInput
    {
        #region Parameters
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
        #endregion

        #region Properties
        /// <summary>
        /// The name of the virtual button mapped to jump.
        /// </summary>
        public bool Jump => Input.GetButtonDown(jump);

        /// <summary>
        /// The button responsible for accelerating the movement of the player.
        /// </summary>
        public bool Run => Input.GetButton(run);

        /// <summary>
        /// The axis responsible for the movement of the player to the left or right.
        /// </summary>
        public float Horizontal => Input.GetAxisRaw(horizontal);

        /// <summary>
        /// The name of the virtual axis mapped to rotate the camera around the X axis.
        /// </summary>
        public float RotateX => Input.GetAxisRaw(rotationAxisX);

        /// <summary>
        /// The name of the virtual axis mapped to rotate the camera around the Y axis.
        /// </summary>
        public float RotateY => Input.GetAxisRaw(rotationAxisY);

        /// <summary>
        /// The axis responsible for the movement of the player up or down.
        /// </summary>
        public float Vertical => Input.GetAxisRaw(vertical);
        #endregion
    }

    private class SmoothRotation
    {
        private float currentAngle, currentAngularVelocity;

        public float CurrentAngle { set => currentAngle = value; }

        public SmoothRotation(float startAngle) => currentAngle = startAngle;

        /// <summary>
        /// Gradually changes an angle given in degrees towards a desired goal angle over time.
        /// </summary>
        /// <param name="target">The position we are trying to reach.</param>
        /// <param name="smoothTime">Rotation smoothing time (in seconds).</param>
        /// <returns>Returns the damped angle in degrees.</returns>
        public float DampRotationAngle(float target, float smoothTime)
        {
            return currentAngle = SmoothDampAngle(currentAngle, target, ref currentAngularVelocity, smoothTime);
        }
    }

    private class SmoothVelocity
    {
        private float current, currentVelocity;

        /// <summary>
        /// Gradually softens the speed value over time.
        /// </summary>
        /// <param name="target">The target position that we want to achieve.</param>
        /// <param name="smoothTime">Motion smoothing time (in seconds).</param>
        /// <returns>Returns the damped velocity.</returns>
        public float SpeedDamping(float target, float smoothTime)
        {
            return current = SmoothDamp(current, target, ref currentVelocity, smoothTime);
        }
    }
    #endregion
}
