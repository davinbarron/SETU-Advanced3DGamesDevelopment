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

            PlayerAccountService.Instance.SignedIn += SignedIn;
            if (PlayerAccountService.Instance.IsSignedIn)
            {
                // If the player is already signed into Unity Player Accounts, proceed directly to the Unity Authentication sign-in.
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

    private async void SignedIn()
    {
        signInComplete = false;
        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);
            signInComplete = true;
            Debug.Log("SignIn is successful.");

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

        var fusionBootstrap = FindFirstObjectByType<FusionBootstrap>(FindObjectsInactive.Include);
        var lobbyManager    = FindFirstObjectByType<LobbyManager>(FindObjectsInactive.Include);

        if (lobbyManager == null)
        {
            Debug.LogError("[UnityServicesManager] No LobbyManager found in the scene.");
            return;
        }

        lobbyManager.ShowLobby(
            fusionBootstrap,
            AuthenticationService.Instance.PlayerId,
            AuthenticationService.Instance.AccessToken);
    }
}
