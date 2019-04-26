using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovementController : MonoBehaviour
{
    //Movement Tracking
    Vector3 pastPos;
    float pastRot;

    //Movement
    public float playerSpeed;
    public float rotationSpeed;
    public float jumpPower;
    //Rotation
    public float horizontalRotateSpeed;
    public float verticalRotateSpeed;
    //Zoom
    public float zoomSpeed;

    //References
    public Rigidbody rb;
    public GameObject body;
    public GameObject camPivot;
    public Client client;

    private void Start()
    {
        pastPos = transform.position;
        pastRot = transform.eulerAngles.y;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        bool isMoved = KeyboardInputCheck();
        MouseInputCheck();

        //Send movement update if player moved
        if (isMoved)
        {
            Vector3 newPos = gameObject.transform.position;
            float newRot = body.transform.eulerAngles.y;
            client.Send("PlayerMove|" + newPos.x + "," + newPos.y + "," + newPos.z + "," + newRot, client.GetUnreliableChannel());
        }

    }

    bool KeyboardInputCheck()
    {

        //Check for jump command
        if (Input.GetKeyDown(KeyCode.Space))
            if (rb)
                rb.AddForce(body.transform.up * rb.mass * jumpPower);

        //Check for ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.None)
                Cursor.lockState = CursorLockMode.Locked;
            else
                Cursor.lockState = CursorLockMode.None;

        }


        //Check current input of WASD keys (if holding down)
        float forceAmt = 0;
        Vector3 curForce = Vector3.zero;
        float rotationAmt = 0;

        if (Input.GetKey(KeyCode.W))
            forceAmt += 1;
        if (Input.GetKey(KeyCode.A))
            rotationAmt -= 1f;
        if (Input.GetKey(KeyCode.S))
            forceAmt -= 1;
        if (Input.GetKey(KeyCode.D))
            rotationAmt += 1f;

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

        //Check if player has moved or rotated
        bool isMoved = false;
        if (Vector3.Distance(pastPos, transform.position) > .001 || pastRot != body.transform.eulerAngles.y)
        {
            pastPos = transform.position;
            pastRot = body.transform.eulerAngles.y;
            isMoved = true;
        }

        return isMoved;
    }

    void MouseInputCheck()
    {
        //Rotation
        if (Input.GetAxis("Mouse X") != 0 || Input.GetAxis("Mouse Y") != 0)
        {
            float camPivotHor = camPivot.transform.eulerAngles.y;
            float camPivotVer = camPivot.transform.eulerAngles.x;
            camPivotHor = camPivotHor + Input.GetAxis("Mouse X") * horizontalRotateSpeed;
            camPivotVer = camPivotVer + (-Input.GetAxis("Mouse Y") * verticalRotateSpeed);
            camPivotVer = camPivotVer % 360;
            if (camPivotVer > 200)
                camPivotVer = -(360 - camPivotVer);
            camPivot.transform.eulerAngles = new Vector3(Mathf.Clamp(camPivotVer, -80f, 80f), camPivotHor, 0);
        }
       
        //Zoom
        if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            float pivotScale = camPivot.transform.localScale.y + (-Input.GetAxis("Mouse ScrollWheel") * zoomSpeed);
            pivotScale = Mathf.Min(5f, Mathf.Max(1f, pivotScale));
            camPivot.transform.localScale = new Vector3(pivotScale, pivotScale, pivotScale);
        }
    }
}
