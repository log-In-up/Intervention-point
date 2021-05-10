using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using static UnityEngine.Physics;

namespace InterventionPoint
{
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    public class Shooting : MonoBehaviour
    {
        #region Parameters
        [Header("Audio sources")]
        [SerializeField, Tooltip("The game object representing the reloading source.")]
        private AudioSource reloadingSource = null;
        [SerializeField, Tooltip("The game object representing the shooting source.")]
        private AudioSource weaponSource = null;
        [SerializeField] private AudioClips audioClips;

        [Header("Components")]
        [SerializeField] private Camera playerCamera = null;
        [SerializeField, Tooltip("The game object representing the casing prefab.")]
        private GameObject casingPrefab = null;
        [SerializeField] private GameObject bulletInMag = null;
        [SerializeField, Tooltip("The point from which the bullet is fired.")]
        private Transform bulletSpawnPoint = null;
        [SerializeField, Tooltip("The point where the cartridge case is released from the weapon.")]
        private Transform casingSpawnPoint = null;
        [SerializeField, Tooltip("Player animator parameters")] private PlayerAnimatorParameters animatorParameters;
        [SerializeField, Tooltip("Input manager settings")] private PlayerInput input;
        [SerializeField] private LayerMask whatIsPlayer;

        [Header("UI settings")]
        [SerializeField, Tooltip("UI element represents current ammo and maximum ammo.")] private Text ammo = null;
        [SerializeField] private float aimingFOV = 30.0f;

        [Header("Weapon settings")]
        [SerializeField, Tooltip("Bullets fired per second.")] private float rateOfFire = 0.1f;
        [SerializeField, Tooltip("")] private int weaponMagazineVolume = 30;
        [SerializeField, Tooltip("The distance at which the weapon can hit.")] private float shootingDistance = 400.0f;
        [SerializeField]
        private float lightDuration = 0.02f, reloadOutOfAmmoTime = 3.0f, reloadAmmoLeftTime = 2.133f,
            showBulletDelay = 0.8f;
        [SerializeField] private ParticleSystem spark = null, muzzleflash = null;
        [SerializeField] private Light muzzleflashLight = null;

        private const bool disable = false, enable = true, notReloaded = true, reloaded = false;
        private const float middleOfViewport = 0.5f;
        private const int one = 1, zero = 0;

        private float nextShot, standardFOV;
        private int currentAmmo, magazineVolume;
        private bool isReloading;

        private Animator animator = null;
        private Coroutine flashLightCoroutine = null, showBullet = null, weaponReload = null;
        #endregion

        #region MonoBehaviour API

        private void Awake()
        {
            animator = GetComponent<Animator>();

            muzzleflashLight.enabled = disable;
        }

        private void Start()
        {
            isReloading = reloaded;
            magazineVolume = currentAmmo = weaponMagazineVolume;
            standardFOV = playerCamera.fieldOfView;

            ammo.text = $"{currentAmmo}/{magazineVolume}";
        }

        private void Update()
        {
            if (input.Reload)
            {
                ReloadWeapon();
            }

            if (!isReloading && !input.Run)
            {
                Shoot();
            }
        }
        #endregion

        #region Custom methods
        private IEnumerator MuzzleFlashLight(float time)
        {
            muzzleflashLight.enabled = enable;
            yield return new WaitForSeconds(time);
            muzzleflashLight.enabled = disable;
        }

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

        private void Shoot()
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

        private IEnumerator Reload(float reloadTime)
        {
            yield return new WaitForSeconds(reloadTime);

            isReloading = reloaded;

            currentAmmo = magazineVolume;
            ammo.text = $"{currentAmmo}/{magazineVolume}";
        }
        #endregion

        #region Inner classes
        [System.Serializable]
        private class AudioClips
        {
            [SerializeField] internal AudioClip aiming = null;
            [SerializeField] internal AudioClip reloadOutOfAmmo = null;
            [SerializeField] internal AudioClip reloadAmmoLeft = null;
            [SerializeField] internal AudioClip shoot = null;
        }
        #endregion
    }
}
