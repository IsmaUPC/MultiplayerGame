using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;

public class PlayerSpawner : MonoBehaviour
{
    public Transform[] spawnLocations;
    private PlayerInputManager im;

    [HideInInspector] public bool spawnPlayerManual = false;
    [HideInInspector] public GameObject playerPrefab;

    public bool isCustomAvatarScene = false;

    private DataTransfer data = null;

    private int[] cosmeticsIdxs;
    private int portId;
    private UDPClient client;

    private void Start()
    {
        client = GameObject.FindGameObjectWithTag("NetWork").GetComponent<UDPClient>();

        if (client)
        {
            data = GameObject.FindGameObjectWithTag("Data").GetComponent<DataTransfer>();

            portId = (isCustomAvatarScene) ? 0 : data.portId;
            cosmeticsIdxs = data.indexs;
            client.spawner = this;

            if (spawnPlayerManual)
            {
                im = GetComponent<PlayerInputManager>();
                im.playerPrefab = playerPrefab;
                im.JoinPlayer();
            }
        }

    }
    void OnPlayerJoined(PlayerInput playerInput)
    {
        Debug.Log("PlayerInput ID: " + portId + "  Name: " + playerInput.gameObject.name);
        PlayerData playerData = playerInput.gameObject.GetComponent<PlayerData>();

        // Set the player ID, add one to the index to start at Player 1
        playerData.playerID = portId;

        // Set the start spawn position of the player using the location at the associated element into the array.
        playerData.SetStartTransform(spawnLocations[portId]);

        if (data)
        {
            data.projectilePrefab.GetComponent<MeshFilter>().mesh = data.projectiles[cosmeticsIdxs[5]]; // 5 = gloves
            playerData.SetBodyParts(data.cosmetics, data.projectilePrefab, cosmeticsIdxs);
        }

        if (portId == data.portId)
        {
            if (!isCustomAvatarScene)
            {
                // Create remote player
                client.SendInfoSpawnToServer(cosmeticsIdxs, portId);
            }
        }
        else
        {
            playerInput.enabled = false;
        }
    }

    public void SpawnNetPlayer(int[] cosmeticsIdxs, int portId)
    {
        this.cosmeticsIdxs = cosmeticsIdxs;
        this.portId = portId;
        im.JoinPlayer();
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
