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
    public Sprite[] ghostFrames;
    public Sprite[] hunterWalkFrames;
    public Sprite[] hunterIdleFrames;
    public float animSpeed = 0.15f;
    public float idleAnimSpeed = 0.25f;
    public float minX = -5f;
    public float maxX = 5f;
    public float minY = -4.7f;
    public float maxY = 11.7f;
    private Joystick joystick;
    private SpriteRenderer sr;
    private float animTimer;
    private int animIndex;
    private bool isTransformed = false;
    private bool wasMoving = false;
    private Vector3 lastPos;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;
        }

        lastPos = transform.position;

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
        if (photonView.IsMine)
        {
            UpdateCamera();

            if (canMove && !isCaught)
            {
                float moveX = 0f;
                float moveY = 0f;

                if (joystick != null)
                {
                    moveX = joystick.Horizontal();
                    moveY = joystick.Vertical();
                }

                Vector2 movement = new Vector2(moveX, moveY).normalized;
                rb.linearVelocity = movement * moveSpeed;
            }
            else if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }

        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        if (sr == null)
        {
            return;
        }

        if (isTransformed || isCaught)
        {
            return;
        }

        bool isMoving = false;

        if (photonView.IsMine)
        {
            if (rb != null && rb.linearVelocity.sqrMagnitude > 0.05f)
            {
                isMoving = true;
                if (rb.linearVelocity.x < -0.1f)
                {
                    sr.flipX = true;
                }
                else if (rb.linearVelocity.x > 0.1f)
                {
                    sr.flipX = false;
                }
            }
        }
        else
        {
            float deltaX = transform.position.x - lastPos.x;
            if ((transform.position - lastPos).sqrMagnitude > 0.0001f)
            {
                isMoving = true;
            }

            if (deltaX < -0.005f)
            {
                sr.flipX = true;
            }
            else if (deltaX > 0.005f)
            {
                sr.flipX = false;
            }

            lastPos = transform.position;
        }

        if (isMoving != wasMoving)
        {
            wasMoving = isMoving;
            animIndex = 0;
            animTimer = 0f;
        }

        Sprite[] currentFrames = null;
        if (isHunter)
        {
            currentFrames = isMoving ? hunterWalkFrames : hunterIdleFrames;
        }
        else
        {
            currentFrames = ghostFrames;
        }

        if (currentFrames == null || currentFrames.Length == 0)
        {
            return;
        }

        float currentSpeed = (isHunter && !isMoving) ? idleAnimSpeed : animSpeed;

        animTimer += Time.deltaTime;
        if (animTimer >= currentSpeed)
        {
            animTimer = 0f;
            animIndex = (animIndex + 1) % currentFrames.Length;
        }

        if (animIndex >= currentFrames.Length)
        {
            animIndex = 0;
        }

        sr.sprite = currentFrames[animIndex];
    }

    private void UpdateCamera()
    {
        if (Camera.main != null)
        {
            float x = Mathf.Clamp(transform.position.x, minX, maxX);
            float y = Mathf.Clamp(transform.position.y, minY, maxY);
            Camera.main.transform.position = new Vector3(x, y, -10f);
        }
    }

    [PunRPC]
    public void SetRole(bool hunter)
    {
        isHunter = hunter;
        isTransformed = false;
        animIndex = 0;
        animTimer = 0f;

        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }

        if (sr != null)
        {
            sr.color = Color.white;
        }

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
        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }

        if (propIndex < 0)
        {
            isTransformed = false;
            animIndex = 0;
            animTimer = 0f;
            if (sr != null && ghostFrames != null && ghostFrames.Length > 0)
            {
                sr.sprite = ghostFrames[0];
            }
            return;
        }

        if (propSprites != null && propIndex >= 0 && propIndex < propSprites.Length)
        {
            if (sr != null)
            {
                sr.sprite = propSprites[propIndex];
                sr.flipX = false;
            }
            isTransformed = true;
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

        if (sr == null)
        {
            sr = GetComponent<SpriteRenderer>();
        }

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
