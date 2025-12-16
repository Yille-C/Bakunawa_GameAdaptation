using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI Panels")]
    public GameObject connectPanel;
    public GameObject lobbyPanel;
    public GameObject modeSelectionPanel; // Your Single/Multi selection panel

    [Header("UI Inputs")]
    public InputField playerNameInput;
    public InputField roomCodeInput;
    public Text errorText;

    void Start()
    {
        // 1. Connect to Photon as soon as the menu loads (in background)
        Debug.Log("Connecting to Photon...");
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Server!");
        PhotonNetwork.AutomaticallySyncScene = true; // Sync scene loading for all
    }

    // --- BUTTON FUNCTIONS ---

    public void OnCreateGameClicked()
    {
        string playerName = playerNameInput.text;
        if (string.IsNullOrEmpty(playerName)) { errorText.text = "Enter Name!"; return; }

        PhotonNetwork.NickName = playerName;

        // Generate Random 4-Letter Code
        string roomCode = GenerateRoomCode();

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;

        PhotonNetwork.CreateRoom(roomCode, options);
    }

    public void OnJoinGameClicked()
    {
        string playerName = playerNameInput.text;
        string roomCode = roomCodeInput.text.ToUpper();

        if (string.IsNullOrEmpty(playerName)) { errorText.text = "Enter Name!"; return; }
        if (string.IsNullOrEmpty(roomCode)) { errorText.text = "Enter Code!"; return; }

        PhotonNetwork.NickName = playerName;
        PhotonNetwork.JoinRoom(roomCode);
    }

    // --- PHOTON CALLBACKS ---

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);

        // Switch UI
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        errorText.text = "Error: " + message;
    }

    // --- HELPER ---
    string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        string code = "";
        for (int i = 0; i < 4; i++)
        {
            code += chars[Random.Range(0, chars.Length)];
        }
        return code;
    }
}
