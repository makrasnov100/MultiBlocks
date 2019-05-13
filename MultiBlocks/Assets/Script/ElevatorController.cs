using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{


    private void OnTriggerEnter(Collider other)
    {
        //Stop Player from moving
        Vector3 vel = other.gameObject.GetComponentInParent<Rigidbody>().velocity;
        vel.x = 0;
        vel.z = 0;
        other.gameObject.GetComponentInParent<Rigidbody>().velocity = vel;

        //TODO Interpolate the player to the middle rather than centering
        Vector3 curPos = other.gameObject.transform.parent.transform.position;
        curPos.x = gameObject.transform.position.x;
        curPos.z = gameObject.transform.position.z;
        other.gameObject.transform.parent.transform.position = curPos;

    }

    private void OnTriggerStay(Collider other)
    {

        if (other.gameObject.GetComponentInParent<Rigidbody>().velocity.y < 15)
            other.gameObject.GetComponentInParent<Rigidbody>().AddForce(Vector3.up * 500);
    }


    private void OnTriggerExit(Collider other)
    {
        Vector3 curVel = other.gameObject.GetComponentInParent<Rigidbody>().velocity;
        curVel.y = 10;
        curVel.x = Random.Range(1, 2);
        curVel.z = Random.Range(1, 2);
        other.gameObject.GetComponentInParent<Rigidbody>().velocity = curVel;
    }
}
