using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(menuName = "Card/CardSpriteData")]
public class CardImageData : ScriptableObject
{
    // Start is called before the first frame update
    [System.Serializable]
    public struct CardSprite
    {
        public CardSuit suit;
        public CardRank rank;
        public Sprite sprite;
    }

    public List<CardSprite> cardSprites = new List<CardSprite>();
    
    public Sprite GetSprite(CardSuit suit, CardRank rank)
    {
        foreach (var cardSprite in cardSprites)
        {
            if(cardSprite.suit == suit & cardSprite.rank == rank)
            {
                return cardSprite.sprite;
            }
        }
        return null;
    }
}
