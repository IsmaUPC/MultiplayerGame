using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomAvatar : MonoBehaviour
{
    public GameObject[] bodies;
    public GameObject[] headParts;
    public GameObject[] eyes;
    public GameObject[] mouthandNoses;
    public GameObject[] bodyParts;
    public GameObject[] gloves;
    public GameObject[] tails;

    private List<GameObject[]> cosmetics = new List<GameObject[]>();
    private int[] indexs;
    private int bodyPart = 0;

    public float rotateSpeed = 10;
    private float dir = 0;
    // Start is called before the first frame update
    void Start()
    {
        cosmetics.Add(bodies);
        cosmetics.Add(headParts);
        cosmetics.Add(eyes);
        cosmetics.Add(mouthandNoses);
        cosmetics.Add(bodyParts);
        cosmetics.Add(gloves);
        cosmetics.Add(tails);

        indexs = new int[cosmetics.Count];
        for (int i = 0; i < indexs.Length; i++)
        {
            indexs[i] = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (dir != 0)
            transform.Rotate(Vector3.up, dir * rotateSpeed * Time.deltaTime);
    }

    void OnSelector(InputValue value)
    {
        Vector2 key = value.Get<Vector2>();
        if(key.magnitude != 0)
        {
            if (key.x == 1)
                ChangeMesh(true);
            else if (key.x == -1)
                ChangeMesh(false);

            if (key.y == 1)
            {
                bodyPart--;
                if (bodyPart < 0)
                    bodyPart = cosmetics.Count - 1;                
            }
            else if (key.y == -1)
            {
                bodyPart++;
                if (bodyPart >= cosmetics.Count)
                    bodyPart = 0;
            }
        }
        
    }
    private void ChangeMesh(bool nextPart)
    {
        cosmetics[bodyPart][indexs[bodyPart]].SetActive(false);
        if (nextPart) NextPart();
        else PrevPart();
        cosmetics[bodyPart][indexs[bodyPart]].SetActive(true);
    }

    private void NextPart()
    {
        indexs[bodyPart]++;
        if (indexs[bodyPart] >= cosmetics[bodyPart].Length)
            indexs[bodyPart] = 0;
    }
    private void PrevPart()
    {
        indexs[bodyPart]--;
        if (indexs[bodyPart] < 0)
            indexs[bodyPart] = cosmetics[bodyPart].Length - 1;
    }

    void OnRotate(InputValue value)
    {
        Vector2 key = value.Get<Vector2>();
        dir = -key.x;
    }
}
