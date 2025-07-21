using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;
using System.Linq;

public class NetworkRunnerHandler : MonoBehaviour
{
    [SerializeField]
    NetworkRunner networkRunnerPrefab;

    NetworkRunner networkRunner;

    private void Awake()
    {
        // Find existing runner, if any
        networkRunner = FindFirstObjectByType<NetworkRunner>();
    }

    private void Start()
    {
        // If not found, instantiate
        if (networkRunner == null)
        {
            networkRunner = Instantiate(networkRunnerPrefab);
            networkRunner.name = "Network Runner";
        }

        // Proceed as normal
        var clientTask = InitializeNetworkRunner(networkRunner, GameMode.AutoHostOrClient, "TestSession", NetAddress.Any(), SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex), null);

        Utils.DebugLog("InitializeNetworkRunner called");
    }

    INetworkSceneManager GetSceneManager(NetworkRunner networkRunner)
    {
        // Try to find an INetworkSceneManager on the runner, otherwise add one
        INetworkSceneManager sceneManager = networkRunner.GetComponents<MonoBehaviour>().OfType<INetworkSceneManager>().FirstOrDefault();

        if (sceneManager == null)
        {
            sceneManager = networkRunner.gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        return sceneManager;
    }

    protected virtual Task InitializeNetworkRunner(NetworkRunner networkRunner, GameMode gameMode, string sessionName, NetAddress address, SceneRef scene, Action<NetworkRunner> initialized)
    {
        INetworkSceneManager sceneManager = GetSceneManager(networkRunner);

        networkRunner.ProvideInput = true;

        return networkRunner.StartGame(new StartGameArgs
        {
            GameMode = gameMode,
            Address = address,
            Scene = scene,
            SessionName = sessionName,
            CustomLobbyName = "OurLobbyID",
            SceneManager = sceneManager
        });
    }
}
