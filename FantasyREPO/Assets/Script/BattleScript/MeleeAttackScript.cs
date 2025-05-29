using UnityEngine;  // UnityEngine ���ӽ����̽� �߰�

public class MeleeAttackScript : MonoBehaviour
{
    public float attackRange = 2f;  // ���� ���� (�� ũ��)
    public float attackAngle = 180f;  // ���� ���� (�⺻�� 180��)
    public Sprite attackRangeSprite;  // ���� ������ �ش��ϴ� �� ��������Ʈ

    void Update()
    {
        if (Input.GetMouseButtonDown(0))  // ���콺 ���� Ŭ��
        {
            PerformAttack();
        }
    }

  

    private void PerformAttack()
    {

        // 1. �÷��̾� �ֺ��� ������Ʈ�� ã�� (���� �� ��� ������Ʈ ����)
        Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(transform.position, attackRange);

        // ����� �α�: ������ ������Ʈ ���
        if (objectsInRange.Length == 0)
        {
            Debug.Log("���� ���� ���Ͱ� �����ϴ�.");
        }
        else
        {
            foreach (var obj in objectsInRange)
            {
                // �±װ� "Enemy"�� ������Ʈ�� ����
                if (obj.CompareTag("Enemy"))
                {
                    Debug.Log("������ ����: " + obj.name);
                }
            }
        }

        // ���� ���� ���Ͱ� �ִٸ� ���� ���� ���
        foreach (Collider2D obj in objectsInRange)
        {
            // �±װ� "Enemy"�� ������Ʈ�� ����
            if (obj.CompareTag("Enemy"))
            {
                Debug.Log("���� ������.");
                // ������ ��ġ�� �÷��̾��� ��ġ�� �������� ������ ���
                Vector2 directionToMonster = (obj.transform.position - transform.position).normalized;

                // ���콺 ��ġ ���
                Vector2 directionToMouse = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position).normalized;

                // ���� ���
                float angle = Vector2.Angle(directionToMonster, directionToMouse);

                // ������ ���� ���� ���� ��� ����
                if (angle <= attackAngle / 2)
                {
                    Debug.Log("���� ���� ����: " + obj.name);
                    // ���� ó�� ���� �߰�
                }
                else
                {
                    Debug.Log("���� �� ���� ����");
                }
            }
        }

    }
}
