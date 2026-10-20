using UnityEngine;
using Photon.Pun;

public class RoomOverlay : MonoBehaviour
{
    public SpriteRenderer roofSprite;
    public float fadeSpeed = 5f;
    private float targetAlpha = 1f;

    private void Start()
    {
        if (roofSprite == null)
        {
            roofSprite = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        if (roofSprite != null)
        {
            Color c = roofSprite.color;
            if (!Mathf.Approximately(c.a, targetAlpha))
            {
                c.a = Mathf.MoveTowards(c.a, targetAlpha, Time.deltaTime * fadeSpeed);
                roofSprite.color = c;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            targetAlpha = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine)
        {
            targetAlpha = 1f;
        }
    }
}
