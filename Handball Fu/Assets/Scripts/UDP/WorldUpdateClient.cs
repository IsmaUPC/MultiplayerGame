using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldUpdateClient : MonoBehaviour
{
    // Client world object
    class WorldObject
    {
        // This two together identify the world object
        public byte netId;

        // Gameobject reference
        public GameObject obj;

        // Past transform
        public Transform pastTransform;

        // Future transform
        public Transform futureTransform;

        // Creator id
        public bool isMyObject;

        // Time since last update to clients
        public float deltaLastTime;
    }

    // World objects
    private List<WorldObject> worldObjects = new List<WorldObject>();

    private float interpolationTime;

    private UDPClient client;

    // Start is called before the first frame update
    void Start()
    {
        // 50 ms
        interpolationTime = 0.050F;

        client = GameObject.FindObjectOfType<UDPClient>();

        worldObjects = new List<WorldObject>();

    }

    // Update is called once per frame
    void Update()
    {
        // TODO NET: Interpolate between transform positions and rotations...
        // Save all transform data or just position and rotation?
    }
    
    // Save a reference to call events from server
    public void AssignUDPClientReference(UDPClient udp)
    {
        client = udp;
    }

    public void CreateWorldObject(byte netID, byte type, bool isMyObject, Transform tform)
    {
        WorldObject wo = new WorldObject();

        wo.netId = netID;
        wo.isMyObject = isMyObject;
        // TODO NET: If structure, depending on type, create specific prefab with specific cosmetics and the specific given transform

        worldObjects.Add(wo);
    }
}
