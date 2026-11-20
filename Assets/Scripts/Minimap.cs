using UnityEngine;

public class Minimap : MonoBehaviour
{
    public RectTransform playerDot;
    public RectTransform mapRect;

    public float worldMinX = -14.33f;
    public float worldMaxX = 14.33f;
    public float worldMinY = -10f;
    public float worldMaxY = 17.33f;

    private void Update()
    {
        if (PlayerController.Local != null && playerDot != null && mapRect != null)
        {
            Vector3 pos = PlayerController.Local.transform.position;
            float normX = Mathf.InverseLerp(worldMinX, worldMaxX, pos.x);
            float normY = Mathf.InverseLerp(worldMinY, worldMaxY, pos.y);

            float posX = (normX - 0.5f) * mapRect.rect.width;
            float posY = (normY - 0.5f) * mapRect.rect.height;

            playerDot.anchoredPosition = new Vector2(posX, posY);
        }
    }
}
