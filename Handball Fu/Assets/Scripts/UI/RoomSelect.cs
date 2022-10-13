using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomSelect : MonoBehaviour
{

    // UI Element to join room
    public GameObject joinRoomPanel;
    public GameObject[] mainButtons = new GameObject[2];
    public void OnCreateClick()
    {

    }

    public void OnJoinClick()
    {
        // Change UI
        mainButtons[0].SetActive(false);
        mainButtons[1].SetActive(false);

        joinRoomPanel.SetActive(true);
    }

    public void OnBackClcik()
    {
        // Change UI
        mainButtons[0].SetActive(true);
        mainButtons[1].SetActive(true);

        joinRoomPanel.SetActive(false);
    }
}
