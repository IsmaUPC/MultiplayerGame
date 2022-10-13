using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomSelect : MonoBehaviour
{

    // UI Element to join room
    public GameObject joinRoomPanel, dropdown;
    public GameObject[] mainButtons = new GameObject[2];
    private int serverType = 0;

    // When clicking to create a room
    public void OnCreateClick()
    {
        // Create a game object and give it the corresponding component depending on the server type we want
        GameObject serverGO;
        switch (serverType)
        {
            case 0:
                serverGO = new GameObject("UDPServer");
                serverGO.isStatic = true;
                UDPServer s = serverGO.AddComponent<UDPServer>();
                s.enabled = false;
                break;
            case 1:
                serverGO = new GameObject("TCPServer");
                serverGO.isStatic = true;
                serverGO.AddComponent<TCPServer>();
                break;
        }
        SceneManager.LoadScene("Lobby");
    }

    public void OnEditIPEnter(string ip)
    {
        Debug.Log(ip);
    }

    public void OnJoinClick()
    {
        // Change UI
        mainButtons[0].SetActive(false);
        mainButtons[1].SetActive(false);
        dropdown.SetActive(false);

        joinRoomPanel.SetActive(true);
    }

    public void OnBackClick()
    {
        // Change UI
        mainButtons[0].SetActive(true);
        mainButtons[1].SetActive(true);
        dropdown.SetActive(true);

        joinRoomPanel.SetActive(false);
    }

    public void ChangeDropdownValue(int value)
    {
        serverType = (serverType+value)%2;
        Debug.Log(serverType);
    }
}
