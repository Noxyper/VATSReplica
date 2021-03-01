using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class PlayerShoot : MonoBehaviour
{
    public GameObject impactEffect;
    public VisualEffect muzzleFlash;
    public GameObject bulletPrefab;
    public Transform weaponProjectilePos;

    [HideInInspector]
    public bool mouseRestriction = false;
    public float weaponRange = 100f;
    public float maxWeaponCooldownTime = 0.25f;
    public float missVariation = 1f;
    
    float weaponCooldownTime = 0f;

    void Update()
    {
        if (!mouseRestriction && (weaponCooldownTime >= maxWeaponCooldownTime || Input.GetButtonDown("Fire")))
        {
            if (Input.GetButton("Fire"))
            {
                Fire();
            }
            weaponCooldownTime = 0;
        }
        else
        {
            weaponCooldownTime += Time.deltaTime;
        }

        muzzleFlash.playRate = Mathf.Clamp(1f / Time.timeScale, 0f, 50f);
    }

    public GameObject Fire(Transform target = null, bool missed = false)
    {
        muzzleFlash.Play();

        RaycastHit hit;
        if (target != null)
        {
            Vector3 direction = target.position - weaponProjectilePos.position;
            if (missed) direction += (weaponProjectilePos.right * Random.Range(-missVariation, missVariation) + weaponProjectilePos.up * Random.Range(-missVariation, missVariation));
            if (Physics.Raycast(weaponProjectilePos.position, direction, out hit, weaponRange, ~LayerMask.GetMask("PlayerController")))
            {
                GameObject bullet = Instantiate(bulletPrefab, weaponProjectilePos.position, Quaternion.LookRotation(hit.point - weaponProjectilePos.position));
                bullet.GetComponent<Bullet>().hitMarker = hit;
                return bullet;
            }
        }
        else
        {
            if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hit, weaponRange, ~LayerMask.GetMask("PlayerController")))
            {

                CharacterStats enemy = hit.transform.GetComponent<CharacterStats>();
                if (enemy != null)
                {
                    enemy.TakeDamage(10f);
                }

                GameObject impactGO = Instantiate(impactEffect, hit.point, Quaternion.LookRotation(hit.normal));
                impactGO.GetComponent<VisualEffect>().playRate = Mathf.Clamp(1f / Time.timeScale, 0f, 50f);
                Destroy(impactGO, 5f);
            }
        }
        return null;
    }
}
