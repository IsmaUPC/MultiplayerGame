using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RoundResult : MonoBehaviour
{
    public GameObject nameTextPrefab;
    public int spaceBetweenNames = 10;
    public GameObject iconWinPrefab;
    public int spaceBetweenIcons = 10;
    public List<Color> userColors = new List<Color>();

    private UDPServer server;
    public List<KeyValuePair<string, int>> players;

    private float nameHeight;
    private float iconWidth;

    // Start is called before the first frame update
    void Start()
    {
        RectTransform rectT = nameTextPrefab.GetComponent<RectTransform>();
        nameHeight = rectT.rect.height;

        rectT = iconWinPrefab.GetComponent<RectTransform>();
        iconWidth = rectT.rect.width;

        server = FindObjectOfType<UDPServer>();
        if (server == null) 
            return;

        players = server.GetPlayersVictories();
        ShowResults();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ShowResults()
    {
        for (int i = 0; i < players.Count; ++i)
        {
            GameObject name = Instantiate(nameTextPrefab, transform);
            name.transform.Translate(new Vector3(0, (nameHeight + spaceBetweenNames) * i, 0));

            TextMeshProUGUI textPRO = name.GetComponent<TextMeshProUGUI>();
            textPRO.text = players[i].Key + ":";
            textPRO.color = userColors[i+1];

            for (int j = 0; j < players[i].Value; j++)
            {
                GameObject point = Instantiate(iconWinPrefab, name.transform);
                point.transform.Translate(new Vector3((iconWidth + spaceBetweenIcons) * j, 0, 0));
            }            
        }
    }
}
