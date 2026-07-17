using UnityEngine;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public RectTransform joystickBackground;
    public RectTransform joystickKnob;
    public float dragRange = 100f;

    private Vector2 inputVector = Vector2.zero;

    public void OnDrag(PointerEventData eventData)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            joystickBackground, 
            eventData.position, 
            eventData.pressEventCamera, 
            out pos))
        {
            // アンカーが中央 (0.5, 0.5) なので、posはそのまま中心からの相対座標になる
            inputVector = new Vector2(pos.x, pos.y);
            if (inputVector.magnitude > dragRange)
            {
                inputVector = inputVector.normalized * dragRange;
            }

            if (joystickKnob != null)
            {
                joystickKnob.anchoredPosition = inputVector;
            }

            // 正規化した入力ベクトル（-1.0f から 1.0f の範囲にする）
            inputVector = inputVector / dragRange;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        inputVector = Vector2.zero;
        if (joystickKnob != null)
        {
            joystickKnob.anchoredPosition = Vector2.zero;
        }
    }

    public Vector2 GetInputAxis()
    {
        return inputVector;
    }
}
