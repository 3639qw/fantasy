using UnityEngine;

[RequireComponent(typeof(Animator))]
public class PlayerAttackController : MonoBehaviour
{

    private Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            AttackTowardsMouse();
        }
        if (Input.GetKeyDown(KeyCode.P))
        {
            PickTowardsMouse();
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            AxeTowardsMouse();
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            HoeTowardsMouse();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            WateringTowardsMouse();
        }
    }

    void AttackTowardsMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 attackDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;

        attackDirection.Normalize();

        animator.SetFloat("AttackX", attackDirection.x);
        animator.SetFloat("AttackY", attackDirection.y);

        animator.SetTrigger("Bow");
    }
    void PickTowardsMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 attackDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;

        attackDirection.Normalize();

        animator.SetFloat("AttackX", attackDirection.x);
        animator.SetFloat("AttackY", attackDirection.y);

        animator.SetTrigger("Pick");
    }
    void AxeTowardsMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 attackDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;

        attackDirection.Normalize();

        animator.SetFloat("AttackX", attackDirection.x);
        animator.SetFloat("AttackY", attackDirection.y);

        animator.SetTrigger("Axe");
    }
    void HoeTowardsMouse()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 attackDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;

        attackDirection.Normalize();

        animator.SetFloat("AttackX", attackDirection.x);
        animator.SetFloat("AttackY", attackDirection.y);

        animator.SetTrigger("Hoe");
    }
    void WateringTowardsMouse()
    { 
        Vector3 mouseScreenPos = Input.mousePosition;
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(mouseScreenPos);
        Vector2 attackDirection = (Vector2)mouseWorldPos - (Vector2)transform.position;

        attackDirection.Normalize();

        animator.SetFloat("AttackX", attackDirection.x);
        animator.SetFloat("AttackY", attackDirection.y);

        animator.SetTrigger("Watering");
    }
}