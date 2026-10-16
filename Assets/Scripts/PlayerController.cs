using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPun
{
    public float moveSpeed = 10f;
    public Rigidbody2D rb;
    public bool isHunter;
    public bool canMove = false;
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
        if (!photonView.IsMine || !canMove)
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
