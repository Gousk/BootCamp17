using Unity.Netcode;
using UnityEngine;

public class DebugNetworkEvents : MonoBehaviour
{
    void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) => Debug.Log($"Connected: {id}");
        NetworkManager.Singleton.OnClientDisconnectCallback += (id) => Debug.Log($"Disconnected: {id}");
        NetworkManager.Singleton.OnServerStarted += () => Debug.Log("Server started!");
    }
    public void Update()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("SERVER: " + NetworkManager.Singleton.ConnectedClients.Count + " clients");
        }
    }
    public void OnEnable()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }
    void OnClientConnected(ulong id)
    {
        Debug.Log($"SPAWNED: {id}");
    }
}
