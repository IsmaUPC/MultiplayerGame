using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : MonoBehaviour
{
    [SerializeField] public int playerID;
    [SerializeField] public Vector3 startPos;
    // Start is called before the first frame update
    void Start()
    {
        transform.position = startPos;
    }
}
