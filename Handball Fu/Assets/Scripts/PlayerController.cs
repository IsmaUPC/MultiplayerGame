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

    // Shoot
    [HideInInspector] public GameObject projectile;
    public Transform projectilePos;

    // States
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
        switch (state)
        {
            case State.MOVE:
                if (movement.magnitude != 0)
                {
                    characterController.Move(new Vector3(movement.x * Time.deltaTime, 0, movement.y * Time.deltaTime));
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetDirection, rotVelocity * Time.deltaTime);
                }
                else if (stillTime < propHunt.timeToConvert)
                    stillTime += Time.deltaTime;
                else propHunt.ChangeMesh();
                break;

            case State.DASH:
                characterController.Move(transform.forward * dashSpeed * Time.deltaTime);
                break;

            case State.LOAD_ARM:
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetDirection, rotVelocity * Time.deltaTime);
                break;

            default:
                break;
        }
    }


    void OnMove(InputValue value)
    {
        Vector2 dir = value.Get<Vector2>();
        movement = dir * velocity;
        if(dir.magnitude != 0)
            targetDirection = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.y), Vector3.up);

        animator.SetFloat("Velocity", dir.magnitude);
        ResetPropHuntCount();

        if (state != State.MOVE) 
            return;
        state = State.MOVE;
    }

    void OnDash()
    {
        // If player is doing other action -> return
        if (state != State.MOVE) 
            return;

        animator.SetBool("Dash", true);
        ResetPropHuntCount();

        if (state != State.DASH)
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
        if (state != State.MOVE) 
            return;

        animator.SetBool("Attack", true);
        state = State.ATTACK;

        ResetPropHuntCount();
    }

    void OnShoot(InputValue value)
    {
        if (state != State.MOVE && value.Get<float>() == 1) 
            return;

        // Key DOWN
        if (value.Get<float>() == 1)
        {
            animator.SetBool("Shoot", true);
            state = State.LOAD_ARM;
        }
        // Key UP
        else if (state == State.LOAD_ARM)
        {
            animator.SetBool("Shoot", false);
            state = State.ATTACK;
        }

        ResetPropHuntCount();
    }

    public void SpawnProjectile()
    {
        Instantiate(projectile, projectilePos.position, transform.rotation);

        // TODO: Call shader mask to hide right punch
    }

    private void ResetPropHuntCount()
    {
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
