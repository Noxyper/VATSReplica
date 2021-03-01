using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

public class Bullet : MonoBehaviour
{
    public GameObject impactEffect;

    public float lifetime = 5f;
    [HideInInspector]
    public bool missed = false;
    [HideInInspector]
    public Vector3 startPos, targetPos;
    [HideInInspector]
    public RaycastHit hitMarker;

    void Update()
    {
        if(lifetime <= 0)
        {
            Destroy(gameObject);
        }
        lifetime -= Time.unscaledDeltaTime;

    }

    void OnDestroy()
    {
        GameObject impactGO = Instantiate(impactEffect, hitMarker.point, Quaternion.LookRotation(hitMarker.normal));
        impactGO.GetComponent<VisualEffect>().playRate = Mathf.Clamp(1f / Time.timeScale, 0f, 50f);
        Destroy(impactGO, 5f);
    }
}
