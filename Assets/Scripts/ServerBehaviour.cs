using UnityEngine;
using UnityEngine.Assertions;

using Unity.Collections;
using Unity.Networking.Transport;

public class ServerBehaviour : MonoBehaviour
{
    [SerializeField]
    public ushort Port = 9000;
    private NetworkDriver localDriver;
    private NativeList<NetworkConnection> connections;

    void Start()
    {
        localDriver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = Port;
        if (localDriver.Bind(endpoint) != 0)
        {
            Debug.Log("Failed to bind to port " + Port);
        }
        else
        {
            localDriver.Listen();
        }

        connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    public void OnDestroy()
    {
        localDriver.Dispose();
        connections.Dispose();
    }

    void FixedUpdate()
    {
        localDriver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated)
            {
                connections.RemoveAtSwapBack(i);
                --i;
            }
        }
        // AcceptNewConnections
        NetworkConnection c;
        while ((c = localDriver.Accept()) != default(NetworkConnection))
        {
            connections.Add(c);
            Debug.Log("Accepted a connection");
        }

        DataStreamReader stream;
        for (int i = 0; i < connections.Length; i++)
        {
            Assert.IsTrue(connections[i].IsCreated);

            NetworkEvent.Type cmd;
            while ((cmd = localDriver.PopEventForConnection(connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    var json = stream.ReadString();
                    var writer = localDriver.BeginSend(NetworkPipeline.Null, connections[i]);
                    writer.WriteString(json);
                    localDriver.EndSend(writer);
                    //TODO for test
                    //if (Random.Range(0, 99) % 9 == 0) 
                    //{
                    //    localDriver.Disconnect(connections[i]);
                    //}
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    connections[i] = default(NetworkConnection);
                }
            }
        }
    }
}