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
    [HideInInspector] public List<KeyValuePair<string, int>> players;
    private List<Transform> playersGO;

    private float nameHeight;
    private float iconWidth;
    private int maxPoints = 0;

    // Start is called before the first frame update
    void Start()
    {
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

    private void InicializeVariables()
    {
        playersGO = new List<Transform>();
        RectTransform rectT = nameTextPrefab.GetComponent<RectTransform>();
        nameHeight = rectT.rect.height;

        rectT = iconWinPrefab.GetComponent<RectTransform>();
        iconWidth = rectT.rect.width;

        for (int i = 0; i < players.Count; ++i)
        {
            if (players[i].Value > maxPoints)
                maxPoints = players[i].Value;
        }
    }
    public void ShowResults()
    {
        InicializeVariables();

        for (int i = 0; i < players.Count; ++i)
        {
            GameObject name = Instantiate(nameTextPrefab, transform);
            name.transform.Translate(new Vector3(0, -(nameHeight + spaceBetweenNames) * i, 0));

            TextMeshProUGUI textPRO = name.GetComponent<TextMeshProUGUI>();
            textPRO.text = players[i].Key + ":";
            textPRO.color = userColors[i + 1];
            List<GameObject> playersGO2 = new List<GameObject>();
            playersGO2.Add(new GameObject());
            playersGO.Add(name.transform);
        }
        StartCoroutine(SpawnPoints());
    }


    IEnumerator SpawnPoints()
    {
        for (int i = 0; i < maxPoints; ++i)
        {
            for (int j = 0; j < players.Count; j++)
            {
                if (players[j].Value > i)
                {
                    GameObject point = Instantiate(iconWinPrefab, playersGO[j]);
                    point.transform.Translate(new Vector3((iconWidth + spaceBetweenIcons) * i, 0, 0));
                }
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
