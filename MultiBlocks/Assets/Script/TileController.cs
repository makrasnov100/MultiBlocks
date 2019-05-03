using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public Animator anim;

    float timeToDestroy; //should always be >1f seconds

    public void Despawn(float timeToDestroy)
    {
        anim = GetComponent<Animator>();

        if (anim)
        {
            this.timeToDestroy = timeToDestroy;

            //Start despawn animation
            anim.SetBool("IsDead", true);

            StartCoroutine(DestroyTile());
        }
    }


    IEnumerator DestroyTile()
    {
        //Set animation speed based on time to destroy

        //Begin despawn color animation

        yield return new WaitForSeconds(timeToDestroy - 1f);

        //Begin despawn drop animation

        yield return new WaitForSeconds(1f); //TODO: make sure animation is exavtly 1 second

        Destroy(gameObject);
    }
}
