using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    public GameObject optionMenu;
    public GameObject crossConfirm;
    public Slider sliderMusic;
    public Slider sliderFx;
    int defaultFullScreen;
    bool clickVolume = false;
    float volume, volAux;
    public GameObject fxSounds;

    public GameObject[] music;

    void Start()
    {
      
        sliderMusic.value = PlayerPrefs.GetFloat("musicGame", 0.75f);
        sliderFx.value = PlayerPrefs.GetFloat("fxGame", 0.75f);
        defaultFullScreen = PlayerPrefs.GetInt("defaultFullScreen", 0);
        if (defaultFullScreen > 1) Screen.fullScreen = true;
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
    public void ChangeSliderVolume(Slider slider)
    {
        if (volume > 0.00f)
        {
            if (slider.name == "SliderMusic")
            {
                PlayerPrefs.SetFloat("musicGame", slider.value);
            }
            else
            {
                PlayerPrefs.SetFloat("fxGame", slider.value);
            }

            volAux = slider.value;
        }
        if (slider.tag == "Music" && music.Length>0)
        {
            foreach (GameObject audio in music)
                audio.GetComponent<AudioSource>().volume = slider.value;
        }
        else 
        {
            fxSounds.GetComponent<AudioSource>().volume = slider.value;
            //fxSounds.GetComponent<AudioManager>().PlayFXHit();
        }

    }

    public void OnClickVolume(Slider slider)
    {
        clickVolume = !clickVolume;
        volume = (clickVolume) ? 0.00f : volume = (slider.name == "SliderMusic") ? PlayerPrefs.GetFloat("musicGame") : PlayerPrefs.GetFloat("fxGame");
        slider.value = volume;
    }
    public void OnOpenMenuOptions() 
    {
        OnOptionMenu();

    }
}
