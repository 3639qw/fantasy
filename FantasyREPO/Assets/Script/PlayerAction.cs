using UnityEngine;

public class PlayerAction : MonoBehaviour
{
    public Animator ToolAnimator; // Player가 아닌 아이템 Sprite의 Animator
    public PlayerMove playerMove;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayWaterAnimation();
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayPickaxeAnimation();
        }
    }

    void PlayTool(int index)
    {
        ToolAnimator.ResetTrigger("ToolAction");
        ToolAnimator.SetInteger("ToolIndex", index);
        ToolAnimator.SetTrigger("ToolAction");
    }

    void PlayPickaxeAnimation()
    {
        Vector2 dir = playerMove.LastDirection;

        if (dir == Vector2.down)
            PlayTool(0); // Hoe_Down
        else if (dir == Vector2.up)
            PlayTool(1); // Hoe_Up
        else if (dir == Vector2.left)
            PlayTool(2); // Hoe_Left
        else if (dir == Vector2.right)
            PlayTool(3); // Hoe_Right
    }

    void PlayWaterAnimation()
    {
        Vector2 dir = playerMove.LastDirection;

        if (dir == Vector2.down)
            PlayTool(4); // Water_Front
        else if (dir == Vector2.right)
            PlayTool(5); // Water_Right
    }
}
