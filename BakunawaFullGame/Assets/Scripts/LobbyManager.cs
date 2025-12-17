using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Lobby UI")]
    public Text roomCodeText;
    public Text playerListText;
    public Button startGameButton;

    [Header("Role Buttons")]
    public Button btnBakunawa;
    public Button btnMandirigma;
    public Button btnTagapangalaga;
    public Button btnAlbularyo;

    private Dictionary<string, Button> roleButtons = new Dictionary<string, Button>();

    void Start()
    {
        // 1. SETUP & CLEANUP
        // We use RemoveAllListeners to ensure the Inspector didn't accidentally
        // have "OnStartGameClicked" assigned to these buttons.

        if (btnBakunawa)
        {
            roleButtons.Add("Bakunawa", btnBakunawa);
            btnBakunawa.onClick.RemoveAllListeners();
            btnBakunawa.onClick.AddListener(() => SelectRole("Bakunawa"));
        }

        if (btnMandirigma)
        {
            roleButtons.Add("Mandirigma", btnMandirigma);
            btnMandirigma.onClick.RemoveAllListeners();
            btnMandirigma.onClick.AddListener(() => SelectRole("Mandirigma"));
        }

        if (btnTagapangalaga)
        {
            roleButtons.Add("Tagapangalaga", btnTagapangalaga);
            btnTagapangalaga.onClick.RemoveAllListeners();
            btnTagapangalaga.onClick.AddListener(() => SelectRole("Tagapangalaga"));
        }

        if (btnAlbularyo)
        {
            roleButtons.Add("Albularyo", btnAlbularyo);
            btnAlbularyo.onClick.RemoveAllListeners();
            btnAlbularyo.onClick.AddListener(() => SelectRole("Albularyo"));
        }

        // Setup Start Game Button
        if (startGameButton)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(OnStartGameClicked);
            startGameButton.gameObject.SetActive(false); // Hide by default
        }
    }

    public override void OnJoinedRoom()
    {
        UpdateLobbyUI();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        UpdateLobbyUI();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) { UpdateLobbyUI(); }
    public override void OnPlayerLeftRoom(Player otherPlayer) { UpdateLobbyUI(); }

    void Update()
    {
        // Force text update loop
        if (PhotonNetwork.InRoom && roomCodeText != null)
        {
            if (roomCodeText.text.Contains("ABCD"))
                roomCodeText.text = "Room Code: " + PhotonNetwork.CurrentRoom.Name;
        }
    }

    void UpdateLobbyUI()
    {
        if (!PhotonNetwork.InRoom) return;

        // 1. Update Room Code
        if (roomCodeText != null) roomCodeText.text = "Room Code: " + PhotonNetwork.CurrentRoom.Name;

        // 2. List Players & Check Readiness
        bool allPlayersHaveRoles = true;

        if (playerListText != null)
        {
            playerListText.text = "List of Players:\n";
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                string role = "No Role";
                if (p.CustomProperties.ContainsKey("Role"))
                {
                    role = (string)p.CustomProperties["Role"];
                }
                else
                {
                    allPlayersHaveRoles = false;
                }
                playerListText.text += p.NickName + " - " + role + "\n";
            }
        }

        // 3. Lock Taken Roles
        foreach (var btn in roleButtons.Values) btn.interactable = true;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            if (p.CustomProperties.ContainsKey("Role"))
            {
                string pickedRole = (string)p.CustomProperties["Role"];
                if (roleButtons.ContainsKey(pickedRole))
                {
                    roleButtons[pickedRole].interactable = false;
                }
            }
        }

        // 4. Host Start Button Logic
        if (startGameButton != null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                startGameButton.gameObject.SetActive(true);
                // Host can only click start if EVERYONE (including themselves) has a role
                startGameButton.interactable = allPlayersHaveRoles;
            }
            else
            {
                startGameButton.gameObject.SetActive(false);
            }
        }
    }

    public void SelectRole(string role)
    {
        Debug.Log("Selected Role: " + role); // Debug check
        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props.Add("Role", role);
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public void OnStartGameClicked()
    {
        // DEBUG LOGS
        Debug.Log("--- START BUTTON CLICKED ---");
        Debug.Log("Am I Master Client? " + PhotonNetwork.IsMasterClient);
        Debug.Log("Am I In Room? " + PhotonNetwork.InRoom);

        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("ERROR: You are not the Host. You cannot start the game.");
            return;
        }

        if (PhotonNetwork.LevelLoadingProgress > 0 && PhotonNetwork.LevelLoadingProgress < 1)
        {
            Debug.LogWarning("Load already in progress. Ignoring.");
            return;
        }

        // Disable button to prevent double clicks
        if (startGameButton != null) startGameButton.interactable = false;

        Debug.Log("Attempting to load scene: MultiplayerGameScene");

        // This stops other messages from interfering with the load
        PhotonNetwork.IsMessageQueueRunning = false;

        PhotonNetwork.LoadLevel("MultiplayerGameScene");
    }
}