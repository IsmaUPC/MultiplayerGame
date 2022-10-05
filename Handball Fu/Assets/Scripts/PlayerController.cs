using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private CharacterController characterController;
    [SerializeField] private PropHunt propHunt;
    public float velocity = 5;
    private Vector2 movement = Vector2.zero;
    private float stillTime = 0;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(movement.magnitude != 0)
            characterController.Move(new Vector3(movement.x * Time.deltaTime, 0, movement.y * Time.deltaTime));
        else if(stillTime < propHunt.timeToConvert)
            stillTime += Time.deltaTime;
        else propHunt.ChangeMesh();
    }

    void OnMove(InputValue dir)
    {
        movement = dir.Get<Vector2>() * velocity;
        Debug.Log("Movement");
        stillTime = 0;
        propHunt.ResetMesh();
    }

    void OnDash()
    {
        Debug.Log("Dash");
        stillTime = 0;
        propHunt.ResetMesh();
    }

    void OnCut()
    {
        Debug.Log("Cut");
        stillTime = 0;
        propHunt.ResetMesh();
    }

    void OnShoot()
    {
        Debug.Log("Shoot");
        stillTime = 0;
        propHunt.ResetMesh();
    }
}
