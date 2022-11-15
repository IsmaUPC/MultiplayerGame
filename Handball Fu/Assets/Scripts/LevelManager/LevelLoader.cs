using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class LevelLoader : MonoBehaviour
{
    private List<string> levels = new List<string>();

    public int levelCount = 0;

    public Animator transition;

    public float transitionTime = 1f;

    public CircleWipeController circleWipe;

    //private float timer = 0;

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        ResetLevels();
    }

    // Update is called once per frame
    void Update()
    {

        // timer += Time.deltaTime;

        // if(timer >= 6.0f && levels.Count > 0)
        // {
        //     //LoadNextLevel();
        //     timer = 0f;
        // }*
    }

    public void OnNextLevel()
    {
        if(levels.Count > 0) 
            StartCoroutine(LoadLevel(levels[Random.Range(0, levels.Count)]));
    }

    IEnumerator LoadLevel(string sceneName)
    {
        //transition.SetTrigger("Start");

        var mousePos = Mouse.current.position.ReadValue();
        var x = (mousePos.x - Screen.width / 2f);
        var y = (mousePos.y - Screen.height / 2f);
        var offset = new Vector2(x / Screen.width, y / Screen.height);
        circleWipe.offset = offset;
        circleWipe.FadeOut();

        yield return new WaitForSeconds(circleWipe.duration);
        var asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        circleWipe.FadeIn();
        yield return new WaitForSeconds(circleWipe.duration);

        //SceneManager.LoadScene(sceneName);

        Debug.Log("Loaded scene: " + sceneName);

        levels.Remove(sceneName);

        //transition.Play("Crossfade_End");
    }

    public void ResetLevels()
    {
        if (levels.Count == 0) levels.Clear();
        for (int i = 1; i < levelCount; i++)
        {
            levels.Add("Level_" + (i + 1).ToString());
        }
    }
}
