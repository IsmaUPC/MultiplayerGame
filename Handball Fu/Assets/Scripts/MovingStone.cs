using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingStone : MonoBehaviour
{
    public int currentPos = 0;
    public List<Vector3> positions = new List<Vector3>();
    private int nextPos = 1;
    public float speed = 2f;
    private float elapsedTime = 0;
    public float timeBetween = 8f;

    // Start is called before the first frame update
    void Start()
    {
        nextPos = currentPos + 1;
        if (nextPos >= positions.Count) nextPos -= positions.Count;
    }

    // Update is called once per frame
    void Update()
    {
        speed += (speed) * Time.deltaTime;
        Debug.Log(speed);
        Move();
        elapsedTime += Time.deltaTime;
    }

    void Move()
    {
        transform.position = Vector3.MoveTowards(transform.position, positions[nextPos], speed * Time.deltaTime);
        if (transform.position == positions[nextPos] && elapsedTime >= timeBetween)
        {
            nextPos++;
            if (nextPos >= positions.Count) nextPos -= positions.Count;
            speed = 0.1f;
            elapsedTime = 0;
        }
    }
}
