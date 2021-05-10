using UnityEngine;
using static UnityEngine.Mathf;
using static UnityEngine.Physics;
using static UnityEngine.Quaternion;

namespace InterventionPoint
{
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource), typeof(CapsuleCollider), typeof(Rigidbody))]
    public sealed class PlayerController : MonoBehaviour
    {
        #region Parameters
        [Header("Game objects")]
        [SerializeField, Tooltip("A game object that contains the Animator component and is the visual part of the player.")]
        private GameObject arms = null;
        [SerializeField, Tooltip("The point from which the surface is checked.")]
        private Transform groundCheck = null;

        [Header("Look settings")]
        [SerializeField, Tooltip("Maximum vertical angle.")] private float maxVerticalAngle = 90.0f;
        [SerializeField, Tooltip("The minimum vertical angle.")] private float minVerticalAngle = -90.0f;
        [SerializeField, Tooltip("Mouse sensitivity.")] private float mouseSensitivity = 7.0f;
        [SerializeField, Tooltip("Rotation smoothing time (in seconds).")] private float smoothRotation = 0.05f;

        [Header("Movement settings")]
        [SerializeField, Tooltip("Motion smoothing time (in seconds).")] private float movementSmoothness = 0.125f;
        [SerializeField, Tooltip("Player running speed (in m/s).")] private float runningSpeed = 4.0f;
        [SerializeField, Tooltip("Player walking speed (in m/s).")] private float walkingSpeed = 3.0f;

        [Header("Other settings")]
        [SerializeField, Tooltip("Player animator parameters")] private PlayerAnimatorParameters animatorParameters;
        [SerializeField, Tooltip("Input manager settings")] private PlayerInput input = null;
        [SerializeField] private LayerMask whatIsPlayer, whatIsSurface;
        [SerializeField, Tooltip("The amount of force applied to the player when jumping (in Newtons).")]
        private float jumpForce = 5.0f;

        [Header("Audio clips")]
        [SerializeField] private AudioClip running = null, walking = null;

        private const bool freezeRotation = true, hided = false, notMoving = false;
        private const float angleLimitation = 0.01f, circle = 360.0f, unfoldedCorner = 180.0f;
        private const int zero = 0;

        private bool isGrounded;
        private float groundCheckRadius;

        private Animator animator = null;
        private AudioSource playerAudioSource = null;
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
            animator = arms.GetComponent<Animator>();
            capsuleCollider = GetComponent<CapsuleCollider>();
            playerAudioSource = GetComponent<AudioSource>();
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
            Vector3 playerInput = new Vector3(input.Horizontal, zero, input.Vertical).normalized;
            Walk(playerInput);

            Vector3 gazeDirection = new Vector3(
                rotationX.DampRotationAngle(RotationAxisX, smoothRotation),
                rotationY.DampRotationAngle(RotationAxisY, smoothRotation),
                zero);
            Rotation(gazeDirection);


            if (input.Jump && isGrounded)
            {
                Jump();
            }
        }
        #endregion

        #region Custom methods        


        private void EmitWalkingSound()
        {
            if (isGrounded)
            {
                playerAudioSource.clip = input.Run ? running : walking;
                if (!playerAudioSource.isPlaying)
                {
                    playerAudioSource.Play();
                }
            }
            else
            {
                playerAudioSource.Pause();
            }
        }

        private void Jump()
        {
            rigidbody3D.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        private float LimitVerticalRotation(float axisY)
        {
            float currentAngle = NormalizeAngle(arms.transform.eulerAngles.x);

            float minimumY = minVerticalAngle + currentAngle;
            float maximumY = maxVerticalAngle + currentAngle;

            return Clamp(axisY, minimumY + angleLimitation, maximumY - angleLimitation);
        }



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

        private void Rotation(Vector3 rotation)
        {
            float limitedY = LimitVerticalRotation(rotation.y);
            rotationY.CurrentAngle = limitedY;

            Vector3 worldVector = arms.transform.InverseTransformDirection(Vector3.up);
            Quaternion playerRotation = arms.transform.rotation * AngleAxis(rotation.x, worldVector) * AngleAxis(limitedY, Vector3.left);

            transform.eulerAngles = new Vector3(zero, playerRotation.eulerAngles.y, zero);
            arms.transform.rotation = playerRotation;
        }

        public void Walk(Vector3 normalizedPlayerInput)
        {
            Vector3 velocity = normalizedPlayerInput * (input.Run ? runningSpeed : walkingSpeed);

            if (normalizedPlayerInput.magnitude > zero)
            {
                animator.SetBool(animatorParameters.run, input.Run);
                animator.SetBool(animatorParameters.walk, !input.Run);

                EmitWalkingSound();
            }
            else
            {
                animator.SetBool(animatorParameters.run, notMoving);
                animator.SetBool(animatorParameters.walk, notMoving);

                playerAudioSource.Pause();
            }

            Vector3 smoothedSpeed = new Vector3(velocityX.SpeedDamping(velocity.x, movementSmoothness), zero,
                    velocityZ.SpeedDamping(velocity.z, movementSmoothness)) * Time.deltaTime;

            transform.Translate(smoothedSpeed.x, zero, smoothedSpeed.z);
        }
        #endregion
    }
}
