using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Home : MonoBehaviour
{
    private bool nextScene = false;
    public void OnStartClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1); // LOBBY
    }

    private void Update()
    {
        if (nextScene)
        {
            nextScene = false;
            OnStartClick();
        }
    }

    private void SetNextScene(int s)
    {
        nextScene = true;
    }

    public void OnBackClick()
    {
        GameObject goUDP = GameObject.FindGameObjectWithTag("NetWork");
        if (goUDP != null)
        {
            UDPClient udp = goUDP.GetComponent<UDPClient>();
            if (udp != null) udp.ShutdownClient();
            Destroy(goUDP);
        }

        GameObject goData = GameObject.FindGameObjectWithTag("Data");
        if (goData != null)
            Destroy(goData);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
    }
    public void OnExitClick()
    {
        Application.Quit();
    }

    void OnEnable()
    {
        UDPClient.OnStart += SetNextScene;
    }
    void OnDisable()
    {
        UDPClient.OnStart -= SetNextScene;
    }
}

