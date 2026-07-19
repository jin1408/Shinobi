using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorEventsEn : MonoBehaviour
{
    //private Animator anim;
    private SoundManager soundMan;

    public WeaponEnemy weapon; 

    public bool isAttacking = false; //To prevent colliders from being activated if there is delay in the animation

    void Start()
    {
        //anim = GetComponent<Animator>();
        soundMan = GetComponentInParent<SoundManager>();
    }

    public void EnableMove()
    {
        //playerCont.EnableMove(true);
    }
    public void DisableMove()
    {
        if (soundMan != null) soundMan.PlaySound("Attack");
        //playerCont.EnableMove(false);
    }

    public void EnableWeaponColl()
    {
        if(isAttacking && weapon != null)
            weapon.EnableColliders();
    }
    public void DisableWeaponColl()
    {
        if (weapon != null) weapon.DisableColliders();
    }

    public void PlaySound(string soundName)
    {
        if (soundMan != null) soundMan.PlaySound(soundName);
    }
}
