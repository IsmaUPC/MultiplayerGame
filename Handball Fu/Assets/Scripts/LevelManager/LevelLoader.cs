using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelLoader : MonoBehaviour
{
    private List<int> levels = new List<int>();
    public int indexLevel1 = 0;
    public int levelCount = 1;
    public float transitionTime = 1f;
    public CircleWipeController circleWipe;
    private int iAmCreateOnScene = 2;

    void OnEnable()
    {
        UDPClient.OnStart += OnNextLevel;
    }
    void OnDisable()
    {
        UDPClient.OnStart -= OnNextLevel;
    }

    void Start()
    {
        DontDestroyOnLoad(this.gameObject);
        ResetLevels();
        UDPServer server = FindObjectOfType<UDPServer>();
        if (server)
            server.SetLevelLoader(this);

        iAmCreateOnScene = SceneManager.GetActiveScene().buildIndex;
    }

    public int GetFirstLevelOfList()
    {
        return levels[0];
    }

    public void OnNextLevel(int level)
    {
        StartCoroutine(LoadLevel(level));
    }

    IEnumerator LoadLevel(int sceneIndexBuild)
    {
        // Find position where the center of the circle will be
        var mousePos = Mouse.current.position.ReadValue();
        var x = (mousePos.x - Screen.width / 2f);
        var y = (mousePos.y - Screen.height / 2f);
        var offset = new Vector2(x / Screen.width, y / Screen.height);
        circleWipe.offset = offset;
        circleWipe.FadeOut();

        yield return new WaitForSeconds(circleWipe.duration);
        SceneManager.LoadScene(sceneIndexBuild);

        circleWipe.FadeIn();
        circleWipe.findPosition = true;
        yield return new WaitForSeconds(circleWipe.duration);

        // If return to custom scene (create a camera and levelLoader) destroy the currents
        if(sceneIndexBuild == iAmCreateOnScene)
        {
            Destroy(Camera.main.gameObject, 0.1f);
            Destroy(gameObject);
        }

        levels.RemoveAt(0);
        ResetLevels();
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

        GameObject lastCamera = Camera.main.gameObject;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        Destroy(lastCamera, 0.1f);
        Destroy(gameObject);
    }
    public void OnExitClick()
    {
        Application.Quit();
    }

    public void ResetLevels()
    {
        if (levels.Count == 0)
        {
            levels.Clear();
            for (int i = 0; i < levelCount; i++)
            {
                levels.Add(indexLevel1 + i);
            }
            levels.Sort();
        }
    }
}
