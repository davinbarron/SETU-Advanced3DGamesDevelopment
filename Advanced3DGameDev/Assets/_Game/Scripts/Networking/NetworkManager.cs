using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Fusion;
using Fusion.Photon.Realtime;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private UnityServiceManager _serviceManager;
    [SerializeField] private NetworkRunner _runnerPrefab;
    [SerializeField] private LobbyManager _lobbyManager;

    private NetworkRunner _runner;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _serviceManager.OnAuthenticated += OnAuthComplete;
        StartCoroutine(InitCoroutine());
    }

    private void OnDestroy()
    {
        if (_serviceManager != null)
            _serviceManager.OnAuthenticated -= OnAuthComplete;
    }

    private IEnumerator InitCoroutine()
    {
        var task = _serviceManager.InitializeAndSignIn();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
            Debug.LogError($"InitializeAndSignIn faulted: {task.Exception}");
    }

    private void OnAuthComplete()
    {
        Debug.Log("Auth confirmed. Connecting to lobby...");
        _ = ConnectToLobby();
    }

    private async Task ConnectToLobby()
    {
        if (_runner == null)
            _runner = Instantiate(_runnerPrefab);

        _runner.AddCallbacks(_lobbyManager);

        var result = await _runner.JoinSessionLobby(
            SessionLobby.Shared,
            authentication: BuildAuthValues()
        );

        if (result.Ok)
        {
            Debug.Log("Connected to lobby.");
            _lobbyManager.Initialise(_runner);
        }
        else
        {
            Debug.LogError($"Failed to connect to lobby: {result.ShutdownReason}");
        }
    }

    // Called by LobbyManager when the player picks a room to create or join
    public async Task StartSession(string roomName)
    {
        var sceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
                           ?? _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var result = await _runner.StartGame(new StartGameArgs
        {
            GameMode     = GameMode.Shared,
            SessionName  = roomName,
            SceneManager = sceneManager,
            AuthValues   = BuildAuthValues()
        });

        if (result.Ok)
        {
            Debug.Log($"Joined room '{roomName}' as {UnityServiceManager.PlayerId}");
            _lobbyManager.OnSessionStarted();
        }
        else
        {
            Debug.LogError($"Failed to join room: {result.ShutdownReason}");
        }
    }

    private AuthenticationValues BuildAuthValues()
    {
        var auth = new AuthenticationValues
        {
            AuthType = CustomAuthenticationType.Custom,
            UserId = UnityServiceManager.PlayerId
        };

        auth.AddAuthParameter("id",    UnityServiceManager.PlayerId);
        auth.AddAuthParameter("token", UnityServiceManager.AccessToken);
        return auth;
    }
}