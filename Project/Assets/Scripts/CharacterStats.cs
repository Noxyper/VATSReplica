using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    public float maxHealthPoints;
    public float maxActionPoints;
    public new string name;

    [HideInInspector]
    public float healthPoints;
    [HideInInspector]
    public float actionPoints;

    void Awake()
    {
        healthPoints = maxHealthPoints;
        actionPoints = maxActionPoints;
    }

    void Update()
    {
        if(actionPoints < maxActionPoints)
        {
            actionPoints += 1f * Time.deltaTime;
        }
    }

    public void TakeDamage(float damage)
    {
        healthPoints -= damage;
        if(healthPoints <= 0)
        {
            StartCoroutine(Die());
        }
    }

    IEnumerator Die()
    {
        if(GetComponent<EnemyMove>())
        {
            EnemyMove enemy = GetComponent<EnemyMove>();
            for (int i = 0; i < enemy.GetComponentsInChildren<Rigidbody>().Length; i++)
            {
                enemy.GetComponentsInChildren<Rigidbody>()[i].isKinematic = false;
            }
            enemy.enemyAnimator.enabled = false;
            yield return new WaitForEndOfFrame();
            enemy.isDead = true;
        }
        if (GetComponent<PlayerMove>())
        {
            PlayerMove player = GetComponent<PlayerMove>();
            for (int i = 0; i < player.GetComponentsInChildren<Rigidbody>().Length; i++)
            {
                player.GetComponentsInChildren<Rigidbody>()[i].isKinematic = false;
            }
            yield return new WaitForEndOfFrame();
            player.playerAnimator.enabled = false;
        }
    }
}
