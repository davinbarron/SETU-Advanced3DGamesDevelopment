using System.Threading.Tasks;
using UnityEngine;
using Fusion;
using Fusion.Photon.Realtime;

public class NetworkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UnityServiceManager _serviceManager;
    [SerializeField] private NetworkRunner _runnerPrefab;

    [Header("Room Configuration")]
    [SerializeField] private string _roomName = "Room_01";

    private NetworkRunner _runner;

    private void Start()
    {
        _serviceManager.OnAuthenticated += OnAuthComplete;
        StartCoroutine(InitCoroutine());
    }

    private System.Collections.IEnumerator InitCoroutine()
    {
        var task = _serviceManager.InitializeAndSignIn();
        yield return new WaitUntil(() => task.IsCompleted);

        if (task.IsFaulted)
            Debug.LogError($"InitializeAndSignIn faulted: {task.Exception}");
    }

    private void OnDestroy()
    {
        if (_serviceManager != null)
            _serviceManager.OnAuthenticated -= OnAuthComplete;
    }

    private void OnAuthComplete()
    {
        Debug.Log("Auth confirmed. Starting Fusion...");
        _ = StartFusionSession();
    }

    private async Task StartFusionSession()
    {
        if (_runner == null)
            _runner = Instantiate(_runnerPrefab);

        var sceneManager = _runner.GetComponent<NetworkSceneManagerDefault>()
                        ?? _runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

        var authValues = new AuthenticationValues();
        
        authValues.AuthType = CustomAuthenticationType.Custom;
        authValues.UserId = UnityServiceManager.PlayerId;
        authValues.AddAuthParameter("id", UnityServiceManager.PlayerId);
        authValues.AddAuthParameter("token", UnityServiceManager.AccessToken);

        Debug.Log($"Setting custom authentication parameters: user = {UnityServiceManager.PlayerId}, token length = {UnityServiceManager.AccessToken?.Length ?? 0}");

        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,
            SessionName = _roomName,
            SceneManager = sceneManager,
            AuthValues = authValues
        });

        if (result.Ok)
            Debug.Log($"Joined room '{_roomName}' successfully as {UnityServiceManager.PlayerId}");
        else
            Debug.LogError($"Failed to join Fusion room: {result.ShutdownReason}");
    }
}