using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioClip[] fxBoingPunch;
    public AudioClip[] fxDash;
    public AudioClip[] fxHit;
    public AudioClip[] fxMockery;
    public AudioClip[] fxPunch;

    public void Start()
    {


    }
    public static void PlaySound()
    {
        GameObject soundGameObject = new GameObject("Sound");
        AudioSource audioSource = soundGameObject.AddComponent<AudioSource>();
        //   audioSource.PlayOneShot();
    }

}
