using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public GameObject optionMenu;
    public GameObject crossConfirm;
    public Slider slider;
    int defaultFullScreen;
    bool clickVolume = false;
    float volume, volAux;

    public GameObject[] audios;

    void Start()
    {
        audios = GameObject.FindGameObjectsWithTag("Audio");
        slider.value = PlayerPrefs.GetFloat("volumeGame", 0.75f);
        defaultFullScreen = PlayerPrefs.GetInt("defaultFullScreen", 0);
        if (defaultFullScreen>1) Screen.fullScreen = true;
         volume = 0.75f;
        volAux = 0.75f;

    }

    public void OnFullScreen()
    {
        // Toggle fullscreen
        Screen.fullScreen = !Screen.fullScreen;
        crossConfirm.SetActive(!Screen.fullScreen);

    }
    public void OnOptionMenu()
    {
        optionMenu.SetActive(!optionMenu.active);
    }
    public void ChangeSliderVolume()
    {
        if (volume > 0.00f)
        {
            PlayerPrefs.SetFloat("volumeGame", slider.value);
            volAux = slider.value;
        }
        foreach (GameObject audio in audios)
            audio.GetComponent<AudioSource>().volume = slider.value;

    }

    public void OnClickVolume()
    {
        clickVolume = !clickVolume;
        volume = (clickVolume) ? 0.00f : volume = volAux;
        slider.value = volume;
    }

}
