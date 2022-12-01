using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReadyToPlay : MonoBehaviour
{
    public Button buttonStart;
    UDPClient client;
    private bool ready = false;
    // Start is called before the first frame update
    void Start()
    {
        client = FindObjectOfType<UDPClient>();
    }

    public void Ready()
    {
        ready = !ready;
        if(ready)
            buttonStart.GetComponentInChildren<TextMeshProUGUI>().text = "Cancel";
        else
            buttonStart.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
        client.SendReadyToPlay(ready);
    }
}
