using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ConnectUiScript : MonoBehaviour
{
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;

    private void Start()
    {
        Debug.Log("ConnectUiScript Start");
        hostButton.onClick.AddListener(HostButtonOnClick);
        clientButton.onClick.AddListener(ClientButtonOnClick);
    }

    private void HostButtonOnClick()
    {
        Debug.Log("HOST BUTTON PRESSED");
        var started = NetworkManager.Singleton.StartHost();
        Debug.Log("StartHost() returned: " + started);
    }

    private void ClientButtonOnClick()
    {
        Debug.Log("CLIENT BUTTON PRESSED");
        var started = NetworkManager.Singleton.StartClient();
        Debug.Log("StartClient() returned: " + started);

        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport == null)
        {
            Debug.LogError("No NetworkTransport set on NetworkManager!");
        }
        else
        {
            Debug.Log("NetworkTransport is set: " + NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetType().Name);
        }
    }
}
