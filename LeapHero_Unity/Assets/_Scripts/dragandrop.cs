using UnityEngine;

public class DragAndDrop: MonoBehaviour
{
    [SerializeField] private Camera cam;
    private Vector3 offset;

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;
    }

    void OnMouseDown()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -cam.transform.position.z;

        Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);
        offset = transform.position - worldPos;
    }

    void OnMouseDrag()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -cam.transform.position.z;

        Vector3 worldPos = cam.ScreenToWorldPoint(mousePos);
        transform.position = worldPos + offset;
    }
}
