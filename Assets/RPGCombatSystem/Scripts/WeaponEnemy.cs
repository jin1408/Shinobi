using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponEnemy : MonoBehaviour
{
    public int dmgValue = 132; //Damage of the weapon
    public Color dmgColor = Color.cyan; //Color of the text with the damage value

    private BoxCollider coll; //Collider of the weapon

    void Awake()
    {
        coll = GetComponent<BoxCollider>();
    }

	private void OnTriggerEnter(Collider other)
	{
        if (other.tag == "Player")
        {
            // 넉백 처리
            other.GetComponent<PlayerController>().ApplyDMG(other.transform.position - transform.position, 250f);

            // HP 데미지 처리
            PlayerStats playerStats = other.GetComponent<PlayerStats>();
            if (playerStats != null)
                playerStats.TakeDamage(dmgValue);
        }
	}

    public void EnableColliders() //Called from the AnimatorEvent script
    {
        coll.enabled = true;
    }

    public void DisableColliders() //Called from the AnimatorEvent script
    {
        coll.enabled = false;
    }
}
