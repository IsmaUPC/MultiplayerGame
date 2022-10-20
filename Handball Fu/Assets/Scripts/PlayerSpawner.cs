using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerSpawner : MonoBehaviour
{
    public Transform[] spawnLocations;
    void OnPlayerJoined(PlayerInput playerInput)
    {
        Debug.Log("PlayerInput ID: " + playerInput.playerIndex);

        // Temporal condition, delete when we have other way to close sockets
        if(playerInput.gameObject.GetComponent<PlayerData>())
        {
            // Set the player ID, add one to the index to start at Player 1
            playerInput.gameObject.GetComponent<PlayerData>().playerID = playerInput.playerIndex;

            // Set the start spawn position of the player using the location at the associated element into the array.
            playerInput.gameObject.GetComponent<PlayerData>().SetStartTransform(spawnLocations[playerInput.playerIndex]);
        }        
    }
}
