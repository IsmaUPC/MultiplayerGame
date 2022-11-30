using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Home : MonoBehaviour
{
    private bool nextScene = false;
    public void OnStartClick()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void Update()
    {
        if (nextScene)
        {
            nextScene = false;
            OnStartClick();
        }
    }

    private void SetNextScene()
    {
        nextScene = true;
    }

    public void OnBackClick()
    {
        // Provisional
        GameObject go = GameObject.FindGameObjectWithTag("NetWork");
        UDPClient udp = go.GetComponent<UDPClient>();
        if (go != null)
        {
            if(udp != null) udp.ShutdownClient();
            Destroy(go);
        }
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
