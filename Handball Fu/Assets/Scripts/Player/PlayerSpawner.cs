using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

public class PlayerSpawner : MonoBehaviour
{
    public Transform[] spawnLocations;

    [HideInInspector] public bool spawnPlayerManual = false;
    [HideInInspector] public GameObject playerPrefab;

    public bool isCustomAvatarScene = false;

    private DataTransfer data = null;
    private WorldUpdateClient updateClientRef;

    struct PlayerSpawnInfo
    {
        public int[] cosmeticsIdxs;
        public int portId;
    }

    private List<PlayerSpawnInfo> playerPendingToSpawn = new List<PlayerSpawnInfo>();
    private UDPClient client;

    private void Start()
    {
        client = GameObject.FindGameObjectWithTag("NetWork").GetComponent<UDPClient>();

        if (client)
        {
            data = GameObject.FindGameObjectWithTag("Data").GetComponent<DataTransfer>();
            updateClientRef = client.gameObject.GetComponent<WorldUpdateClient>();
            client.spawner = this;

            PlayerSpawnInfo p = new PlayerSpawnInfo();
            p.portId = (isCustomAvatarScene) ? 0 : data.portId;
            p.cosmeticsIdxs = data.indexs;
            playerPendingToSpawn.Add(p);

            if (spawnPlayerManual)
            {
                GameObject player = Instantiate(playerPrefab);
                OnPlayerJoined(player.GetComponent<PlayerInput>());
                updateClientRef.SetPlayerReference(player);?
            }
            playerPendingToSpawn.Clear();
        }

    }

    private void Update()
    {
        if(playerPendingToSpawn.Count != 0)
        {
            Debug.Log("Spawning player...");
            GameObject player = Instantiate(playerPrefab);
            PlayerInput pi = player.GetComponent<PlayerInput>();
            OnPlayerJoined(pi);
            pi.DeactivateInput();
            playerPendingToSpawn.RemoveAt(0);
        }
    }
    void OnPlayerJoined(PlayerInput playerInput)
    {
        Debug.Log("PlayerInput ID: " + playerPendingToSpawn[0].portId + "  Name: " + playerInput.gameObject.name);
        PlayerData playerData = playerInput.gameObject.GetComponent<PlayerData>();

        // Set the player ID, add one to the index to start at Player 1
        playerData.playerID = playerPendingToSpawn[0].portId;

        // Set the start spawn position of the player using the location at the associated element into the array.
        playerData.SetStartTransform(spawnLocations[playerPendingToSpawn[0].portId]);
        if (data)
        {
            Mesh mesh = data.projectiles[playerPendingToSpawn[0].cosmeticsIdxs[5]]; // 5 = gloves
            playerData.SetBodyParts(data.cosmetics, mesh, playerPendingToSpawn[0].cosmeticsIdxs);
        }

        // Create remote player
        if(!isCustomAvatarScene)
        {
            if (playerPendingToSpawn[0].portId == data.portId)          
                client.SendInfoSpawnToServer(playerPendingToSpawn[0].cosmeticsIdxs, playerPendingToSpawn[0].portId);
            else
                playerInput.DeactivateInput();
        }
        
    }

    public void SpawnNetPlayer(int[] cosmeticsIdxs, int portId)
    {
        if(!isCustomAvatarScene)
        {
            Debug.Log("Adding player...");
            PlayerSpawnInfo p = new PlayerSpawnInfo();
            p.portId = portId;
            p.cosmeticsIdxs = cosmeticsIdxs;
            playerPendingToSpawn.Add(p);
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(PlayerSpawner))]
public class RandomScript_Editor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); // for other non-HideInInspector fields

        PlayerSpawner script = (PlayerSpawner)target;

        // draw checkbox for the bool
        script.spawnPlayerManual = EditorGUILayout.Toggle("Spawn Player Manual", script.spawnPlayerManual);
        if (script.spawnPlayerManual) // if bool is true, show other fields
        {
            script.playerPrefab = EditorGUILayout.ObjectField("Player Prefab", script.playerPrefab, typeof(GameObject), true) as GameObject;
        }
    }
}
#endif
