using UnityEngine;
using System.Collections.Generic;

public class HandLayoutManager : MonoBehaviour
{
    [Header("Layout")]
    [SerializeField] private float cardSpacing = 80f;
    [SerializeField] private float maxAngle = 20f;
    [SerializeField] private float arcHeight = 30f;
    [SerializeField] private float hoverLift = 20f;



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

    public void UpdateLayout(float HoverLift = 0f)
    {
        cards.Clear();
        foreach (RectTransform child in transform)
            cards.Add(child);

        int count = cards.Count;
        if (count == 0) return;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0 : (float)i / (count - 1) - 0.5f;

            float x = t * cardSpacing * count;
            float y = -(t * t) * arcHeight * count;
            float angle = -t * maxAngle * 2;

            if(HoverLift > 0f)
            {
                y += HoverLift;
                angle = 0f;
            }
            cards[i].localPosition = new Vector3(x, y + HoverLift, 0);
            cards[i].localRotation = Quaternion.Euler(0, 0, angle);
        }
    }
}