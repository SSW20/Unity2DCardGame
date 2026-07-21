using UnityEngine;
using UnityEngine.UI;

public class CardSoundController : MonoBehaviour
{
    private static CardSoundController instance;

    [Header("Card Hover")]
    [SerializeField] private AudioClip[] hoverClips;
    [SerializeField, Range(0f, 1f)] private float hoverVolume = 0.18f;
    [SerializeField, Min(0f)] private float hoverMinInterval = 0.06f;

    [Header("Card Draw (Player / AI)")]
    [SerializeField] private AudioClip[] drawClips;
    [SerializeField, Range(0f, 1f)] private float drawVolume = 0.35f;
    [SerializeField, Min(0f)] private float drawStartOffset = 0.08f;

    [Header("Card Placed In Slot")]
    [SerializeField] private AudioClip[] placeClips;
    [SerializeField, Range(0f, 1f)] private float placeVolume = 0.8f;

    [Header("Card Transfer (Field To Deck / Grave)")]
    [SerializeField] private AudioClip[] transferClips;
    [SerializeField, Range(0f, 1f)] private float transferVolume = 0.3f;
    [SerializeField, Min(0f)] private float transferStartOffset = 0.08f;
    [SerializeField, Min(1)] private int maxTransferSoundsPerBurst = 2;
    [SerializeField, Min(0.01f)] private float transferBurstWindow = 0.5f;

    [Header("Joker Intro")]
    [SerializeField] private AudioClip[] jokerAppearClips;
    [SerializeField, Range(0f, 1f)] private float jokerAppearVolume = 0.7f;
    [SerializeField] private AudioClip[] jokerToDeckClips;
    [SerializeField, Range(0f, 1f)] private float jokerToDeckVolume = 0.35f;
    [SerializeField, Min(0f)] private float jokerToDeckStartOffset = 0.08f;

    [Header("Bloodlines UI Buttons")]
    [SerializeField] private AudioClip uiHoverClip;
    [SerializeField, Range(0f, 1f)] private float uiHoverVolume = 0.12f;
    [SerializeField] private AudioClip uiClickClip;
    [SerializeField, Range(0f, 1f)] private float uiClickVolume = 0.2f;

    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusicClip;
    [SerializeField, Range(0f, 1f)] private float backgroundMusicVolume = 0.1f;
    [SerializeField] private bool loopBackgroundMusic = true;

    private AudioSource hoverSource;
    private AudioSource drawSource;
    private AudioSource placeSource;
    private AudioSource transferSource;
    private AudioSource jokerAppearSource;
    private AudioSource jokerToDeckSource;
    private AudioSource uiHoverSource;
    private AudioSource uiClickSource;
    private AudioSource backgroundMusicSource;
    private bool backgroundMusicStarted;
    private float lastHoverPlayTime = float.NegativeInfinity;
    private float transferBurstStartTime = float.NegativeInfinity;
    private int transferSoundsInBurst;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;

        if (backgroundMusicClip == null)
            backgroundMusicClip = Resources.Load<AudioClip>("Audio/Music/voices_of_spring");

        hoverSource = CreateSource();
        drawSource = CreateSource();
        placeSource = CreateSource();
        transferSource = CreateSource();
        jokerAppearSource = CreateSource();
        jokerToDeckSource = CreateSource();
        uiHoverSource = CreateSource();
        uiClickSource = CreateSource();
        backgroundMusicSource = CreateSource();
        backgroundMusicSource.loop = loopBackgroundMusic;

