using UnityEngine;
public class OneShotStartT003 : MonoBehaviour
{
    public AngelCopperQuestT003 angel;
    void Start()
    {
        if (angel) angel.StartQuest();
    }
}