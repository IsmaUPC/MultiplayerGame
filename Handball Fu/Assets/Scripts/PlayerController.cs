using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    private Vector2 movement = Vector2.zero;
    private Quaternion targetDirection;
    public float velocity = 5;
    public float rotVelocity = 5;

    // Prop Hunt
    [SerializeField] private PropHunt propHunt;
    private float stillTime = 0;

    // Dash
    public float dashTime = 0.2f;
    public float dashSpeed = 20;
    private bool isDashing = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(!isDashing)
        {
            if (movement.magnitude != 0)
            {
                characterController.Move(new Vector3(movement.x * Time.deltaTime, 0, movement.y * Time.deltaTime));
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetDirection, rotVelocity * Time.deltaTime);
            }
            else if (stillTime < propHunt.timeToConvert)
                stillTime += Time.deltaTime;
            else propHunt.ChangeMesh();
        }
        else
            characterController.Move(transform.forward * dashSpeed * Time.deltaTime);
    }

    void OnMove(InputValue value)
    {
        Vector2 dir = value.Get<Vector2>();
        targetDirection = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y), Vector3.up);
        movement = dir * velocity;

        animator.SetFloat("Velocity", dir.magnitude);
        stillTime = 0;
        propHunt.ResetMesh();
    }

    void OnDash()
    {
        animator.SetBool("Dash", true);

        stillTime = 0;
        propHunt.ResetMesh();

        StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        animator.SetBool("Dash", false);
    }

    void OnCut()
    {
        animator.SetBool("Attack", true);

        stillTime = 0;
        propHunt.ResetMesh();
    }

    void OnShoot()
    {
        animator.SetBool("Attack", true);

        stillTime = 0;
        propHunt.ResetMesh();
    }

    void FinishAttack()
    {
        animator.SetBool("Attack", false);
    }

}
