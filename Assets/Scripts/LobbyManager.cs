using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyManager : MonoBehaviourPunCallbacks
{
    public TMP_InputField nicknameInput;
    public TMP_InputField roomNameInput;

    public GameObject menuPanel;
    public GameObject lobbyBrowserPanel;
    public GameObject browserContent;
    public GameObject roomContent;

    public Transform roomListContent;
    public GameObject roomItemPrefab;

    public TextMeshProUGUI roomTitleText;
    public TextMeshProUGUI playerCountText;
    public Button startGameButton;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public void OnClickQuickPlay()
    {
        SetNickname();
        PhotonNetwork.JoinRandomRoom();
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        string roomName = "Room" + Random.Range(100, 999);
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public void OnClickCreateRoom()
    {
        SetNickname();

        string roomName = roomNameInput.text;
        if (roomName == "")
        {
            roomName = "Room" + Random.Range(100, 999);
        }

        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;
        PhotonNetwork.CreateRoom(roomName, options);
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (Transform child in roomListContent)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < roomList.Count; i++)
        {
            if (roomList[i].RemovedFromList || !roomList[i].IsOpen)
            {
                continue;
            }

            GameObject item = Instantiate(roomItemPrefab, roomListContent);
            item.GetComponentInChildren<TextMeshProUGUI>().text = roomList[i].Name + " (" + roomList[i].PlayerCount + "/" + roomList[i].MaxPlayers + ")";

            string roomName = roomList[i].Name;
            item.GetComponent<Button>().onClick.AddListener(() => JoinRoom(roomName));
        }
    }

    public void JoinRoom(string roomName)
    {
        SetNickname();
        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        lobbyBrowserPanel.SetActive(true);
        browserContent.SetActive(false);
        roomContent.SetActive(true);

        roomTitleText.text = "\"" + PhotonNetwork.CurrentRoom.Name + "\"";
        playerCountText.text = PhotonNetwork.CurrentRoom.PlayerCount + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;

        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.gameObject.SetActive(true);
        }
        else
        {
            startGameButton.gameObject.SetActive(false);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        playerCountText.text = PhotonNetwork.CurrentRoom.PlayerCount + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        playerCountText.text = PhotonNetwork.CurrentRoom.PlayerCount + " / " + PhotonNetwork.CurrentRoom.MaxPlayers;

        if (PhotonNetwork.IsMasterClient)
        {
            startGameButton.gameObject.SetActive(true);
        }
    }

    public void OnClickLeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        roomContent.SetActive(false);
        browserContent.SetActive(true);
    }

    public void OnClickStartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("GameScene");
        }
    }

    private void SetNickname()
    {
        string nick = nicknameInput.text;
        if (nick == "")
        {
            nick = "Player" + Random.Range(100, 999);
        }
        PhotonNetwork.NickName = nick;
    }
}
