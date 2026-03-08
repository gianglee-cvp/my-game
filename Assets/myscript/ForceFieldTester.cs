using UnityEngine;
using ProceduralForceField;

public class ForceFieldTester : MonoBehaviour
{
    void Start()
    {
        // TÃ¬m táº¥t cáº£ force field trong scene
        ProceduralForceFieldOverlay[] overlays =
            FindObjectsByType<ProceduralForceFieldOverlay>(FindObjectsSortMode.None);

        foreach (var overlay in overlays)
        {
            overlay.Trigger(overlay.transform.position);
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                ProceduralForceFieldOverlay overlay =
                    hit.collider.GetComponentInParent<ProceduralForceFieldOverlay>();

                if (overlay != null)
                {
                    overlay.Trigger(hit.point);
                }
            }
        }
    }
}
