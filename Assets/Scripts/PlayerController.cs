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
    public float mapMinX = -14.33f;
    public float mapMaxX = 14.33f;
    public float mapMinY = -10f;
    public float mapMaxY = 17.33f;
    public GameObject flashlight;
    public GameObject backpackLight;
    public GameObject ghostAura;
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
        UpdateLights();
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
            float camVert = Camera.main.orthographicSize;
            float camHoriz = camVert * Camera.main.aspect;
            float minCamX = mapMinX + camHoriz;
            float maxCamX = mapMaxX - camHoriz;
            float minCamY = mapMinY + camVert;
            float maxCamY = mapMaxY - camVert;

            float x = transform.position.x;
            if (minCamX < maxCamX)
            {
                x = Mathf.Clamp(x, minCamX, maxCamX);
            }
            else
            {
                x = (mapMinX + mapMaxX) * 0.5f;
            }

            float y = transform.position.y;
            if (minCamY < maxCamY)
            {
                y = Mathf.Clamp(y, minCamY, maxCamY);
            }
            else
            {
                y = (mapMinY + mapMaxY) * 0.5f;
            }

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

        UpdateLights();
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
            UpdateLights();
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
            UpdateLights();
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

        UpdateLights();

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

    private void UpdateLights()
    {
        if (backpackLight != null && backpackLight.activeSelf != isHunter)
        {
            backpackLight.SetActive(isHunter);
        }

        if (ghostAura != null)
        {
            bool showAura = !isHunter && !isTransformed && !isCaught;
            if (ghostAura.activeSelf != showAura)
            {
                ghostAura.SetActive(showAura);
            }
        }

        if (flashlight != null)
        {
            if (flashlight.activeSelf != isHunter)
            {
                flashlight.SetActive(isHunter);
            }

            if (isHunter)
            {
                Vector2 dir = Vector2.zero;
                if (joystick != null && (photonView.IsMine || !PhotonNetwork.IsConnected))
                {
                    dir = new Vector2(joystick.Horizontal(), joystick.Vertical());
                }
                else if (rb != null)
                {
                    dir = rb.linearVelocity;
                }

                if (dir.sqrMagnitude > 0.05f)
                {
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
                    flashlight.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3((mapMinX + mapMaxX) * 0.5f, (mapMinY + mapMaxY) * 0.5f, 0f);
        Vector3 size = new Vector3(mapMaxX - mapMinX, mapMaxY - mapMinY, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
