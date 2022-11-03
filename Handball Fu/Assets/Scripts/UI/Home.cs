using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Home : MonoBehaviour
{
    public void OnStartClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
    public void OnBackClick()
    {
        // Provisional
        GameObject[] go = GameObject.FindGameObjectsWithTag("NetWork");
        UDPClient udp = go[0].GetComponent<UDPClient>();
        if (go[0] != null)
        {
            if(udp != null) udp.DisconnectFromServer();
            Destroy(go[0]);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
    public void OnExitClick()
    {
        Application.Quit();
    }
}
