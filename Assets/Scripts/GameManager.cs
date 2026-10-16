using UnityEngine;
using TMPro;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject rolePanel;
    public TextMeshProUGUI roleText;

    private void Start()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 30;

        if (PhotonNetwork.IsConnectedAndReady)
        {
            Vector2 spawnPos = new Vector2(Random.Range(-4f, 4f), Random.Range(-2f, 2f));
            PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);

            if (PhotonNetwork.IsMasterClient)
            {
                Invoke("AssignRoles", 1f);
            }
        }
    }

    private void AssignRoles()
    {
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        if (allPlayers.Length > 0)
        {
            int hunterIndex = Random.Range(0, allPlayers.Length);

            for (int i = 0; i < allPlayers.Length; i++)
            {
                bool isHunter = (i == hunterIndex);
                allPlayers[i].photonView.RPC("SetRole", RpcTarget.AllBuffered, isHunter);
            }
        }
    }

    public void ShowRoleScreen(bool isHunter)
    {
        rolePanel.SetActive(true);

        if (isHunter)
        {
            roleText.text = "You are the Hunter!";
        }
        else
        {
            roleText.text = "You are a Prop!";
        }

        Invoke("StartGame", 5f);
    }

    private void StartGame()
    {
        rolePanel.SetActive(false);

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.IsMine)
            {
                players[i].canMove = true;
            }
        }
    }
}
