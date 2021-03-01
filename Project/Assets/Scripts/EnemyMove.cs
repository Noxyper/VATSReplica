using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Limbs
{
    public GameObject joints;
    public GameObject muscles;
    public Collider[] bones;
    public Transform indicatorPosition;

    [HideInInspector]
    public float hitChance;
    [HideInInspector]
    public bool limbIndicatorExists;
}

[RequireComponent(typeof(CharacterStats))]
public class EnemyMove : MonoBehaviour
{
    Vector3 _startPosition;

    int _checkpointIndex;
    float _animTransSpeed = 5f;

    [HideInInspector]
    public CharacterController controller;
    [HideInInspector]
    public Stack<RectTransform>[] indicatorSelectors;
    public Limbs[] limbs;
    public Vector3[] checkpoints;
    public Animator enemyAnimator;

    [HideInInspector]
    public bool isDead = false;
    public float enemySpeed = 5f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        _startPosition = transform.position;
        _checkpointIndex = 0;
    }

    void Update()
    {
        if (!isDead)
        {
            Vector3 tempMovementDirection = (checkpoints[_checkpointIndex] - transform.position).normalized;

            controller.SimpleMove((tempMovementDirection) * enemySpeed);

            transform.rotation = Quaternion.LookRotation(Vector3.RotateTowards(transform.forward, tempMovementDirection, enemySpeed * Time.deltaTime, 0.0f));

            if ((checkpoints[_checkpointIndex] - transform.position).magnitude < 1f)
            {
                _checkpointIndex++;
                if (_checkpointIndex >= checkpoints.Length)
                {
                    controller.enabled = false;
                    transform.position = _startPosition;
                    _checkpointIndex = 0;
                }
            }

            enemyAnimator.SetFloat("MoveX", Mathf.Lerp(enemyAnimator.GetFloat("MoveX"), -transform.TransformDirection(tempMovementDirection).x, Time.deltaTime * _animTransSpeed));
            enemyAnimator.SetFloat("MoveY", Mathf.Lerp(enemyAnimator.GetFloat("MoveY"), -transform.TransformDirection(tempMovementDirection).z, Time.deltaTime * _animTransSpeed));
            enemyAnimator.SetBool("Moving", (tempMovementDirection.magnitude * enemySpeed) != 0);
        }
        else
        {
            VATS.SetLayer(gameObject, LayerMask.NameToLayer("Default"), true);
        }
    }

    void LateUpdate()
    {
        if (!isDead)
        {
            controller.enabled = true;
        }
    }

    void FixedUpdate()
    {
        if (isDead)
        {
            for (int i = 0; i < GetComponentsInChildren<Rigidbody>().Length; i++)
            {
                GetComponentsInChildren<Rigidbody>()[i].velocity = Physics.gravity;
            }
        }
    }
}
