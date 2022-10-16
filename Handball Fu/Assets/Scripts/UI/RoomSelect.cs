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

    public GameObject udpServer, tcpServer, udpClient, tcpClient;

    // When clicking to create a room
    public void OnCreateClick()
    {
        // Create a game object and give it the corresponding component depending on the server type we want
        Debug.Log("Creating server game object");
        GameObject[] lastServers = GameObject.FindGameObjectsWithTag("NetWork");
        if (lastServers.Length > 0)
        {
            lastServers[0].GetComponent<UDPServer>().OnServerClose();
            Destroy(lastServers[0]);
        }

        GameObject serverGO;
        switch (serverType)
        {
            case 0:
                serverGO = Instantiate(udpServer);
                DontDestroyOnLoad(serverGO);
                break;
            case 1:
                serverGO = Instantiate(tcpServer);
                DontDestroyOnLoad(serverGO);
                break;
        }
        Debug.Log("Loading Lobby");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
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
