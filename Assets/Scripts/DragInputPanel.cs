using UnityEngine;
using UnityEngine.EventSystems;

public class DragInputPanel : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    private float deltaX = 0f;
    private float lastPointerX = 0f;
    private bool isDragging = false;

    public void OnPointerDown(PointerEventData eventData)
    {
        lastPointerX = eventData.position.x;
        isDragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        deltaX = eventData.position.x - lastPointerX;
        lastPointerX = eventData.position.x;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isDragging = false;
        deltaX = 0f;
    }

    public float GetDeltaX()
    {
        float d = deltaX;
        // 読んだらリセットして、Update間で重複加算を防ぐ
        deltaX = 0f;
        return d;
    }

    public bool IsDragging()
    {
        return isDragging;
    }
}
