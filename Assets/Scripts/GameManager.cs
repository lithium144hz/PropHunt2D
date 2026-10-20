using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;

public class GameManager : MonoBehaviourPunCallbacks
{
    public GameObject rolePanel;
    public TextMeshProUGUI roleText;

    public GameObject topBar;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI phaseText;

    public GameObject gameOverPanel;
    public TextMeshProUGUI winnerText;

    public Button catchButton;
    public TextMeshProUGUI catchButtonText;

    public GameObject transformButton;
    public GameObject propSelectPanel;
    public GameObject minimap;

    private float currentTimer;
    private bool timerRunning = false;
    private int currentPhase = 0;
    private float catchCooldown = 0f;

    private void Start()
    {
        PhotonNetwork.SendRate = 30;
        PhotonNetwork.SerializationRate = 30;

        if (topBar != null)
        {
            topBar.SetActive(false);
        }

        if (minimap != null)
        {
            minimap.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (catchButton != null)
        {
            catchButton.gameObject.SetActive(false);
        }

        if (transformButton != null)
        {
            transformButton.SetActive(false);
        }

        if (propSelectPanel != null)
        {
            propSelectPanel.SetActive(false);
        }

        if (PhotonNetwork.IsConnectedAndReady)
        {
            Vector2 spawnPos = new Vector2(Random.Range(-2.5f, 2.5f), Random.Range(-8f, -6f));
            PhotonNetwork.Instantiate("Player", spawnPos, Quaternion.identity);

            if (PhotonNetwork.IsMasterClient)
            {
                Invoke("AssignRoles", 1f);
            }
        }
    }

    private void Update()
    {
        if (timerRunning)
        {
            currentTimer -= Time.deltaTime;
            if (currentTimer < 0)
            {
                currentTimer = 0;
            }

            int minutes = Mathf.FloorToInt(currentTimer / 60);
            int seconds = Mathf.FloorToInt(currentTimer % 60);
            if (timerText != null)
            {
                timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            }

            if (PhotonNetwork.IsMasterClient && currentTimer <= 0)
            {
                if (currentPhase == 1)
                {
                    photonView.RPC("RpcStartHuntingPhase", RpcTarget.AllBuffered);
                }
                else if (currentPhase == 2)
                {
                    photonView.RPC("RpcGameOver", RpcTarget.AllBuffered, "PROPS WIN!");
                }
            }
        }

        if (catchCooldown > 0)
        {
            catchCooldown -= Time.deltaTime;
            if (catchButton != null)
            {
                catchButton.interactable = false;
            }
            if (catchButtonText != null)
            {
                catchButtonText.text = Mathf.CeilToInt(catchCooldown).ToString();
            }
        }
        else if (catchButton != null && catchButton.gameObject.activeSelf)
        {
            catchButton.interactable = true;
            if (catchButtonText != null)
            {
                catchButtonText.text = "CATCH";
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
        rolePanel.transform.SetAsLastSibling();

        if (isHunter)
        {
            roleText.text = "You are the Hunter!";
        }
        else
        {
            roleText.text = "You are a Prop!";
        }

        Invoke("EndIntro", 5f);
    }

    private void EndIntro()
    {
        rolePanel.SetActive(false);

        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("RpcStartHidingPhase", RpcTarget.AllBuffered);
        }
    }

    [PunRPC]
    public void RpcStartHidingPhase()
    {
        currentPhase = 1;
        currentTimer = 30f;
        timerRunning = true;

        if (topBar != null)
        {
            topBar.SetActive(true);
        }

        if (minimap != null)
        {
            minimap.SetActive(true);
        }

        if (phaseText != null)
        {
            phaseText.text = "PROPS ARE HIDING...";
        }

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.IsMine)
            {
                players[i].canMove = !players[i].isHunter;

                if (!players[i].isHunter && transformButton != null)
                {
                    transformButton.SetActive(true);
                }
            }
        }
    }

    [PunRPC]
    public void RpcStartHuntingPhase()
    {
        currentPhase = 2;
        currentTimer = 120f;

        if (phaseText != null)
        {
            phaseText.text = "HUNTER IS RELEASED!";
        }

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.IsMine)
            {
                players[i].canMove = true;

                if (players[i].isHunter && catchButton != null)
                {
                    catchButton.gameObject.SetActive(true);
                }
            }
        }
    }

    public void OnClickCatch()
    {
        if (catchCooldown > 0)
        {
            return;
        }

        PlayerController myPlayer = null;
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.IsMine)
            {
                myPlayer = players[i];
                break;
            }
        }

        if (myPlayer == null || !myPlayer.isHunter)
        {
            return;
        }

        catchCooldown = 5f;

        for (int i = 0; i < players.Length; i++)
        {
            if (!players[i].isHunter && !players[i].isCaught)
            {
                float distance = Vector2.Distance(myPlayer.transform.position, players[i].transform.position);
                if (distance <= 2.2f)
                {
                    players[i].photonView.RPC("RpcGetCaught", RpcTarget.AllBuffered);
                }
            }
        }
    }

    public void OnClickTogglePropSelect()
    {
        if (propSelectPanel != null)
        {
            propSelectPanel.SetActive(!propSelectPanel.activeSelf);
        }
    }

    public void OnClickChooseProp(int propIndex)
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.IsMine && !players[i].isHunter)
            {
                players[i].SelectProp(propIndex);
                break;
            }
        }

        if (propSelectPanel != null)
        {
            propSelectPanel.SetActive(false);
        }
    }

    public void CheckAllPropsCaught()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        int remainingProps = 0;

        for (int i = 0; i < players.Length; i++)
        {
            if (!players[i].isHunter && !players[i].isCaught)
            {
                remainingProps++;
            }
        }

        if (remainingProps == 0)
        {
            photonView.RPC("RpcGameOver", RpcTarget.AllBuffered, "HUNTER WINS!");
        }
    }

    [PunRPC]
    public void RpcGameOver(string winner)
    {
        timerRunning = false;
        currentPhase = 3;

        if (topBar != null)
        {
            topBar.SetActive(false);
        }

        if (catchButton != null)
        {
            catchButton.gameObject.SetActive(false);
        }

        if (transformButton != null)
        {
            transformButton.SetActive(false);
        }

        if (propSelectPanel != null)
        {
            propSelectPanel.SetActive(false);
        }

        if (minimap != null)
        {
            minimap.SetActive(false);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (winnerText != null)
        {
            winnerText.text = winner;
        }

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].photonView.IsMine)
            {
                players[i].canMove = false;
            }
        }
    }

    public void OnClickReturnToLobby()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        PhotonNetwork.LoadLevel("LobbyScene");
    }
}
