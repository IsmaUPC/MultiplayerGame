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

    private DataTransfer data = null;
    private void Start()
    {
        data = GameObject.FindGameObjectWithTag("Data").GetComponent<DataTransfer>();
        if (spawnPlayerManual)
        {
            im = GetComponent<PlayerInputManager>();
            im.playerPrefab = playerPrefab;
            im.JoinPlayer();
        }
    }
    void OnPlayerJoined(PlayerInput playerInput)
    {
        Debug.Log("PlayerInput ID: " + playerInput.playerIndex + "  Name: " + playerInput.gameObject.name);
        PlayerData playerData = playerInput.gameObject.GetComponent<PlayerData>();

        // Set the player ID, add one to the index to start at Player 1
        playerData.playerID = playerInput.playerIndex;

        // Set the start spawn position of the player using the location at the associated element into the array.
        playerData.SetStartTransform(spawnLocations[playerInput.playerIndex]);

        if (data)
        {
            data.projectilePrefab.GetComponent<MeshFilter>().mesh = data.projectiles[data.indexs[5]]; // 5 = gloves
            playerData.SetBodyParts(data.cosmetics, data.projectilePrefab, data.indexs);
        }
        // TODO: Notify to server: Create this player on other clients
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
