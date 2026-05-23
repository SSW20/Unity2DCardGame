using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handPanel;
    [SerializeField] private HandLayoutManager handLayoutManager;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            AddCardToHand();
        }

         if (Input.GetKeyDown(KeyCode.B))
        {
            RemoveCardFromHand();
        }
    }
    private void RemoveCardFromHand()
    {
        // 자식이 없으면 패스
    if (handPanel.childCount == 0) return;

    // 마지막 카드 삭제
    GameObject lastCard = handPanel.GetChild(handPanel.childCount - 1).gameObject;
    Destroy(lastCard);
    handLayoutManager.UpdateLayout();
    }



    private void AddCardToHand()
    {
        GameObject newCard = Instantiate(cardPrefab, handPanel.transform);
        handLayoutManager.UpdateLayout();
    }

    public void ButtonClicked()
    {
        Debug.Log("Button was clicked!");
    }
}
