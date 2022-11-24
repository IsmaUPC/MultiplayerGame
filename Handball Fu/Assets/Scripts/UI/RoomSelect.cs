using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class RoomSelect : MonoBehaviour
{
    // UI Element to join room
    public GameObject ipPanel, usernamePanel;
    public GameObject[] mainButtons = new GameObject[2];
    public TextMeshProUGUI errorText;
    private float errorTime;

    // Server starting logic
    public GameObject udpServerPrefab, udpClientPrefab;
    private UDPClient udpComponent;
    private GameObject udpGO;
    private string username, ip;

    private void Start()
    {
        errorText.enabled = false;
    }

    // When clicking to create a room
    public void OnCreateClick()
    {
        // Create a udp server prefab clone
        Debug.Log("Creating server game object");
        GameObject lastServers = GameObject.FindGameObjectWithTag("NetWork");
        if (lastServers != null)
        {
            lastServers.GetComponent<UDPServer>().OnServerClose();
            Destroy(lastServers);
        }

        GameObject serverGO;
        serverGO = Instantiate(udpServerPrefab);
        DontDestroyOnLoad(serverGO);

        Debug.Log("Loading Lobby");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void Update()
    {
        // Check if error total show time is exhausted
        if (errorText.enabled && errorTime > 0.0F)
        {
            errorTime -= Time.deltaTime;
        }
        else if (errorText.enabled && errorTime < 0.0F)
        {
            errorText.enabled = false;
        }

        // Check connection state, if could connect, load next scene if not show error and reset objects
        if (udpGO != null && udpComponent.GetCurrentState() == 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        else if (udpGO != null && udpComponent.GetCurrentState() == 3)
        {
            ShowErrorMessage("Connection timed out");
            udpComponent.ShutdownClient();
            Destroy(udpGO);
            udpGO = null;
            udpComponent = null;
        }
    }

    public void EditUsername(string tmp)
    {
        username = tmp;
    }

    public void OnUsernameEnter()
    {
        if(username == "")
        {
            ShowErrorMessage("Username cannot be blank!");
            return;
        }
        else if(username.Length > 20)
        {
            ShowErrorMessage("Username cannot be longer than 20 characters!");
            username = "";
            return;
        }
        else
        {
            ipPanel.SetActive(true);
            usernamePanel.SetActive(false);
        }
    }

    public void EditIP(string tmp)
    {
        ip = tmp;
    }

    public void OnIPEnter()
    {
        GameObject lastObj = GameObject.FindGameObjectWithTag("NetWork");
        if (lastObj != null)
        {
            lastObj.GetComponent<UDPClient>().ShutdownClient();
            Destroy(lastObj);
        }
        GameObject client;
        client = Instantiate(udpClientPrefab);
        DontDestroyOnLoad(client);
        udpComponent = client.GetComponent<UDPClient>();
        udpComponent.ClientStart();
        bool result = udpComponent.ConnectToIp(ip, username);
        if (!result)
        {
            ShowErrorMessage("IP invalid format");
            udpComponent.ShutdownClient();
            Destroy(client);
            udpComponent = null;
        }
        else
        {
            udpGO = client;
        }
    }

    private void ShowErrorMessage(string message)
    {
        errorText.text = message;
        errorText.enabled = true;
        errorTime = 5.0F;
    }

    public void OnJoinClick()
    {
        // Change UI
        mainButtons[0].SetActive(false);
        mainButtons[1].SetActive(false);

        usernamePanel.SetActive(true);
    }

    public void IPBackToUsername()
    {
        ipPanel.SetActive(false);
        usernamePanel.SetActive(true);

        if (udpComponent != null)
        {
            udpComponent.ShutdownClient();
            udpComponent = null;
        }
        if (udpGO != null)
        {
            Destroy(udpGO);
            udpGO = null;
        }
    }

    public void OnBackClick()
    {
        // Change UI
        mainButtons[0].SetActive(true);
        mainButtons[1].SetActive(true);

        usernamePanel.SetActive(false);
    }
}
