using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PropHunt : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private Mesh[] newModels;
    [SerializeField] private SkinnedMeshRenderer meshFilter;
    public float timeToConvert = 2;
    private Mesh originalMesh;

    public bool iAmTransformed = false;
    //[SerializeField] private PlayerController controller;
    void Start()
    {
        originalMesh = meshFilter.sharedMesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeMesh()
    {
        if(!iAmTransformed)
            meshFilter.sharedMesh = newModels[Random.Range(0, newModels.Length)];
        iAmTransformed = true;
    }
    public void ResetMesh()
    {
        if (iAmTransformed)
            meshFilter.sharedMesh = originalMesh;
        iAmTransformed = false;
    }
}
