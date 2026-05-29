using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform handPanel;
    [SerializeField] private Transform deckPanel;
    [SerializeField] private Transform graveyardPanel;

    [SerializeField] private HandLayoutManager handLayoutManager;
    [SerializeField] private GameOverPannel gameOverPannel;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DrawCards());
    }

    private IEnumerator DrawCards(int count = 5)
    {
        for (int i = 0; i < count; i++)
        {
            AddCardToHand();
            yield return new WaitForSeconds(0.1f); // 0.1초 딜레이
        }
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

         if (Input.GetKeyDown(KeyCode.G))
         {
             gameOverPannel.Show();
         }
         if(Input.GetKeyDown(KeyCode.H))
        {
            gameOverPannel.Hide();
        }
    }
    private void RemoveCardFromHand()
    {
        for (int i = handPanel.childCount - 1; i >= handPanel.childCount - 5; i--)
        {
            if (i < 0) break;
            // 자식이 없으면 패스
            if (handPanel.childCount == 0) return;

            GameObject lastCard = handPanel.GetChild(i).gameObject;
            lastCard.GetComponent<CardUI>().isAnimation = true;

            Sequence seq = DOTween.Sequence();
            seq.Append(lastCard.transform.DOScale(Vector3.zero, 0.3f));
            seq.Join(lastCard.transform.DOMove(graveyardPanel.position, 0.3f).SetEase(Ease.InCubic));
            seq.AppendCallback(() => Destroy(lastCard));

            handLayoutManager.UpdateLayout();
        }
    }



    private void AddCardToHand()
    {
        GameObject newCard = Instantiate(cardPrefab, handPanel.transform);


        newCard.transform.position = deckPanel.position;
        newCard.transform.localScale = Vector3.one;

        handLayoutManager.UpdateLayout();
    }

    public void ButtonClicked()
    {
        Debug.Log("Button was clicked!");
    }
}
