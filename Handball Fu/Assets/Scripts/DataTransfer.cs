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
        if(FindObjectsOfType<DataTransfer>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
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

        UDPClient tmp = FindObjectOfType<UDPClient>();
        if (tmp != null)
        {
            portId = tmp.GetPortIdx();
        }
    }

    void OnEnable()
    {
        UDPClient.OnStart += TransferData;
    }
    void OnDisable()
    {
        UDPClient.OnStart -= TransferData;
    }

    public void SetCustomAvatar(CustomAvatar custom)
    {
        ca = custom;
    }

    public void TransferData(int s)
    {
        if(ca != null)
            ca.UpdateAvatar();    
    }
}
