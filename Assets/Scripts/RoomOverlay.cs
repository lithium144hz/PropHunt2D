using UnityEngine;
using Photon.Pun;

public class RoomOverlay : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine && animator != null)
        {
            animator.SetBool("isInside", true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PhotonView pv = other.GetComponent<PhotonView>();
        if (pv != null && pv.IsMine && animator != null)
        {
            animator.SetBool("isInside", false);
        }
    }
}
