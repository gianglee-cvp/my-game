using UnityEngine;

public class WheelUVByInput : MonoBehaviour
{
    public float scrollSpeed = 1f;
    Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        float vertical = Input.GetAxis("Vertical");
        if (Mathf.Abs(vertical) > 0.01f)
        {
            float offset = rend.material.mainTextureOffset.y
                         + Time.deltaTime * scrollSpeed * Mathf.Sign(-vertical);

            rend.material.mainTextureOffset = new Vector2(0, offset);
        }
    }
}
