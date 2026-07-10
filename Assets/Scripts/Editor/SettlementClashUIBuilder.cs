using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SettlementClashUIBuilder
{
    [MenuItem("Tools/UI/Create Settlement Score Clash UI")]
    public static void CreateSettlementClashUI()
    {
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("씬에 Canvas가 없습니다. 먼저 Canvas를 만들어주세요.");
            return;
        }

        GameObject panelGO = new GameObject("SettlementClashPanel", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(panelGO, "Create Settlement Clash Panel");
        panelGO.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        GameObject playerGO = CreateScoreText(panelRect, "PlayerScoreText", new Color(0.3f, 0.6f, 1f));
        GameObject aiGO = CreateScoreText(panelRect, "AiScoreText", new Color(1f, 0.4f, 0.4f));

        SettlementScoreClashUI clash = panelGO.AddComponent<SettlementScoreClashUI>();

        SerializedObject clashSO = new SerializedObject(clash);
        clashSO.FindProperty("root").objectReferenceValue = panelGO;
        clashSO.FindProperty("playerScoreRect").objectReferenceValue = playerGO.GetComponent<RectTransform>();
        clashSO.FindProperty("playerScoreText").objectReferenceValue = playerGO.GetComponent<TMP_Text>();
        clashSO.FindProperty("aiScoreRect").objectReferenceValue = aiGO.GetComponent<RectTransform>();
        clashSO.FindProperty("aiScoreText").objectReferenceValue = aiGO.GetComponent<TMP_Text>();
        clashSO.ApplyModifiedProperties();

        GameTurnManager turnManager = Object.FindObjectOfType<GameTurnManager>();
        if (turnManager != null)
        {
            SerializedObject turnSO = new SerializedObject(turnManager);
            turnSO.FindProperty("scoreClashUI").objectReferenceValue = clash;
            turnSO.ApplyModifiedProperties();
            Debug.Log("GameTurnManager의 Score Clash UI 필드에 자동 연결했습니다.");
        }
        else
        {
            Debug.LogWarning("씬에서 GameTurnManager를 찾지 못했습니다. Score Clash UI 필드에 수동으로 연결해주세요.");
        }

        panelGO.SetActive(false);

        Selection.activeGameObject = panelGO;
        EditorUtility.SetDirty(panelGO);
        EditorSceneManager.MarkSceneDirty(panelGO.scene);

        Debug.Log("SettlementClashPanel 생성 완료.");
    }

    private static GameObject CreateScoreText(RectTransform parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(400f, 150f);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = "0";
        text.fontSize = 96f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.fontStyle = FontStyles.Bold;

        return go;
    }
}
