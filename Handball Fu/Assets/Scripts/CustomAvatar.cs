using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomAvatar : MonoBehaviour
{
    private DataTransfer data;
    private List<Mesh[]> cosmetics = new List<Mesh[]>();
    private GameObject[] bodyParts;
    private int[] indexs;
    private int bodyPartIndex = 0;

    public float rotateSpeed = 10;
    private float dir = 0;


    Vector3 posAux;
    public GameObject[] selectors;

    // Start is called before the first frame update
    void Start()
    {

        // Get data GO for get cosmetic meshes
        data = GameObject.FindGameObjectWithTag("Data").GetComponent<DataTransfer>();
        data.SetCustomAvatar(this);
        cosmetics = data.cosmetics;

        // Fill index[] to 0
        indexs = new int[cosmetics.Count];
        indexs = data.indexs;

        // Fill body parts
        bodyParts = new GameObject[indexs.Length];
        bodyParts = GetComponent<PlayerData>().bodyParts;

        selectors[bodyPartIndex].gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate character
        if (dir != 0)
            transform.Rotate(Vector3.up, dir * rotateSpeed * Time.deltaTime);

        if(!selectors[bodyPartIndex].gameObject.active) selectors[bodyPartIndex].gameObject.SetActive(true);
    }
    public void UpdateAvatar()
    {
        // Store index, with this numbers we are able to generate anything player (seed-like)
        data.indexs = indexs;
    }

    void OnSelector(InputValue value)
    {
        selectors[bodyPartIndex].gameObject.SetActive(false);
        // UI control
        Vector2 key = value.Get<Vector2>();
        if(key.magnitude != 0)
        {
            // Next cosmetic
            if (key.x == 1)
                ChangeMesh(true);
            else if (key.x == -1)
                ChangeMesh(false);

            // Next body part
            if (key.y == 1)
            {
                bodyPartIndex--;
                if (bodyPartIndex < 0)
                    bodyPartIndex = cosmetics.Count - 1;                
            }
            else if (key.y == -1)
            {
                bodyPartIndex++;
                if (bodyPartIndex >= cosmetics.Count)
                    bodyPartIndex = 0;
            }
        }

    }
    private void ChangeMesh(bool nextPart)
    {
        if (nextPart) NextPart();
        else PrevPart();

        // Change mesh, more efficient than have multiple-object and active/desactive them
        bodyParts[bodyPartIndex].GetComponent<SkinnedMeshRenderer>().sharedMesh = cosmetics[bodyPartIndex][indexs[bodyPartIndex]];
    }

    private void NextPart()
    {
        indexs[bodyPartIndex]++;
        if (indexs[bodyPartIndex] >= cosmetics[bodyPartIndex].Length)
            indexs[bodyPartIndex] = 0;
    }
    private void PrevPart()
    {
        indexs[bodyPartIndex]--;
        if (indexs[bodyPartIndex] < 0)
            indexs[bodyPartIndex] = cosmetics[bodyPartIndex].Length - 1;
    }

    void OnRotate(InputValue value)
    {
        Vector2 key = value.Get<Vector2>();
        dir = -key.x;
    }
}
