using UnityEngine;

public class HandTrailController : MonoBehaviour
{
    private TrailRenderer trail;

    void Awake()
    {
        // ���o TrailRenderer
        trail = GetComponent<TrailRenderer>();

        // �w�]�����]�A�i�H�令 true�^
        //trail.enabled = false;
    }

    void Update()
    {
        //// �d�ҡG���U�ƹ�����~�Ұʴݼv
        //if (Input.GetMouseButtonDown(0))
        //{
        //    trail.Clear();      // �M���ª��ݼv
        //    trail.enabled = true;
        //}

        //// ��}�ƹ�����N����
        //if (Input.GetMouseButtonUp(0))
        //{
        //    trail.enabled = false;
        //}
    }
}
