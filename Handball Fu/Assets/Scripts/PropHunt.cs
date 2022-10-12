using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PropHunt : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] private SkinnedMeshRenderer meshFilter;
    [SerializeField] private Mesh[] newModels;
    [SerializeField] private Material[] newMaterials;
    private GameObject[] bodyParts;

    public float timeToConvert = 2;
    private Mesh originalMesh;
    private Material originalMaterial;
    public bool iAmTransformed = false;

    //[SerializeField] private PlayerController controller;
    void Start()
    {
        originalMesh = meshFilter.sharedMesh;
        originalMaterial = meshFilter.sharedMaterial;
        bodyParts = GetComponent<PlayerData>().bodyParts;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ChangeMesh()
    {
        if(!iAmTransformed)
        {
            int index = Random.Range(0, newModels.Length);
            meshFilter.sharedMesh = newModels[index];
            meshFilter.sharedMaterial = newMaterials[index];

            for (int i = 0; i < bodyParts.Length; i++)
            {
                bodyParts[i].SetActive(false);
            }
        }
        iAmTransformed = true;
    }
    public void ResetMesh()
    {
        if (iAmTransformed)
        {
            meshFilter.sharedMesh = originalMesh;
            meshFilter.sharedMaterial = originalMaterial;

            for (int i = 0; i < bodyParts.Length; i++)
            {
                bodyParts[i].SetActive(true);
            }
        }
        iAmTransformed = false;
    }
}
