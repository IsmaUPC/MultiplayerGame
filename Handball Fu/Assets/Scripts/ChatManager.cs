using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.UI;
using TMPro;

public class ChatManager : MonoBehaviour
{
    public int maxMessages = 20;
    public string username = "Alejandro";

    public GameObject chatPanel, textObject;
    public TMP_InputField chatBox;
    public Color infoColor;
    public List<Color> userColors = new List<Color>();

    [SerializeField]
    List<Message> messageList = new List<Message>();
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void SendMessageToChat(string text, Message.MessageType type = Message.MessageType.PLAYER)
    {
        if (messageList.Count >= maxMessages)
        {
            Destroy(messageList[0].textObject.gameObject);
            messageList.Remove(messageList[0]);
        }

        Message message = new Message();
        message.text = text;
        GameObject newTextObject = Instantiate(textObject, chatPanel.transform);

        message.textObject = newTextObject.GetComponent<TextMeshProUGUI>();
        if (type == Message.MessageType.PLAYER)
        {
            message.textObject.text = "<color=#" + ColorUtility.ToHtmlStringRGB(userColors[3]) + ">" + username + ": " + "</color>" + text;
        }
        else
        {
            message.textObject.text = message.text;
            message.textObject.color = infoColor;
        }

        //Add styles to text
        message.textObject.text = AddStyles(message.textObject.text);

        messageList.Add(message);

        Debug.Log(text);
    }

    public string AddStyles(string text)
    {
        if (text.Contains("***"))
        {
            int index = text.IndexOf("***");
            int index2 = text.IndexOf("***", index + 3);
            if (index2 != -1)
            {
                text = text.Remove(index, 3).Insert(index, "<b><i>");
                index2 += 3;
                text = text.Remove(index2, 3);
                text = text.Insert(index2, "</b></i>");
            }
        }
        else if (text.Contains("**"))
        {
            int index = text.IndexOf("**");
            int index2 = text.IndexOf("**", index + 2);
            if (index2 != -1)
            {
                text = text.Remove(index, 2).Insert(index, "<i>");
                index2 += 1;
                text = text.Remove(index2, 2).Insert(index2, "</i>");
            }
        }
        else if (text.Contains("*"))
        {
            int index = text.IndexOf("*");
            int index2 = text.IndexOf("*", index + 1);
            if (index2 != -1)
            {
                text = text.Remove(index, 1).Insert(index, "<b>");
                index2 += 2;
                text = text.Remove(index2, 1).Insert(index2, "</b>");
            }
        }
        return text;
    }

    public void OnSendMessage()
    {
        if(chatBox.text != "")
        {
            SendMessageToChat(chatBox.text);
            chatBox.text = "";
        }
    }
    public void OnFocusChatbox()
    {
        if (!chatBox.isFocused) chatBox.ActivateInputField();
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