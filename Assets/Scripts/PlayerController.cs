using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 10f;
    public float boundary = 8f;
    public float dragSensitivity = 0.02f;

    [Header("デバッグ設定")]
    public bool isInvincible = false;

    private bool isDead = false;
    private float btnInput = 0f; // スマホボタン用 & ジョイスティック用 (-1: 左, 1: 右)
    private float dragInput = 0f; // ドラッグ操作用デリバティブ

    void Update()
    {
        if (isDead) return;

        float h = 0f;

        // キーボード入力
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed)
                h = -1f;
            else if (kb.rightArrowKey.isPressed || kb.dKey.isPressed)
                h = 1f;
        }

        // スマホ用ボタン・ジョイスティック入力があれば優先
        if (btnInput != 0f)
        {
            h = btnInput;
        }

        // 左右の移動（キーボード、ボタン、ジョイスティック）
        if (h != 0f)
        {
            Vector3 move = new Vector3(h * moveSpeed * Time.deltaTime, 0f, 0f);
            transform.Translate(move, Space.World);
        }

        // ドラッグ入力（スワイプ）
        if (dragInput != 0f)
        {
            Vector3 move = new Vector3(dragInput * dragSensitivity, 0f, 0f);
            transform.Translate(move, Space.World);
            dragInput = 0f; // 使用後にリセット
        }

        // 移動範囲制限
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -boundary, boundary);
        transform.position = pos;
    }

    public void SetBtnInput(float input)
    {
        btnInput = input;
    }

    public void SetDragInput(float input)
    {
        dragInput = input;
    }

    public void SetDead()
    {
        if (isInvincible) return; // 無敵なら死なない
        isDead = true;
    }

    public bool IsDead()
    {
        return isDead;
    }
}
