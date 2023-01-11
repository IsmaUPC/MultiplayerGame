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
     AudioSource audio;

    public void Start()
    {
        audio = GetComponent<AudioSource>();
    }
    public void PlayFXBoingPunch()
    {
        audio.PlayOneShot(fxBoingPunch[RNG(fxBoingPunch.Length)]);
    }
    public void PlayFXDash()
    {
        audio.PlayOneShot(fxDash[RNG(fxDash.Length)]);
    }
    public void PlayFXHit()
    {
        audio.PlayOneShot(fxHit[RNG(fxHit.Length)]);
    }
    public void PlayFXMockery()
    {
        audio.PlayOneShot(fxMockery[RNG(fxMockery.Length)]);
    }
    public void PlayFXPunch()
    {
        audio.PlayOneShot(fxPunch[RNG(fxPunch.Length)]);
    }

    //Random Number Ganerator
    public int RNG(int length)
    {
        return Random.Range(0, length);
    }

}
