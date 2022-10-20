using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public int playerID;
    public GameObject[] bodyParts;
    private Transform startTrasnform;

    // Start is called before the first frame update
    void Start()
    {
        transform.position = startTrasnform.position;
        transform.rotation = startTrasnform.rotation;
    }

    public void SetStartTransform(Transform trans)
    {
        startTrasnform = trans;
    }
}
