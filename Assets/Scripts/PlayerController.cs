using System.Collections;
using UnityEngine;
using UnityEngine.UI;
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
        [SerializeField, Tooltip("A game object that contains a Camera component.")]
        private GameObject playerEyes = null;
        [SerializeField, Tooltip("The game object representing the casing prefab.")]
        private GameObject casingPrefab = null;
        [SerializeField, Tooltip("The game object representing the shooting source.")]
        private GameObject shotingAudioSource = null;
        [SerializeField, Tooltip("The game object representing the reloading source.")]
        private GameObject reloadingAudioSource = null;
        [SerializeField] private GameObject bulletInMag = null;
        [SerializeField, Tooltip("The point from which the surface is checked.")]
        private Transform groundCheck = null;
        [SerializeField, Tooltip("The point from which the bullet is fired.")]
        private Transform bulletSpawnPoint = null;
        [SerializeField, Tooltip("The point where the cartridge case is released from the weapon.")]
        private Transform casingSpawnPoint = null;

        [Header("Look settings")]
        [SerializeField, Tooltip("Maximum vertical angle.")] private float maxVerticalAngle = 90.0f;
        [SerializeField, Tooltip("The minimum vertical angle.")] private float minVerticalAngle = -90.0f;
        [SerializeField, Tooltip("Mouse sensitivity.")] private float mouseSensitivity = 7.0f;
        [SerializeField, Tooltip("Rotation smoothing time (in seconds).")] private float smoothRotation = 0.05f;
        [SerializeField] private float aimingFOV = 30.0f;

        [Header("Movement settings")]
        [SerializeField, Tooltip("Motion smoothing time (in seconds).")] private float movementSmoothness = 0.125f;
        [SerializeField, Tooltip("Player running speed (in m/s).")] private float runningSpeed = 4.0f;
        [SerializeField, Tooltip("Player walking speed (in m/s).")] private float walkingSpeed = 3.0f;

        [Header("Weapon settings")]
        [SerializeField, Tooltip("Bullets fired per second.")] private float rateOfFire = 0.2f;
        [SerializeField, Tooltip("")] private int weaponMagazineVolume = 30;
        [SerializeField, Tooltip("The distance at which the weapon can hit.")] private float shootingDistance = 400.0f;
        [SerializeField] private float lightDuration = 0.02f;
        [SerializeField] private float reloadOutOfAmmoTime = 3.0f;
        [SerializeField] private float reloadAmmoLeftTime = 2.133f;
        [SerializeField] private float showBulletDelay = 0.8f;
        [SerializeField] private ParticleSystem spark = null;
        [SerializeField] private ParticleSystem muzzleflash = null;
        [SerializeField] private Light muzzleflashLight = null;

        [Header("UI settings")]
        [SerializeField, Tooltip("UI element represents current ammo and maximum ammo.")] private Text ammo = null;

        [Header("Other settings")]
        [SerializeField, Tooltip("Player animator parameters")] private AnimatorParameters animatorParameters;
        [SerializeField, Tooltip("")] private AudioClips audioClips;
        [SerializeField, Tooltip("Input manager settings")] private PlayerInput input;
        [SerializeField] private LayerMask whatIsPlayer, whatIsSurface;
        [SerializeField, Tooltip("The amount of force applied to the player when jumping (in Newtons).")]
        private float jumpForce = 5.0f;

        private const bool disable = false, enable = true, freezeRotation = true, hided = false, notMoving = false, notReloaded = true, reloaded = false;
        private const float angleLimitation = 0.01f, circle = 360.0f, middleOfViewport = 0.5f, unfoldedCorner = 180.0f;
        private const int one = 1, zero = 0;

        private bool isGrounded, isReloading;
        private int currentAmmo, magazineVolume;
        private float groundCheckRadius, nextShot, standardFOV;

        private Animator animator = null;
        private AudioSource playerAudioSource = null, reloadingSource = null, weaponSource = null;
        private Camera playerCamera = null;
        private CapsuleCollider capsuleCollider = null;
        private Coroutine flashLightCoroutine = null, showBullet = null, weaponReload = null;
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
            playerCamera = playerEyes.GetComponent<Camera>();
            reloadingSource = reloadingAudioSource.GetComponent<AudioSource>();
            rigidbody3D = GetComponent<Rigidbody>();
            weaponSource = shotingAudioSource.GetComponent<AudioSource>();

            rigidbody3D.freezeRotation = freezeRotation;
            muzzleflashLight.enabled = disable;
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

            isReloading = reloaded;
            magazineVolume = currentAmmo = weaponMagazineVolume;
            standardFOV = playerCamera.fieldOfView;

            ammo.text = $"{currentAmmo}/{magazineVolume}";
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

            if (input.Reload)
            {
                ReloadWeapon();
            }

            if (!isReloading && !input.Run)
            {
                Shooting();
            }

            if (input.Jump && isGrounded)
            {
                Jump();
            }
        }
        #endregion        

        #region Custom methods        
        private void EmitEffects()
        {
            if (flashLightCoroutine != null)
            {
                StopCoroutine(flashLightCoroutine);
            }
            flashLightCoroutine = StartCoroutine(MuzzleFlashLight(lightDuration));

            spark.Emit(one);
            muzzleflash.Emit(one);
        }

        private void EmitWalkingSound()
        {
            if (isGrounded)
            {
                playerAudioSource.clip = input.Run ? audioClips.running : audioClips.walking;
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
        
        private void Fire()
        {
            Vector3 origin = playerCamera.ViewportToWorldPoint(new Vector3(middleOfViewport, middleOfViewport, zero));

            currentAmmo -= one;

            weaponSource.clip = audioClips.shoot;
            weaponSource.Play();

            ammo.text = $"{currentAmmo}/{magazineVolume}";

            //We shoot the ray from the Viewport and get the aiming point
            if (Raycast(origin, playerCamera.transform.forward, out RaycastHit raycastHit, shootingDistance, whatIsPlayer))
            {
                //We send a ray from the barrel of the weapon to the aiming point
                if (Raycast(bulletSpawnPoint.position, raycastHit.point, out RaycastHit hit))
                {
                    //Processing the hit
                }
            }

            Instantiate(casingPrefab, casingSpawnPoint.transform.position, casingSpawnPoint.transform.rotation);

            EmitEffects();
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

        private IEnumerator MuzzleFlashLight(float time)
        {
            muzzleflashLight.enabled = enable;
            yield return new WaitForSeconds(time);
            muzzleflashLight.enabled = disable;
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
        
        private IEnumerator Reload(float reloadTime)
        {
            yield return new WaitForSeconds(reloadTime);

            isReloading = reloaded;

            currentAmmo = magazineVolume;
            ammo.text = $"{currentAmmo}/{magazineVolume}";
        }
        
        private void ReloadWeapon()
        {
            if (currentAmmo != zero)
            {
                reloadingSource.clip = audioClips.reloadAmmoLeft;
                animator.Play(animatorParameters.reloadAmmoLeft, zero, zero);

                StartReload(reloadAmmoLeftTime);
            }
            else
            {
                reloadingSource.clip = audioClips.reloadOutOfAmmo;
                animator.Play(animatorParameters.reloadOutOfAmmo, zero, zero);

                StartReload(reloadOutOfAmmoTime);
            }
            reloadingSource.Play();
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
        
        private void Shooting()
        {
            bool aiming = input.Aim;
            playerCamera.fieldOfView = aiming ? aimingFOV : standardFOV;

            animator.SetBool(animatorParameters.aim, aiming);

            if (currentAmmo != zero)
            {
                if (input.Shot && Time.time > nextShot)
                {
                    nextShot = Time.time + rateOfFire;

                    if (aiming)
                    {
                        animator.Play(animatorParameters.aimFire, zero, zero);

                        weaponSource.clip = audioClips.aiming;
                        weaponSource.Play();
                    }
                    else
                    {
                        animator.Play(animatorParameters.fire, zero, zero);
                    }
                    Fire();
                }
            }
            else
            {
                bulletInMag.SetActive(disable);
            }
        }
        
        private IEnumerator ShowBulletsInMag(float time)
        {
            yield return new WaitForSeconds(time);

            bulletInMag.SetActive(enable);
        }
        
        private void StartReload(float time)
        {
            if (currentAmmo == zero)
            {
                if (showBullet != null)
                {
                    StopCoroutine(showBullet);
                }
                showBullet = StartCoroutine(ShowBulletsInMag(showBulletDelay));
            }

            if (weaponReload != null)
            {
                StopCoroutine(weaponReload);
            }
            isReloading = notReloaded;
            weaponReload = StartCoroutine(Reload(time));
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

        #region Nested classes
        [System.Serializable]
        private class AudioClips
        {
            [SerializeField] internal AudioClip aiming = null;
            [SerializeField] internal AudioClip reloadOutOfAmmo = null;
            [SerializeField] internal AudioClip reloadAmmoLeft = null;
            [SerializeField] internal AudioClip running = null;
            [SerializeField] internal AudioClip shoot = null;
            [SerializeField] internal AudioClip walking = null;
        }

        [System.Serializable]
        private class AnimatorParameters
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

            [SerializeField, Tooltip("")]
            internal string reloadOutOfAmmo = "Reload Out Of Ammo";

            [SerializeField, Tooltip("")]
            internal string reloadAmmoLeft = "Reload Ammo Left";
            #endregion
        }

        [System.Serializable]
        private class PlayerInput
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

        private class SmoothRotation
        {
            private float currentAngle, currentAngularVelocity;

            public float CurrentAngle { set => currentAngle = value; }

            public SmoothRotation(float startAngle) => currentAngle = startAngle;

            public float DampRotationAngle(float target, float smoothTime)
            {
                return currentAngle = SmoothDampAngle(currentAngle, target, ref currentAngularVelocity, smoothTime);
            }
        }

        private class SmoothVelocity
        {
            private float current, currentVelocity;

            public float SpeedDamping(float target, float smoothTime)
            {
                return current = SmoothDamp(current, target, ref currentVelocity, smoothTime);
            }
        }
        #endregion
    }
}
