using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float velocity = 10;
    public int maxBounce = 4;
    private int currentBounce = 0;
    private bool isRecover = false;
    [HideInInspector] public float initY = 0;
    [HideInInspector] public PlayerController parent;
    private WorldUpdateServer worldServer;

    AudioManager audioMan;
    GameObject FXSounds;

    // Start is called before the first frame update
    void Start()
    {
        FXSounds = GameObject.FindGameObjectWithTag("FX");

        audioMan = FXSounds.GetComponent<AudioManager>();

        worldServer = FindObjectOfType<WorldUpdateServer>();
        Physics.IgnoreCollision(parent.GetComponent<Collider>(), GetComponent<Collider>());
    }

    // Update is called once per frame
    public void UpdateTransform()
    {
        if(!isRecover)
            transform.position += transform.forward * velocity * Time.deltaTime;
        if(transform.position.y > initY)
            transform.position = new Vector3(transform.position.x, initY, transform.position.z);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check no collision with his owner
        if (collision.gameObject != parent.gameObject)
        {
            audioMan.PlayFXBoingPunch();
            if (collision.gameObject.GetComponent<PlayerController>())
            {
                currentBounce = maxBounce;
                if (worldServer != null)
                    worldServer.PlayerDied(collision.gameObject, parent.gameObject);
            }

            currentBounce++;
            if (currentBounce >= maxBounce)
            {
                if (worldServer != null)
                {
                    GetComponent<Rigidbody>().useGravity = true;
                    worldServer.ActiveGravityPunch(gameObject);
                }                
            }

            if (collision.gameObject.tag == "Floor")
            {
                isRecover = true;
                GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                GetComponent<SphereCollider>().isTrigger = true;
                Physics.IgnoreCollision(parent.GetComponent<Collider>(), GetComponent<Collider>(), false);
            }
            else
            {
                var contact = collision.contacts[0];
                var newDir = Vector3.zero;
                var curDir = transform.TransformDirection(Vector3.forward);
                newDir = Vector3.Reflect(curDir, contact.normal);
                newDir.y = 0;
                transform.rotation = Quaternion.FromToRotation(Vector3.forward, newDir);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (worldServer && other.gameObject == parent.gameObject)
            DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        ReStartShoot();
        worldServer.DestroyObjectNotify(gameObject);
    }
    public void ReStartShoot()
    {
        parent.shader.UndoTransparent();
        parent.shoot = false;
    }
}
