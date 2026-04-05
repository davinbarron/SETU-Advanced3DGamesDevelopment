using Fusion;
using System;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using UnityEngine;

// Class to manage Unity Services
public class UnityServiceManager : MonoBehaviour
{
    private static bool   signInComplete;
    private static string _playerName;
    private static bool   _signInInProgress;

    private bool _lobbyBootstrapped;

    public static string PlayerId    => AuthenticationService.Instance.PlayerId;
    public static string AccessToken => AuthenticationService.Instance.AccessToken;
    
    public static string PlayerName
    {
        get => _playerName;
        set => _playerName = value;
    }

    private async void Awake()
    {
        try
        {
            // Initialize Unity Services
            await UnityServices.InitializeAsync();

            // Avoid duplicate subscriptions when this object is recreated.
            PlayerAccountService.Instance.SignedIn -= SignedIn;
            PlayerAccountService.Instance.SignedIn += SignedIn;
            if (PlayerAccountService.Instance.IsSignedIn)
            {
                // If already signed into Player Accounts, continue auth flow immediately.
                SignedIn();
                return;
            }

            try
            {
                await PlayerAccountService.Instance.StartSignInAsync();
            }
            catch (PlayerAccountsException ex)
            {
                // Compare error code to PlayerAccountsErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }
            catch (RequestFailedException ex)
            {
                // Compare error code to CommonErrorCodes
                // Notify the player with the proper error message
                Debug.LogException(ex);
            }

        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error initializing Unity Services: {ex.Message}");
        }
    }

    private void OnDestroy()
    {
        if (PlayerAccountService.Instance != null)
        {
            PlayerAccountService.Instance.SignedIn -= SignedIn;
        }
    }

    private async void SignedIn()
    {
        if (_lobbyBootstrapped)
            return;

        if (_signInInProgress)
            return;

        _signInInProgress = true;
        signInComplete = false;

        try
        {
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
                Debug.Log("SignIn is successful.");
            }
            else
            {
                Debug.Log("Already signed in to Unity Authentication. Skipping re-authentication.");
            }

            signInComplete = true;

            // Fetch the human-friendly display name from Unity Player Accounts.
            _playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            Debug.Log($"Player name: {_playerName}");
        }
        catch (AuthenticationException ex)
        {
            // Compare error code to AuthenticationErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        catch (RequestFailedException ex)
        {
            // Compare error code to CommonErrorCodes
            // Notify the player with the proper error message
            Debug.LogException(ex);
        }
        finally
        {
            _signInInProgress = false;
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[UnityServicesManager] Authentication did not complete. Lobby startup aborted.");
            return;
        }

        var fusionBootstrap = FindFirstObjectByType<FusionBootstrap>(FindObjectsInactive.Include);
        var lobbyManager    = FindFirstObjectByType<LobbyManager>(FindObjectsInactive.Include);

        if (lobbyManager == null)
        {
            Debug.LogError("[UnityServicesManager] No LobbyManager found in the scene.");
            return;
        }

        _lobbyBootstrapped = true;
        lobbyManager.ShowLobby(
            fusionBootstrap,
            AuthenticationService.Instance.PlayerId,
            AuthenticationService.Instance.AccessToken);
    }
}
