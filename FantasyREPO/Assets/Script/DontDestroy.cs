using UnityEngine;
using System.Collections.Generic;

public class DontDestroy : MonoBehaviour
{
    static List<string> dontDestroy = new List<string>();

    void Awake()
    {
        // 만약 파괴 예약 상태라면 스킵
        if (this == null || gameObject == null)
        {
            return;
        }

        // 같은 이름의 오브젝트가 이미 DontDestroy 처리됐다면 현재 오브젝트는 삭제
        if (dontDestroy.Contains(gameObject.name))
        {
            Destroy(gameObject);
            return;
        }

        // 중복이 아니면 리스트에 추가하고 파괴되지 않도록 설정
        dontDestroy.Add(gameObject.name);
        DontDestroyOnLoad(gameObject);
    }
}