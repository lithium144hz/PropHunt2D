using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    public float moveSpeed = 10f;
    public Rigidbody2D rb;
    public bool isHunter;
    public bool isCaught = false;
    public bool canMove = false;
    public Sprite[] propSprites;
    private Joystick joystick;

    private void Start()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (photonView.IsMine)
        {
            joystick = FindFirstObjectByType<Joystick>();
        }

        IgnoreOtherPlayers();
    }

    private void Update()
    {
        if (!photonView.IsMine || !canMove || isCaught)
        {
            return;
        }

        float moveX = 0f;
        float moveY = 0f;

        if (joystick != null)
        {
            moveX = joystick.Horizontal();
            moveY = joystick.Vertical();
        }

        Vector2 movement = new Vector2(moveX, moveY).normalized;
        rb.linearVelocity = movement * moveSpeed;

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
        }
    }

    [PunRPC]
    public void SetRole(bool hunter)
    {
        isHunter = hunter;

        if (photonView.IsMine)
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.ShowRoleScreen(isHunter);
            }
        }
    }

    [PunRPC]
    public void RpcChangeProp(int propIndex)
    {
        if (propSprites != null && propIndex >= 0 && propIndex < propSprites.Length)
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = propSprites[propIndex];
            }
        }
    }

    public void SelectProp(int propIndex)
    {
        photonView.RPC("RpcChangeProp", RpcTarget.AllBuffered, propIndex);
    }

    [PunRPC]
    public void RpcGetCaught()
    {
        isCaught = true;
        canMove = false;
        rb.linearVelocity = Vector2.zero;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, 0.3f);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            GameManager gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.CheckAllPropsCaught();
            }
        }
    }

    private void IgnoreOtherPlayers()
    {
        Collider2D myCol = GetComponent<Collider2D>();
        PlayerController[] allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        for (int i = 0; i < allPlayers.Length; i++)
        {
            if (allPlayers[i] != this)
            {
                Collider2D otherCol = allPlayers[i].GetComponent<Collider2D>();
                if (myCol != null && otherCol != null)
                {
                    Physics2D.IgnoreCollision(myCol, otherCol);
                }
            }
        }
    }
}
