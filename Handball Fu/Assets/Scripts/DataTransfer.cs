using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataTransfer : MonoBehaviour
{
    public Mesh[] bodies;
    public Mesh[] headParts;
    public Mesh[] eyes;
    public Mesh[] mouthandNoses;
    public Mesh[] bodyParts;
    public Mesh[] gloves;
    public Mesh[] tails;

    public Mesh[] projectiles;
    public GameObject projectilePrefab;

    public List<Mesh[]> cosmetics = new List<Mesh[]>();
    public int[] indexs;
    private CustomAvatar ca;

    public int portId = 0;
    // Start is called before the first frame update
    void Awake()
    {
        // Add all GameObject to list 
        cosmetics.Add(bodies);
        cosmetics.Add(headParts);
        cosmetics.Add(eyes);
        cosmetics.Add(mouthandNoses);
        cosmetics.Add(bodyParts);
        cosmetics.Add(gloves);
        cosmetics.Add(tails);

        // Preserve GO
        DontDestroyOnLoad(this);

        portId = FindObjectOfType<UDPClient>().GetPortIdx();
    }

    public void SetCustomAvatar(CustomAvatar custom)
    {
        ca = custom;
    }

    public void TransferData()
    {
        ca.UpdateAvatar();    
    }
}
