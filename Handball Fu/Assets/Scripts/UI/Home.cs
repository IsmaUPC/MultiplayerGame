using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Home : MonoBehaviour
{
    // Start is called before the first frame update
    public void OnStartClick()
    {
        SceneManager.LoadScene("Start");
    }
    public void OnExitClick()
    {
        Application.Quit();
    }
}
