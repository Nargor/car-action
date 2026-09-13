using UnityEngine;
using UnityEngine.EventSystems;

public class UIDragController : MonoBehaviour, IBeginDragHandler, IDragHandler, IPointerDownHandler
{
    [Header("Drag Settings")]
    public RectTransform targetPanel; // The entire window to move
    public RectTransform canvasRect;

    private Vector2 pointerOffset;

    void Start()
    {
        if (targetPanel == null)
            targetPanel = transform.parent as RectTransform;

        if (canvasRect == null)
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvasRect = canvas.transform as RectTransform;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetPanel != null)
            targetPanel.SetAsLastSibling(); // Bring to front
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (targetPanel == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetPanel,
            eventData.position,
            eventData.pressEventCamera,
            out pointerOffset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetPanel == null || canvasRect == null) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            // Move panel based on drag position and offset
            Vector2 newPos = localPoint - pointerOffset;

            // Clamp inside canvas bounds
            Vector2 minPos = canvasRect.rect.min + (targetPanel.rect.size * targetPanel.pivot);
            Vector2 maxPos = canvasRect.rect.max - (targetPanel.rect.size * (Vector2.one - targetPanel.pivot));

            newPos.x = Mathf.Clamp(newPos.x, minPos.x, maxPos.x);
            newPos.y = Mathf.Clamp(newPos.y, minPos.y, maxPos.y);

            targetPanel.localPosition = newPos;
        }
    }
}
