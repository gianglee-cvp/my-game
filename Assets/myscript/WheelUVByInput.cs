using UnityEngine;

public class WheelUVByInput : MonoBehaviour
{
    public float scrollSpeed = 1f;
    private Renderer rend;
    private Material wheelMaterial;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend != null)
        {
            // Cache material once to avoid repeated material instantiation checks per frame.
            wheelMaterial = rend.material;
        }
    }

    void Update()
    {
        float vertical = Input.GetAxis("Vertical");
        if (wheelMaterial != null && Mathf.Abs(vertical) > 0.01f)
        {
            float offset = wheelMaterial.mainTextureOffset.y
                         + Time.deltaTime * scrollSpeed * Mathf.Sign(-vertical);

            wheelMaterial.mainTextureOffset = new Vector2(0, offset);
        }
    }
}
