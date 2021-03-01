using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerProxyAnimation : MonoBehaviour
{
    Animator _animator;
    public Transform[] limbs;
    public VATS VATS;

    void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    void OnAnimatorIK()
    {
        Vector3 lookPosition = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / 2, Screen.height / 2, 100f));

        _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
        _animator.SetIKRotation(AvatarIKGoal.RightHand, Quaternion.LookRotation(lookPosition, limbs[0].up));

        _animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
        _animator.SetIKRotation(AvatarIKGoal.LeftHand, Quaternion.LookRotation(lookPosition, limbs[1].up));

        _animator.SetLookAtWeight(1);
        _animator.SetLookAtPosition(lookPosition);
    }
}
