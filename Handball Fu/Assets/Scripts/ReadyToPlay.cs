using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReadyToPlay : MonoBehaviour
{
    public Button buttonStart;
    public string textStart = "Start";
    public string textCancel = "Cancel";
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
            buttonStart.GetComponentInChildren<TextMeshProUGUI>().text = textCancel;
        else
            buttonStart.GetComponentInChildren<TextMeshProUGUI>().text = textStart;
        client.SendReadyToPlay(ready);
    }
}
