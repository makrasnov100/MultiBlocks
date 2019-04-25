using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    public float playerSpeed;
    public float rotationSpeed;
    public float jumpPower;

    public Rigidbody rb;
    public GameObject body;

    void Update()
    {
        KeyboardInputCheck();
    }

    void KeyboardInputCheck()
    {
        float forceAmt = 0;
        Vector3 curForce = Vector3.zero;
        float rotationAmt = 0;

        //Check current input of WASD keys (if holding down)
        if (Input.GetKey(KeyCode.W))
            forceAmt += 1;
        if (Input.GetKey(KeyCode.A))
            rotationAmt -= 1f;
        if (Input.GetKey(KeyCode.S))
            forceAmt -= 1;
        if (Input.GetKey(KeyCode.D))
            rotationAmt += 1f;

        //Check for jump command
        if (Input.GetKeyDown(KeyCode.Space))
            if (rb)
                rb.AddForce(body.transform.up * rb.mass * jumpPower);


        curForce = body.transform.forward * forceAmt;

        if (rb)
        {
            Vector3 curVelocity = rb.velocity;
            if (forceAmt != 0)
            {
                curForce = curForce.normalized * (Time.deltaTime * 100) * playerSpeed;
                curVelocity.x = curForce.x;
                curVelocity.z = curForce.z;
                rb.velocity = curVelocity;
            }

            if (rotationAmt != 0)
            {
                Vector3 newRot = body.transform.eulerAngles;
                newRot.y += rotationAmt * Time.deltaTime * rotationSpeed;
                body.transform.eulerAngles = newRot;
            }
        }
    }
}
