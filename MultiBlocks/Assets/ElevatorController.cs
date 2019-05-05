using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElevatorController : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponentInParent<Rigidbody>().velocity.y < 10)
            other.gameObject.GetComponentInParent<Rigidbody>().AddForce(Vector3.up * 500);
    }
}
