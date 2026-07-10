using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class SettlementScoreClashUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;

    [Header("Player Score (아래에서 등장)")]
    [SerializeField] private RectTransform playerScoreRect;
    [SerializeField] private TMP_Text playerScoreText;
    [SerializeField] private Vector2 playerStartAnchoredPos = new Vector2(0f, -600f);

    [Header("AI Score (위에서 등장)")]
    [SerializeField] private RectTransform aiScoreRect;
    [SerializeField] private TMP_Text aiScoreText;
    [SerializeField] private Vector2 aiStartAnchoredPos = new Vector2(0f, 600f);

    [Header("Timing")]
    [SerializeField] private Vector2 centerAnchoredPos = Vector2.zero;
    [SerializeField] private float contactGap = 90f;
    [SerializeField] private float growDuration = 0.6f;
    [SerializeField] private float growTargetScale = 1.3f;

    // 서로 겹치지 않도록, 가운데가 아니라 각자 나온 방향으로 살짝 떨어진 접촉 지점에서 만난다.
    private Vector2 PlayerMeetPos => centerAnchoredPos + (playerStartAnchoredPos - centerAnchoredPos).normalized * contactGap;
    private Vector2 AiMeetPos => centerAnchoredPos + (aiStartAnchoredPos - centerAnchoredPos).normalized * contactGap;

    [Header("Collide (부딪힘)")]
    [SerializeField] private float impactPunchStrength = 0.5f;
    [SerializeField] private float impactDuration = 0.15f;

    [Header("Push (진 쪽을 나온 방향으로 밀어냄)")]
    [SerializeField] private float pushDuration = 0.9f;
    [SerializeField] private float winnerAdvanceRatio = 0.35f;
    [SerializeField] private float loserKnockbackMultiplier = 1f;
    [SerializeField] private float hitReactionScale = 1.4f;
    [SerializeField] private float hitReactionDuration = 0.1f;
    [SerializeField] private float winPunchDuration = 0.4f;
    [SerializeField] private float endHoldDuration = 0.3f;

    private float displayedPlayerValue;
    private float displayedAiValue;

    public void PlayClash(float playerScore, float aiScore, Action onComplete)
    {
        if (root != null)
            root.SetActive(true);

        StartCoroutine(ClashRoutine(playerScore, aiScore, onComplete));
    }

    private IEnumerator ClashRoutine(float playerScore, float aiScore, Action onComplete)
    {
        SetupStartState(playerScore, aiScore);

        yield return GrowAndMoveToCenter(playerScore, aiScore);

        bool tie = Mathf.Approximately(playerScore, aiScore);
        bool playerWins = playerScore > aiScore;
        float diff = Mathf.Abs(playerScore - aiScore);
        float playerTarget = tie ? 0f : (playerWins ? diff : 0f);
        float aiTarget = tie ? 0f : (playerWins ? 0f : diff);

        // 부딪힌 직후부터 밀려나기가 끝날 때까지 이어지는 하나의 카운트다운 (유희왕 라이프포인트 연출)
        float countdownDuration = impactDuration + pushDuration;
        Tweener playerValueTween = DOTween.To(() => displayedPlayerValue, SetPlayerValue, playerTarget, countdownDuration).SetEase(Ease.OutExpo);
        Tweener aiValueTween = DOTween.To(() => displayedAiValue, SetAiValue, aiTarget, countdownDuration).SetEase(Ease.OutExpo);

        yield return CollideImpact();
        yield return PushLoserAway(playerScore, aiScore);

        yield return playerValueTween.WaitForCompletion();
        yield return aiValueTween.WaitForCompletion();

        yield return new WaitForSeconds(endHoldDuration);

        if (root != null)
            root.SetActive(false);

        onComplete?.Invoke();
    }

    private void SetupStartState(float playerScore, float aiScore)
    {
        playerScoreRect.gameObject.SetActive(true);
        aiScoreRect.gameObject.SetActive(true);

        playerScoreRect.anchoredPosition = playerStartAnchoredPos;
        aiScoreRect.anchoredPosition = aiStartAnchoredPos;

        playerScoreRect.localScale = Vector3.zero;
        aiScoreRect.localScale = Vector3.zero;

        // 등장할 때부터 실제 라운드 점수를 그대로 표시 (카운트업 없음)
        SetPlayerValue(playerScore);
        SetAiValue(aiScore);
    }

    private IEnumerator GrowAndMoveToCenter(float playerScore, float aiScore)
    {
        Sequence seq = DOTween.Sequence();

        // 일정한 속도로 눈에 보이게 다가오다가 부딪히는 느낌 (Linear)
        seq.Join(playerScoreRect.DOAnchorPos(PlayerMeetPos, growDuration).SetEase(Ease.Linear));
        seq.Join(playerScoreRect.DOScale(growTargetScale, growDuration).SetEase(Ease.OutBack));
        seq.Join(aiScoreRect.DOAnchorPos(AiMeetPos, growDuration).SetEase(Ease.Linear));
        seq.Join(aiScoreRect.DOScale(growTargetScale, growDuration).SetEase(Ease.OutBack));

        yield return seq.WaitForCompletion();
    }

    // 접촉 지점에서 부딪히는 충격만 주고, 바로 이긴 쪽 방향으로 밀리는 단계로 넘어간다.
    private IEnumerator CollideImpact()
    {
        playerScoreRect.anchoredPosition = PlayerMeetPos;
        aiScoreRect.anchoredPosition = AiMeetPos;

        Sequence impactSeq = DOTween.Sequence();
        impactSeq.Join(playerScoreRect.DOPunchScale(Vector3.one * impactPunchStrength, impactDuration, 10, 1f));
        impactSeq.Join(aiScoreRect.DOPunchScale(Vector3.one * impactPunchStrength, impactDuration, 10, 1f));
        yield return impactSeq.WaitForCompletion();
    }

    // 이긴(큰) 숫자가 진(작은) 숫자를 그 숫자가 나왔던 방향으로 밀어내면서 사라지게 하고,
    // 이긴 숫자는 그 방향으로 전진하며 남은 값(diff)까지 줄어든다.
    private IEnumerator PushLoserAway(float playerScore, float aiScore)
    {
        bool tie = Mathf.Approximately(playerScore, aiScore);

        if (tie)
        {
            Sequence tieSeq = DOTween.Sequence();
            tieSeq.Join(playerScoreRect.DOAnchorPos(playerStartAnchoredPos, pushDuration).SetEase(Ease.InCubic));
            tieSeq.Join(playerScoreRect.DOScale(0f, pushDuration).SetEase(Ease.InCubic));
            tieSeq.Join(aiScoreRect.DOAnchorPos(aiStartAnchoredPos, pushDuration).SetEase(Ease.InCubic));
            tieSeq.Join(aiScoreRect.DOScale(0f, pushDuration).SetEase(Ease.InCubic));
            yield return tieSeq.WaitForCompletion();

            playerScoreRect.gameObject.SetActive(false);
            aiScoreRect.gameObject.SetActive(false);
            yield break;
        }

        bool playerWins = playerScore > aiScore;

        RectTransform loserRect = playerWins ? aiScoreRect : playerScoreRect;
        RectTransform winnerRect = playerWins ? playerScoreRect : aiScoreRect;
        Vector2 loserStartPos = playerWins ? aiStartAnchoredPos : playerStartAnchoredPos;
        Vector2 winnerMeetPos = playerWins ? PlayerMeetPos : AiMeetPos;

        // 1보다 크면 진 쪽이 원래 나왔던 위치보다 더 멀리 튕겨나간다.
        Vector2 loserKnockbackPos = Vector2.LerpUnclamped(centerAnchoredPos, loserStartPos, loserKnockbackMultiplier);
        Vector2 winnerAdvancePos = Vector2.Lerp(winnerMeetPos, loserStartPos, winnerAdvanceRatio);

        // 맞는 순간 살짝 부풀었다가(피격) 곧바로 줄어들며 날아가는 스케일 연출
        float shrinkDuration = Mathf.Max(0.01f, pushDuration - hitReactionDuration);
        Sequence loserScaleSeq = DOTween.Sequence();
        loserScaleSeq.Append(loserRect.DOScale(growTargetScale * hitReactionScale, hitReactionDuration).SetEase(Ease.OutQuad));
        loserScaleSeq.Append(loserRect.DOScale(0f, shrinkDuration).SetEase(Ease.OutExpo));

        Sequence pushSeq = DOTween.Sequence();
        pushSeq.Join(loserRect.DOAnchorPos(loserKnockbackPos, pushDuration).SetEase(Ease.OutExpo));
        pushSeq.Join(loserScaleSeq);
        pushSeq.Join(winnerRect.DOAnchorPos(winnerAdvancePos, pushDuration).SetEase(Ease.OutExpo));

        yield return pushSeq.WaitForCompletion();

        loserRect.gameObject.SetActive(false);
        yield return winnerRect.DOPunchScale(Vector3.one * 0.3f, winPunchDuration, 8, 1f).WaitForCompletion();
    }

    private void SetPlayerValue(float value)
    {
        displayedPlayerValue = value;
        playerScoreText.text = Mathf.RoundToInt(value).ToString();
    }

    private void SetAiValue(float value)
    {
        displayedAiValue = value;
        aiScoreText.text = Mathf.RoundToInt(value).ToString();
    }
}
