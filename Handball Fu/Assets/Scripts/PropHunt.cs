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
    private Mesh originalMesh;
    private Material originalMaterial;

    public float timeToConvert = 2;
    public bool iAmTransformed = false;

    void Start()
    {
        originalMesh = meshFilter.sharedMesh;
        originalMaterial = meshFilter.sharedMaterial;
        bodyParts = GetComponent<PlayerData>().bodyParts;
    }

    // Change aspect to random object
    public void ChangeMesh()
    {
        if(!iAmTransformed)
        {
            int index = Random.Range(0, newModels.Length);
            meshFilter.sharedMesh = newModels[index];
            meshFilter.sharedMaterial = newMaterials[index];

            // Hide cuurent meshes
            for (int i = 0; i < bodyParts.Length; i++)
            {
                bodyParts[i].SetActive(false);
            }
        }
        iAmTransformed = true;
    }

    // Return to original aspect
    public void ResetMesh()
    {
        if (iAmTransformed)
        {
            meshFilter.sharedMesh = originalMesh;
            meshFilter.sharedMaterial = originalMaterial;

            // Show original meshes
            for (int i = 0; i < bodyParts.Length; i++)
            {
                bodyParts[i].SetActive(true);
            }
        }
        iAmTransformed = false;
    }
}
