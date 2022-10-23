using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements;
using TMPro;
using UnityEditor.PackageManager;

public class RoomSelect : MonoBehaviour
{
    // UI Element to join room
    public GameObject joinRoomPanel, dropdown;
    public GameObject[] mainButtons = new GameObject[2];
    public TextMeshProUGUI errorText;
    private float errorTime;
    private int serverType = 0;

    public GameObject udpServer, tcpServer, udpClient, tcpClient;
    private UDPClient udp;
    private GameObject udpC;
    private string username;

    private void Start()
    {
        errorText.enabled = false;
    }

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

    private void Update()
    {
        if (errorText.enabled && errorTime > 0.0F)
        {
            errorTime -= Time.deltaTime;
        }
        else if (errorText.enabled && errorTime < 0.0F)
        {
            errorText.enabled = false;
        }

        if(udpC != null && udp.GetCurrentState() == 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else if (udpC != null && udp.GetCurrentState() == 3)
        {
            errorText.text = "Connection timed out";
            errorText.enabled = true;
            errorTime = 5.0F;
            udp.ShutdownClient();
            Destroy(udpC);
            udpC = null;
            udp = null;
        }
    }

    public void OnEditUsername(string _username)
    {
        username = _username;
    }

    public void OnEditIPEnter(string ip)
    {
        if(username == "")
        {
            errorText.text = "Input a username";
            errorText.enabled = true;
            errorTime = 5.0F;
            return;
        }
        GameObject[] lastServers = GameObject.FindGameObjectsWithTag("NetWork");
        if (lastServers.Length > 0)
        {
            lastServers[0].GetComponent<UDPServer>().OnServerClose();
            Destroy(lastServers[0]);
        }
        GameObject client;
        switch(serverType)
        {
            case 0:
                client = Instantiate(udpClient);
                DontDestroyOnLoad(client);
                udp = client.GetComponent<UDPClient>();
                udp.ClientStart();
                bool result = udp.ConnectToIp(ip, username);
                if (!result)
                {
                    errorText.text = "IP invalid format";
                    errorText.enabled = true;
                    errorTime = 5.0F;
                    udp.ShutdownClient();
                    Destroy(client);
                    udp = null;
                }
                else
                {
                    udpC = client;
                }
                break;
            case 1:
                client = Instantiate(tcpClient);
                DontDestroyOnLoad(client);
                break;
        }
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

        if (udp != null)
        {
            udp.ShutdownClient();
            udp = null;
        }
        if (udpC != null)
        {
            Destroy(udpC);
            udpC = null;
        }
    }

    public void ChangeDropdownValue(int value)
    {
        serverType = (serverType+value)%2;
        Debug.Log(serverType);
    }
}