        PreloadClips(hoverClips);
        PreloadClips(drawClips);
        PreloadClips(placeClips);
        PreloadClips(transferClips);
        PreloadClips(jokerAppearClips);
        PreloadClips(jokerToDeckClips);
        PreloadClip(uiHoverClip);
        PreloadClip(uiClickClip);
        PreloadClip(backgroundMusicClip);
    }

    private void Start()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            if (button != null && button.GetComponent<CardButtonSoundRelay>() == null)
                button.gameObject.AddComponent<CardButtonSoundRelay>();
        }
    }

    public static void PlayBackgroundMusic()
    {
        CardSoundController player = Resolve();
        if (player == null
            || player.backgroundMusicSource == null
            || player.backgroundMusicClip == null
            || player.backgroundMusicStarted)
        {
            return;
        }

        player.backgroundMusicStarted = true;
        player.backgroundMusicSource.Stop();
        player.backgroundMusicSource.clip = player.backgroundMusicClip;
        player.backgroundMusicSource.volume = player.backgroundMusicVolume;
        player.backgroundMusicSource.Play();
    }

    public static void PlayHover()
    {
        CardSoundController player = Resolve();
        if (player == null || Time.unscaledTime - player.lastHoverPlayTime < player.hoverMinInterval)
            return;

        player.lastHoverPlayTime = Time.unscaledTime;
        player.PlayRandom(player.hoverSource, player.hoverClips, player.hoverVolume);
    }

    public static void PlayDraw()
    {
        CardSoundController player = Resolve();
        player?.hoverSource.Stop();
        player?.PlayRandom(
            player.drawSource,
            player.drawClips,
            player.drawVolume,
            player.drawStartOffset);
    }

    public static void PlayPlace()
    {
        CardSoundController player = Resolve();
        player?.hoverSource.Stop();
        player?.PlayRandom(player.placeSource, player.placeClips, player.placeVolume);
    }

    public static void ResetTransferBurst()
    {
        CardSoundController player = Resolve();
        if (player == null)
            return;

        player.transferBurstStartTime = Time.unscaledTime;
        player.transferSoundsInBurst = 0;
    }

    public static void PlayCardTransfer()
    {
        CardSoundController player = Resolve();
        if (player == null || !player.TryConsumeTransferSound())
            return;

        player.hoverSource.Stop();
        player.drawSource.Stop();
        player.placeSource.Stop();
        player.PlayRandom(
            player.transferSource,
            player.transferClips,
            player.transferVolume,
            player.transferStartOffset);
    }

    public static void PlayJokerAppear()
    {
        CardSoundController player = Resolve();
        player?.PlayRandom(
            player.jokerAppearSource,
            player.jokerAppearClips,
            player.jokerAppearVolume);
    }

    public static void PlayJokerToDeck()
    {
        CardSoundController player = Resolve();
        if (player == null || !player.TryConsumeTransferSound())
            return;

        player?.PlayRandom(
            player.jokerToDeckSource,
            player.jokerToDeckClips,
            player.jokerToDeckVolume,
            player.jokerToDeckStartOffset);
    }

    public static void PlayUIHover()
    {
        CardSoundController player = Resolve();
        player?.PlayClip(player.uiHoverSource, player.uiHoverClip, player.uiHoverVolume);
    }

    public static void PlayUIClick()
    {
        CardSoundController player = Resolve();
        player?.PlayClip(player.uiClickSource, player.uiClickClip, player.uiClickVolume);
    }

    private bool TryConsumeTransferSound()
    {
        if (Time.unscaledTime - transferBurstStartTime > transferBurstWindow)
        {
            transferBurstStartTime = Time.unscaledTime;
            transferSoundsInBurst = 0;
        }

        if (transferSoundsInBurst >= maxTransferSoundsPerBurst)
            return false;

        transferSoundsInBurst++;
        return true;
    }

    private static CardSoundController Resolve()
    {
        if (instance == null)
            instance = FindFirstObjectByType<CardSoundController>();

        return instance;
    }

    private void PlayRandom(
        AudioSource source,
        AudioClip[] clips,
        float volume,
        float startOffset = 0f)
    {
        if (source == null || clips == null || clips.Length == 0)
            return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == null)
            return;

        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.time = Mathf.Clamp(startOffset, 0f, Mathf.Max(0f, clip.length - 0.001f));
        source.Play();
    }

    private void PlayClip(AudioSource source, AudioClip clip, float volume)
    {
        if (source == null || clip == null)
            return;

        source.Stop();
        source.clip = clip;
        source.volume = volume;
        source.time = 0f;
        source.Play();
    }

    private AudioSource CreateSource()
    {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        return source;
    }

    private static void PreloadClips(AudioClip[] clips)
    {
        if (clips == null)
            return;

        foreach (AudioClip clip in clips)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }
    }

    private static void PreloadClip(AudioClip clip)
    {
        if (clip != null && clip.loadState == AudioDataLoadState.Unloaded)
            clip.LoadAudioData();
    }
}
