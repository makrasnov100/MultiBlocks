using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileController : MonoBehaviour
{
    public MeshRenderer meshRender;

    public void Despawn(float timeToDestroy)
    {
        StartCoroutine(BlockDestruction(timeToDestroy));
    }

    public IEnumerator BlockDestruction(float timeToDestroy)
    {
        float delay = Random.Range(0f, 1f);
        yield return new WaitForSeconds(delay);

        float sectionTiming = (timeToDestroy - delay) / 3;

        //Turn block yellow
        meshRender.material.SetColor("_Color", Color.yellow);
        yield return new WaitForSeconds(sectionTiming);

        //Turn block orange
        meshRender.material.SetColor("_Color", new Color(1, .5f, 0, 0));
        yield return new WaitForSeconds(sectionTiming);

        //Turn block red
        meshRender.material.SetColor("_Color", Color.red);
        yield return new WaitForSeconds(sectionTiming);

        //Destroy block
        meshRender.material.SetColor("_Color", Color.magenta);
        MapController.Instance.DespawnIntoTilePool(gameObject);

    }
}
