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
        public Vector3 futurePosition;
        public Quaternion futureRotation;

        // Creator id
        public bool isMyObject;

        // Time since last update to clients
        public float deltaLastTime;

        public bool atTargetTransform;
    }

    // World objects
    private List<WorldObject> worldObjects = new List<WorldObject>();

    private float interpolationTime;

    private UDPClient client;

    private GameObject ownPlayerRef;

    private PlayerSpawner ps;

    // Start is called before the first frame update
    void Start()
    {
        // 50 ms
        interpolationTime = 0.05f;

        worldObjects = new List<WorldObject>();

        ps = FindObjectOfType<PlayerSpawner>();

    }

    // Update is called once per frame
    void Update()
    {
        Interpolation();
    }

    private void Interpolation()
    {
        // Iterate world objects and get interpolated transform
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            worldObjects[i].deltaLastTime += Time.deltaTime;
            if (worldObjects[i].deltaLastTime < interpolationTime && !worldObjects[i].atTargetTransform)
            {
                worldObjects[i].obj.transform.position = Vector3.Lerp(worldObjects[i].pastTransform.position, worldObjects[i].futurePosition, worldObjects[i].deltaLastTime / interpolationTime);
                worldObjects[i].obj.transform.rotation = Quaternion.Lerp(worldObjects[i].pastTransform.rotation, worldObjects[i].futureRotation, worldObjects[i].deltaLastTime / interpolationTime);
            }
            if (!worldObjects[i].atTargetTransform && (Vector3.Distance(worldObjects[i].obj.transform.position, worldObjects[i].futurePosition) < 0.01F || worldObjects[i].deltaLastTime >= interpolationTime))
            {
                worldObjects[i].atTargetTransform = true;
                worldObjects[i].obj.transform.position = worldObjects[i].futurePosition;
                worldObjects[i].obj.transform.rotation = worldObjects[i].futureRotation;
            }
        }
    }

    public void UpdateFutureTransform(byte netID, Vector3 tform, int state)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                worldObjects[i].pastTransform.position = worldObjects[i].futurePosition;
                worldObjects[i].pastTransform.rotation = worldObjects[i].futureRotation;
                worldObjects[i].futurePosition = new Vector3(tform.x, worldObjects[i].obj.transform.position.y, tform.z);
                worldObjects[i].futureRotation = Quaternion.Euler(0, tform.y, 0);
                worldObjects[i].deltaLastTime = 0.0f;
                worldObjects[i].atTargetTransform = false;
                break;
            }
        }
    }

    public void DestroyAllObjects()
    {
        for (int i = worldObjects.Count - 1; i >= 0; --i)
        {
            if (worldObjects[i].obj != null)
            {
                Destroy(worldObjects[i].obj);
            }
            worldObjects.RemoveAt(i);
        }
        worldObjects.Clear();
    }

    public void DestroyWorldObject(byte netID)
    {
        for (int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                if (worldObjects[i].obj != null)
                {
                    Destroy(worldObjects[i].obj);
                }
                worldObjects.RemoveAt(i);
                Debug.Log("Network object with ID " + netID.ToString() + " destroyed");
                break;
            }
        }
    }

    // Save a reference to call events from server
    public void AssignUDPClientReference(UDPClient udp)
    {
        client = udp;
    }

    public void CreateWorldObject(byte netID, byte type, bool isMyObject, Transform tform = null, int[] idxs = null, int portID = 0)
    {
        WorldObject wo = new WorldObject();
        wo.netId = netID;
        wo.isMyObject = isMyObject;

        wo.pastTransform = tform;
        wo.atTargetTransform = true;
        switch (type)
        {
            // Case 0 used for player game objects
            case 0:
                if (isMyObject)
                {
                    wo.obj = ownPlayerRef;
                    wo.netId = (byte)portID;
                }
                else
                {
                    ps.SpawnNetPlayer(idxs, portID);
                }

                break;

            // Case 1 used for projectile game objects
            case 1:
                //wo.obj = Instantiate(projectilePrefab, tform);
                break;
            default:
                break;
        }
        wo.deltaLastTime = 0.0F;
        worldObjects.Add(wo);

        Debug.Log("Network object with ID " + netID.ToString() + " created");

        worldObjects.Add(wo);
    }

    public void SetPlayerCreationReference(byte netID, ref GameObject obj)
    {
        for(int i = 0; i < worldObjects.Count; ++i)
        {
            if (worldObjects[i].netId == netID)
            {
                worldObjects[i].obj = obj;
                break;
            }
        }
    }

    public void SetPlayerReference(GameObject obj)
    {
        ownPlayerRef = obj;
    }
}
