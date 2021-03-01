using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class APRefillArea : MonoBehaviour
{
    public float APRefillRate;

    void OnTriggerStay(Collider other)
    {
        CharacterStats character = other.GetComponent<CharacterStats>();
        if (character != null)
        {
            if (character.actionPoints < character.maxActionPoints)
            {
                character.actionPoints += APRefillRate * Time.deltaTime;
            }
        }
    }
}
