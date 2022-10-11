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
    private TrailRenderer trail;
    private State state = State.MOVE;

    enum State { MOVE, DASH, LOCK };

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if(state != State.LOCK)
        {
            if (state != State.DASH)
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
    }

    void OnMove(InputValue value)
    {
        Vector2 dir = value.Get<Vector2>();
        targetDirection = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y), Vector3.up);
        movement = dir * velocity;

        animator.SetFloat("Velocity", dir.magnitude);
        stillTime = 0;
        propHunt.ResetMesh();

        if (state == State.LOCK) return;
        state = State.MOVE;
    }

    void OnDash()
    {
        if (state == State.LOCK) return;

        animator.SetBool("Dash", true);

        stillTime = 0;
        propHunt.ResetMesh();

        if(state != State.DASH)
            StartCoroutine(Dash());
    }

    private IEnumerator Dash()
    {
        state = State.DASH;
        trail.emitting = true;
        yield return new WaitForSeconds(dashTime);
        state = State.MOVE;
        trail.emitting = false;
        animator.SetBool("Dash", false);
    }

    void OnCut()
    {
        if (state == State.LOCK) return;

        animator.SetBool("Attack", true);
        state = State.LOCK;

        stillTime = 0;
        propHunt.ResetMesh();
    }

    void OnShoot(InputValue value)
    {
        if (state == State.LOCK && value.Get<float>() == 1) return;

        if (value.Get<float>() == 1)
        {
            animator.SetBool("Shoot", true);
            state = State.LOCK;
        }
        else
            animator.SetBool("Shoot", false);       

        stillTime = 0;
        propHunt.ResetMesh();
    }

    void FinishAttack()
    {
        animator.SetBool("Attack", false);
        state = State.MOVE;
    }

    void Die()
    {
        animator.SetBool("Die", true);
        state = State.LOCK;
    }

    void Victory()
    {
        animator.SetBool("Victory", true);
        state = State.LOCK;
    }

}
