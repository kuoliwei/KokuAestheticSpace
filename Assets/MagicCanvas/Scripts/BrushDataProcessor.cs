using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class BrushDataProcessor : MonoBehaviour
{
    [SerializeField] private List<ScratchCard> scratchCards;
    [SerializeField] private HandReferenceDotSpawner refDotSpawner; // �ѦҬ��I���

    //[SerializeField] HandParticleEffectSpawner spawner;
    public bool isRevealing = false;
    public void HandleBrushData(List<BrushData> dataList)
    {
        List<Vector2> screenPosList = new List<Vector2>(); // [�s�W] �����Ҧ���y�Ъ��ù���m
        foreach (var data in dataList)
        {
            if (data.point == null || data.point.Length < 2)
                continue;


            float x = data.point[0];
            //float y = 1f - data.point[1];            // JSON �ǨӬ����W�����I�A���ର���U�����I
            float y = data.point[1];            // �אּ�����P�_�A��������
            Vector2 uv = new Vector2(x, y);

            screenPosList.Add(new Vector2(x * Screen.width, y * Screen.height)); // [�s�W] �[�J�ù��y�вM��

            foreach (var card in scratchCards)
            {
                if (!card.isRevealing)
                {
                    card.EraseAtNormalizedUV(uv); // �C�@�ӥd�����B�z�o�Ө��I
                }
            }
        }

        // �X�X �e�����I�P�B �X�X //
        if (refDotSpawner != null)
        {
            if (screenPosList.Count > 0)
                refDotSpawner.SyncDotsToScreenPositions(screenPosList);
            else
                refDotSpawner.ClearAll();
        }

        //// [�s�W] ����h�ӯS�Ī���
        //if (scratchCards[0].isRevealing && screenPosList.Count > 0)
        //    spawner.SyncParticlesToScreenPositions(screenPosList); // �h�Ӥ�N�|���h�ӯS��
        //else
        //    spawner.ClearAll(); // �S�H�ɥ����P��
    }
}
