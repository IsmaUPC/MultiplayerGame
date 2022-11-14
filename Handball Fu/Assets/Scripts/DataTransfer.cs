using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataTransfer : MonoBehaviour
{
    public List<GameObject[]> cosmetics = new List<GameObject[]>();
    public int[] indexs;
    private CustomAvatar ca;
    // Start is called before the first frame update
    void Start()
    {
        DontDestroyOnLoad(this);
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
