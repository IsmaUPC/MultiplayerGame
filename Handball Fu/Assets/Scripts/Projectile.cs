using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float velocity = 10;
    public int maxBounce = 4;
    public float timeToLive = 4;
    private int currentBounce = 0;
    [HideInInspector] public PlayerController parent;
    private float initY = 0;

    // Start is called before the first frame update
    void Start()
    {
        initY = transform.position.y;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += transform.forward * velocity * Time.deltaTime;
        transform.position = new Vector3(transform.position.x, initY, transform.position.z);
        timeToLive -= Time.deltaTime;
        if (timeToLive < 0.0F)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check no collision with his owner
        if (collision.gameObject != parent.gameObject)
        {
            currentBounce++;
            if (currentBounce >= maxBounce)
                GetComponent<Rigidbody>().useGravity = true;

            if (collision.gameObject.tag == "Floor")
            {
                parent.shader.UndoTransparent();
                Destroy(transform.gameObject);
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
}
