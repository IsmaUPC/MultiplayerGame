using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PropHunt : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Mesh[] newModels;
    public float timeToConvert = 2;
    private MeshFilter meshFilter;
    private Mesh originalMesh;

    public bool iAmTransformed = false;
    //[SerializeField] private PlayerController controller;
    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        originalMesh = meshFilter.mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeMesh()
    {
        if(!iAmTransformed)
            meshFilter.mesh = newModels[Random.Range(0, newModels.Length)];
        iAmTransformed = true;
    }
    public void ResetMesh()
    {
        if (iAmTransformed)
            meshFilter.mesh = originalMesh;
        iAmTransformed = false;
    }
}
