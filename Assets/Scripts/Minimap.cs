using UnityEngine;
using Photon.Pun;

public class Minimap : MonoBehaviour
{
    public RectTransform playerDot;
    public RectTransform mapRect;

    public float worldMinX = -14f;
    public float worldMaxX = 14f;
    public float worldMinY = -10f;
    public float worldMaxY = 17f;

    private Transform localPlayer;

    private void Update()
    {
        if (localPlayer == null)
        {
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].photonView.IsMine)
                {
                    localPlayer = players[i].transform;
                    break;
                }
            }
        }

        if (localPlayer != null && playerDot != null && mapRect != null)
        {
            float normX = Mathf.InverseLerp(worldMinX, worldMaxX, localPlayer.position.x);
            float normY = Mathf.InverseLerp(worldMinY, worldMaxY, localPlayer.position.y);

            float posX = (normX - 0.5f) * mapRect.rect.width;
            float posY = (normY - 0.5f) * mapRect.rect.height;

            playerDot.anchoredPosition = new Vector2(posX, posY);
        }
    }
}
