using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLoader : MonoBehaviour
{
    private List<string> levels = new List<string>();

    public int levelCount = 0;

    public Animator transition;

    public float transitionTime = 1f;

    private float timer = 0;

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

        timer += Time.deltaTime;

        if(timer >= 6.0f && levels.Count > 0)
        {
            LoadNextLevel();
            timer = 0f;
        }
    }

    public void LoadNextLevel()
    {
        StartCoroutine(LoadLevel(levels[Random.Range(0, levels.Count)]));
    }

    IEnumerator LoadLevel(string sceneName)
    {
        transition.SetTrigger("Start");

        yield return new WaitForSeconds(transitionTime);

        SceneManager.LoadScene(sceneName);

        Debug.Log("Loaded scene: " + sceneName);

        levels.Remove(sceneName);

        transition.Play("Crossfade_End");
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
