using UnityEngine;

public class FollowCamera : MonoBehaviour
{
	public float interpVelocity;
	public float minDistance;
	public float followDistance;
	public GameObject target;
	public Vector3 offset;
	Vector3 targetPos;

	void Start()
	{
		targetPos = transform.position;
	}
	void FixedUpdate()
	{
		if (target)
        {
			//---------------------------- 
			// 플레이어 Lerp
            Vector3 posNoZ = transform.position;
            posNoZ.z = target.transform.position.z;
            Vector3 targetDirection = (target.transform.position - posNoZ);
			interpVelocity = targetDirection.magnitude * 10f;
			targetPos = transform.position + (targetDirection.normalized * interpVelocity * Time.deltaTime);
			transform.position = Vector3.Lerp(transform.position, targetPos + offset, 0.25f);
			//---------------------------- 
		}
	}
}
