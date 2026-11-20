using UnityEngine;
using Photon.Pun;
using Unity.Cinemachine;

public class PlayerController : MonoBehaviourPun
{
    public static PlayerController Local { get; private set; }

    public float moveSpeed = 10f;
    public Rigidbody2D rb;
    public bool isHunter;
    public bool isCaught = false;
    public bool canMove = false;
    public Sprite[] propSprites;
    public GameObject flashlight;
    public GameObject backpackLight;
    public GameObject ghostAura;
    private Joystick joystick;
    private SpriteRenderer sr;
    private Animator animator;
    private bool isTransformed = false;
    private Vector3 lastPos;

    private void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        lastPos = transform.position;

        if (sr != null)
        {
            sr.color = Color.white;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }

        if (photonView.IsMine)
        {
            Local = this;
            joystick = FindFirstObjectByType<Joystick>();

            CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();
            if (vcam != null)
            {
                vcam.Follow = transform;
            }
        }
    }

    private void OnDestroy()
    {
        if (Local == this)
        {
            Local = null;
        }
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
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
        if (isTransformed || isCaught)
        {
            if (animator != null && animator.enabled)
            {
                animator.enabled = false;
            }
            return;
        }

        if (animator != null && !animator.enabled)
        {
            animator.enabled = true;
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

        if (animator != null)
        {
            animator.SetBool("isHunter", isHunter);
            animator.SetBool("isMoving", isMoving);
        }
    }

    [PunRPC]
    public void SetRole(bool hunter)
    {
        isHunter = hunter;
        isTransformed = false;

        if (sr != null)
        {
            sr.color = Color.white;
        }

        if (animator != null)
        {
            animator.enabled = true;
            animator.SetBool("isHunter", isHunter);
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
        if (propIndex < 0)
        {
            isTransformed = false;
            if (animator != null)
            {
                animator.enabled = true;
            }
            UpdateLights();
            return;
        }

        if (propSprites != null && propIndex >= 0 && propIndex < propSprites.Length)
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
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

        if (animator != null)
        {
            animator.enabled = false;
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
                if (joystick != null && photonView.IsMine)
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
}
