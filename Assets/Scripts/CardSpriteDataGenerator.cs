using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class CardSpriteDataGenerator : Editor
{
    [MenuItem("Tools/Generate CardSpriteData")]
    public static void Generate()
    {        
        CardImageData data = ScriptableObject.CreateInstance<CardImageData>();
        data.cardSprites = new List<CardImageData.CardSprite>();

        var suits = new Dictionary<CardSuit, string>
        {
            { CardSuit.Spade, "spades" },
            { CardSuit.Heart, "hearts" },
            { CardSuit.Diamond, "diamonds" },
            { CardSuit.Club, "clubs" }
        };

        var ranks = new Dictionary<CardRank, string>
        {
            { CardRank.Ace, "ace" },
            { CardRank.Two, "2" },
            { CardRank.Three, "3" },
            { CardRank.Four, "4" },
            { CardRank.Five, "5" },
            { CardRank.Six, "6" },
            { CardRank.Seven, "7" },
            { CardRank.Eight, "8" },
            { CardRank.Nine, "9" },
            { CardRank.Ten, "10" },
            { CardRank.Jack, "jack" },
            { CardRank.Queen, "queen" },
            { CardRank.King, "king" }
        };

        foreach (var suit in suits)
        {
            foreach (var rank in ranks)
            {
                string path = $"Assets/Resources/CardSprites/sprites/simple/cards_simple_{suit.Value}_{rank.Value}.png";
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    CardImageData.CardSprite cardSprite = new CardImageData.CardSprite
                    {
                        suit = suit.Key,
                        rank = rank.Key,
                        sprite = sprite
                    };
                    data.cardSprites.Add(cardSprite);
                }
                else
                {
                    Debug.LogWarning($"Sprite not found at path: {path}");
                }
            }
        }
        
        // 메모리의 ScriptableObject를 .asset 파일로 저장
        AssetDatabase.CreateAsset(data, "Assets/CardImageData.asset");
        AssetDatabase.SaveAssets();
        Debug.Log($"완료! 총 {data.cardSprites.Count}장 로드됨");
        
    }
}
