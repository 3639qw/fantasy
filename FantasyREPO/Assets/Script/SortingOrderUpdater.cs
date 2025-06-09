using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SortingOrderUpdater : MonoBehaviour
{
    private SpriteRenderer _renderer;
    private int _defaultOrder;

    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
        _defaultOrder = _renderer.sortingOrder;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        
        if (collision.CompareTag("Tree"))
        {
            SpriteRenderer treeRenderer = collision.GetComponent<SpriteRenderer>();

            if (treeRenderer != null)
            {
                float playerY = transform.position.y;
                float treeY = collision.transform.position.y;
                
                Debug.Log(playerY + ", " + treeY);

                int treeOrder = treeRenderer.sortingOrder;

                if (playerY+0.5 > treeY)
                {
                    // 플레이어가 나무보다 위 → 나무보다 뒤 (낮은 값)
                    _renderer.sortingOrder = treeOrder - 1;
                }
                else
                {
                    // 플레이어가 나무보다 아래 또는 같음 → 나무보다 앞 (높은 값)
                    _renderer.sortingOrder = treeOrder + 1;
                }
            }
        }else if (collision.CompareTag("Bench") || collision.CompareTag("Signs"))
        {
            SpriteRenderer objRenderer = collision.GetComponent<SpriteRenderer>();

            if (objRenderer != null)
            {
                float playerY = transform.position.y;
                float treeY = collision.transform.position.y;
                
                Debug.Log(playerY + ", " + treeY);

                int treeOrder = objRenderer.sortingOrder;

                if (playerY+0.2 > treeY)
                {
                    // 플레이어가 나무보다 위 → 나무보다 뒤 (낮은 값)
                    _renderer.sortingOrder = treeOrder - 1;
                }
                else
                {
                    // 플레이어가 나무보다 아래 또는 같음 → 나무보다 앞 (높은 값)
                    _renderer.sortingOrder = treeOrder + 1;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Tree") || collision.CompareTag("Bench"))
        {
            // 기본값으로 복구
            _renderer.sortingOrder = _defaultOrder;
        }
    }
}