using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UI;
using UnityEngine.UI;
using TMPro;

public class ChatManager : MonoBehaviour
{
    public int maxMessages = 20;

    public GameObject chatPanel, textObject;
    public TMP_InputField chatBox;
    public Scrollbar scrollBar;
    public Color infoColor;

    public UDPClient client;
    public UDPServer server;

    private int myColorIdx;

    public List<Color> userColors = new List<Color>();
    [SerializeField] private string[] stylesKeywords = {"***","**","*","!!","!"};
    [SerializeField] private string[] stylesHTML = { "<b><i>","<i>","<b>", "<uppercase><color=red>","<uppercase>" };

    private List<Message> messageList = new List<Message>();
    // Start is called before the first frame update
    void Start()
    {
        if (stylesKeywords.Length != stylesHTML.Length)
            Debug.LogWarning("Lenght of StylesKeyWords is diferent that StylesHTML!");

        client = GameObject.FindGameObjectWithTag("NetWork").GetComponent<UDPClient>();
    }

    // Update is called once per frame
    void Update()
    {
        if (client != null)
        {
            string tmp = client.GetLastMessage();
            if (tmp != "")
            {
                int ind = int.Parse(tmp.Substring(0, 1));
                string[] d = tmp.Substring(1).Split(';');
                SendMessageToChat(ind, d[0], d[1]);
            }
        }
    }
    public void SendMessageToChat(int colorIdx, string username, string text, Message.MessageType type = Message.MessageType.PLAYER)
    {
        // If list is fill, applay FIFO
        if (messageList.Count >= maxMessages)
        {
            Destroy(messageList[0].textObject.gameObject);
            messageList.Remove(messageList[0]);
        }

        Message message = new Message();
        message.text = text;
        GameObject newTextObject = Instantiate(textObject, chatPanel.transform);
        message.textObject = newTextObject.GetComponent<TextMeshProUGUI>();

        //Add styles to text
        message.text = AddStyles(text);
        // Set color in function message type
        if (type == Message.MessageType.PLAYER)
        {
            message.textObject.text = "<color=#" + ColorUtility.ToHtmlStringRGB(userColors[colorIdx]) + ">" + username + ": " + "</color>" + message.text;
        }
        else
        {
            message.textObject.text = message.text;
            message.textObject.color = infoColor;
        }

        messageList.Add(message);
        Debug.Log(text);
    }

    public string AddStyles(string text)
    {
        for (int i = 0; i < stylesKeywords.Length; i++)
        {
            if(text.Contains(stylesKeywords[i]))
            {
                // Contain the keyword style?
                int index = text.IndexOf(stylesKeywords[i]);
                Debug.Log(index);
                // And user close it?
                int index2 = text.IndexOf(stylesKeywords[i], index + 1);
                Debug.Log(index2);
                // Yes
                if (index2 != -1)
                {
                    text = text.Remove(index, stylesKeywords[i].Length).Insert(index, stylesHTML[i]);
                    Debug.Log(text);
                    index2 += Mathf.Abs(stylesKeywords[i].Length - stylesHTML[i].Length);
                    text = text.Remove(index2, stylesKeywords[i].Length).Insert(index2, stylesHTML[i].Replace("<", "</"));
                    Debug.Log(text);
                }
            }
        }

        return text;
    }

    public void OnSendMessage()
    {
        if(chatBox.text != "")
        {
            client.SendMessageToServer(chatBox.text);
            chatBox.text = "";
            if(scrollBar.value <= 0)
                scrollBar.value = 0;
        }
    }
}

public class Message
{
    public string text;
    public TextMeshProUGUI textObject;
    MessageType messageType;
    public enum MessageType
    {
        PLAYER,
        INFO
    }
}