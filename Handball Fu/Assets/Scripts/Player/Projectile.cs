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

    // Start is called before the first frame update
    void Start()
    {

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
        Debug.Log(collision.gameObject.name);
        // Check no collision with his owner
        if (collision.gameObject != parent.gameObject)
        {
            currentBounce++;
            if (currentBounce >= maxBounce)
            {
                GetComponent<Rigidbody>().useGravity = true;
                WorldUpdateServer worldServer = FindObjectOfType<WorldUpdateServer>();
                if (worldServer)
                    worldServer.ActiveGravityPunch(gameObject);
            }

            if (collision.gameObject.tag == "Floor")
            {
                isRecover = true;
                GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
                GetComponent<SphereCollider>().isTrigger = true;
            }
            else
            {
                var contact = collision.contacts[0];
                var newDir = Vector3.zero;
                var curDir = transform.TransformDirection(Vector3.forward);
                newDir = Vector3.Reflect(curDir, contact.normal);
                transform.rotation = Quaternion.FromToRotation(Vector3.forward, newDir);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == parent.gameObject)
            DestroyProjectile();
    }

    private void DestroyProjectile()
    {
        parent.shader.UndoTransparent();
        parent.shoot = false;
        Destroy(gameObject);
    }
}
