using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PropHunt : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Mesh[] newModels;
    private Mesh originalMesh;

    public bool changeMesh = false;
    //[SerializeField] private PlayerController controller;
    void Start()
    {
        originalMesh = meshFilter.mesh;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnPropHunt(InputValue value)
    {
        meshFilter.mesh = newModels[Random.Range(0, newModels.Length)];
        //meshFilter.mesh = originalMesh;
    }
}
