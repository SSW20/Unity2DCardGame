using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public class HandLayoutManager : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float cardSpacing = 80f;
    [SerializeField] private float maxAngle = 20f;
    [SerializeField] private float arcHeight = 30f;

    [Header("Aniamation")]
    [SerializeField] private float moveDuration  = 0.3f;


    // 각 카드의 원래 위치/회전 저장
    private List<RectTransform> cards = new List<RectTransform>();

    void Start()
    {
        UpdateLayout();
    }

    void Update()
    {
        
    }

    // Update() 제거 — 매 프레임 재계산 안함

    public void UpdateLayout()
    {
        cards.Clear();
        foreach (RectTransform child in transform)
        {
            CardUI cardUI = child.GetComponent<CardUI>();
            if (cardUI != null && cardUI.isAnimation) continue; 
            cards.Add(child);
        }
           
        
        int count = cards.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0 : (float)i / (count - 1) - 0.5f;

            float x = t * cardSpacing * count;
            float y = -(t * t) * arcHeight * count;
            float angle = -t * maxAngle * 2;

            Vector3 targetPos = new Vector3(x, y, 0);
            Quaternion targetRot = Quaternion.Euler(0, 0, angle);
            CardUI cardUI = cards[i].GetComponent<CardUI>();
            if (cardUI != null)
            {
                // 호버 중이어도 "정답 위치"는 항상 최신으로 갱신
                cardUI.homeLocalPosition = targetPos;
                cardUI.homeLocalRotation = targetRot;

                if (cardUI.bIsHover) continue;   // 호버 중인 카드는 트윈으로 안 끌어당김
            }

            cards[i].DOKill();
            cards[i].DOLocalMove(targetPos, moveDuration).SetEase(Ease.OutCubic);
            cards[i].DOLocalRotateQuaternion(targetRot, moveDuration).SetEase(Ease.OutCubic);
        }
    }
}