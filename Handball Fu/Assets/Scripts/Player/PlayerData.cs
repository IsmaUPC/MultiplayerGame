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

    public void SetBodyParts(List<Mesh[]> cosmetics, Mesh projectileMesh, int[] indexs)
    {
        for (int i = 0; i < indexs.Length; i++)
        {
            bodyParts[i].GetComponent<SkinnedMeshRenderer>().sharedMesh = cosmetics[i][indexs[i]];
        }
        if (GetComponent<CustomAvatar>() == null)
        {
            GetComponent<PropHunt>().SetBodyParts(bodyParts);
            GetComponent<PlayerController>().projectileMesh = projectileMesh;
        }
    }
}
