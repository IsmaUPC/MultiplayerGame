using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // General
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Animator animator;

    // Movement
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

    enum State { AWAKE, MOVE, DASH, ATTACK, LOAD_ARM, DIE };
    private State state = State.AWAKE;

    // Start is called before the first frame update
    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        state = State.MOVE;
    }

    // Update is called once per frame
    void Update()
    {
        if (state == State.MOVE)
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
        else if (state == State.LOAD_ARM)
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetDirection, rotVelocity * Time.deltaTime);
        else if (state == State.DASH)
            characterController.Move(transform.forward * dashSpeed * Time.deltaTime);

    }


    void OnMove(InputValue value)
    {
        Vector2 dir = value.Get<Vector2>();
        movement = dir * velocity;
        if(dir.magnitude != 0)
            targetDirection = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y), Vector3.up);

        animator.SetFloat("Velocity", dir.magnitude);
        stillTime = 0;
        propHunt.ResetMesh();

        if (state != State.MOVE) return;
        state = State.MOVE;
    }

    void OnDash()
    {
        if (state != State.MOVE) return;

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
        if (state != State.MOVE) return;

        animator.SetBool("Attack", true);
        state = State.ATTACK;

        stillTime = 0;
        propHunt.ResetMesh();
    }

    void OnShoot(InputValue value)
    {
        if (state != State.MOVE && value.Get<float>() == 1) return;

        if (value.Get<float>() == 1)
        {
            animator.SetBool("Shoot", true);
            state = State.LOAD_ARM;
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
        state = State.DIE;
    }

    void Victory()
    {
        animator.SetBool("Victory", true);
        state = State.DIE;
    }

}
