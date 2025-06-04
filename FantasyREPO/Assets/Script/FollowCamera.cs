using UnityEngine;
using UnityEngine.Tilemaps;

public class FollowCamera : MonoBehaviour
{
    public float interpVelocity;
    public float minDistance;
    public float followDistance;
    public GameObject target;
    public Vector3 offset;
    Vector3 targetPos;

    // ✅ 타일맵을 경계로 사용
    public Tilemap tilemap;

    float camHalfWidth;
    float camHalfHeight;

    Vector2 minCameraPos;
    Vector2 maxCameraPos;

    void Start()
    {
        targetPos = transform.position;

        Camera cam = Camera.main;
        camHalfHeight = cam.orthographicSize;
        camHalfWidth = camHalfHeight * cam.aspect;

        // ✅ 타일맵 경계 계산
        if (tilemap != null)
        {
            Bounds bounds = tilemap.localBounds;

            minCameraPos = new Vector2(
                bounds.min.x + camHalfWidth,
                bounds.min.y + camHalfHeight
            );
            maxCameraPos = new Vector2(
                bounds.max.x - camHalfWidth,
                bounds.max.y - camHalfHeight
            );
        }
        else
        {
            Debug.LogWarning("타일맵이 지정되지 않았습니다.");
        }
    }

    void FixedUpdate()
    {
        if (target)
        {
            Vector3 posNoZ = transform.position;
            posNoZ.z = target.transform.position.z;

            Vector3 targetDirection = target.transform.position - posNoZ;
            interpVelocity = targetDirection.magnitude * 10f;

            targetPos = transform.position + (targetDirection.normalized * interpVelocity * Time.deltaTime);
            Vector3 nextPos = Vector3.Lerp(transform.position, targetPos + offset, 0.25f);

            // ✅ 타일맵 경계로 Clamp
            float clampedX = Mathf.Clamp(nextPos.x, minCameraPos.x, maxCameraPos.x);
            float clampedY = Mathf.Clamp(nextPos.y, minCameraPos.y, maxCameraPos.y);

            transform.position = new Vector3(clampedX, clampedY, nextPos.z);
        }
    }
}