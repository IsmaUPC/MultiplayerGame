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
        GameObject go = GameObject.Find("UDPServer");
        if(go != null)
        {
            go.GetComponent<UDPServer>().OnServerClose();
            Destroy(go);
        }
        else
        {
            go = GameObject.Find("UDPClient");
            go.GetComponent<UDPClient>().ShutdownClient();
            Destroy(go);
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
    public void OnExitClick()
    {
        Application.Quit();
    }
}
