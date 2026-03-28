using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;

public class UnityServiceManager : MonoBehaviour
{
    public static UnityServiceManager Instance { get; private set; }

    public event Action OnAuthenticated;

    public static bool IsAuthenticated { get; private set; }
    public static string PlayerId { get; private set; }
    public static string AccessToken { get; private set; }
    public static string PlayerName { get; set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task InitializeAndSignIn()
    {
        try
        {
            await UnityServices.InitializeAsync();
            Debug.Log("Unity Services initialized. Attempting sign in...");

            PlayerAccountService.Instance.SignedIn += OnPlayerAccountSignedIn;

            if (!PlayerAccountService.Instance.IsSignedIn)
            {
                await PlayerAccountService.Instance.StartSignInAsync();
                Debug.Log("StartSignInAsync completed. Waiting for browser callback...");
            }
            else
            {
                await SignInToAuthServiceAsync();
            }
        }
        catch (PlayerAccountsException ex)
        {
            Debug.LogError($"Player Account Sign-In Failed: {ex.Message}");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Request Failed during Init: {ex.Message}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Unity Services could not be initialised: {ex.Message}");
        }
    }

    private async void OnPlayerAccountSignedIn()
    {
        await SignInToAuthServiceAsync();
    }

    private async Task SignInToAuthServiceAsync()
    {
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(
                PlayerAccountService.Instance.AccessToken
            );

            IsAuthenticated = true;
            PlayerId        = AuthenticationService.Instance.PlayerId;
            AccessToken     = AuthenticationService.Instance.AccessToken;

            PlayerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            Debug.Log($"Auth successful. Player ID: {PlayerId}, Display Name: {PlayerName}");

            OnAuthenticated?.Invoke();
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"Auth Service Exchange Failed: {ex.Message}");
        }
    }
}