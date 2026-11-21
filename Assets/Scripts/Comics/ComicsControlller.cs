using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using DG.Tweening;
public class ComicController : MonoBehaviour
{
    [Header("Настройки комикса")]
    [SerializeField] private List<ComicPage> pages = new List<ComicPage>();
    [SerializeField] private AudioSource audioSource;

    [Header("Анимация фреймов")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private Ease fadeEase = Ease.OutQuad;
    [SerializeField] private bool showFirstFrameOnStart = true;

    [Header("Анимация смены страниц")]
    [SerializeField] private float pageSlideDuration = 0.45f;
    [SerializeField] private Ease pageSlideEase = Ease.InExpo; // Резкое ускорение вниз

    [Header("События")]
    public UnityEvent onComicFinished;
    public event Action OnComicFinishedAction;

    private int _currentPageIndex = 0;
    private int _currentFrameIndex = -1;
    private bool _isFinished = false;
    private bool _isTransitioning = false;

    private void Start()
    {
        InitializeComic();

        if (showFirstFrameOnStart)
        {
            ShowNextFrame();
        }
    }

    private void Update()
    {
        if (_isFinished || _isTransitioning) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ShowNextFrame();
        }
    }

    private void InitializeComic()
    {
        for (int i = 0; i < pages.Count; i++)
        {
            if (pages[i].pageRect != null)
            {
                pages[i].pageRect.anchoredPosition = Vector2.zero;
                pages[i].pageRect.gameObject.SetActive(i == 0);
            }

            foreach (var frame in pages[i].frames)
            {
                if (frame.canvasGroup != null)
                {
                    frame.canvasGroup.alpha = 0f;
                }
            }
        }
    }

    public void ShowNextFrame()
    {
        if (_isFinished || _isTransitioning || pages.Count == 0) return;

        _currentFrameIndex++;

        // Если фреймы на текущей странице закончились — переходим к следующей
        if (_currentFrameIndex >= pages[_currentPageIndex].frames.Count)
        {
            SwitchToNextPage();
            return;
        }

        RevealCurrentFrame();
    }

    private void RevealCurrentFrame()
    {
        ComicFrame currentFrame = pages[_currentPageIndex].frames[_currentFrameIndex];

        if (currentFrame.canvasGroup != null)
        {
            currentFrame.canvasGroup.DOKill();
            currentFrame.canvasGroup
                .DOFade(1f, fadeDuration)
                .SetEase(fadeEase)
                .SetLink(currentFrame.canvasGroup.gameObject);
        }

        PlayFrameAudio(currentFrame.audioClip);
    }

    private void SwitchToNextPage()
    {
        int nextPageIndex = _currentPageIndex + 1;

        // Если страницы закончились — завершаем комикс
        if (nextPageIndex >= pages.Count)
        {
            FinishComic();
            return;
        }

        _isTransitioning = true;

        // Останавливаем звук текущей страницы во время перелистывания
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        RectTransform oldPage = pages[_currentPageIndex].pageRect;
        RectTransform newPage = pages[nextPageIndex].pageRect;

        // Включаем новую страницу в центре
        if (newPage != null)
        {
            newPage.anchoredPosition = Vector2.zero;
            newPage.gameObject.SetActive(true);
        }

        _currentPageIndex = nextPageIndex;
        _currentFrameIndex = 0;

        if (oldPage != null)
        {
            // Ставим старую страницу поверх новой, чтобы она уезжала вниз и открывала её
            oldPage.SetAsLastSibling();

            // Высота самой страницы (с небольшим запасом), чтобы гарантированно ушла за экран
            float slideDistance = oldPage.rect.width > 0 ? oldPage.rect.width + 50f : Screen.width;

            oldPage.DOKill();
            oldPage.DOAnchorPosX(-slideDistance, pageSlideDuration)
                .SetEase(Ease.OutFlash)
                .SetLink(oldPage.gameObject)
                .OnComplete(() =>
                {
                    oldPage.gameObject.SetActive(false);
                    _isTransitioning = false;
                    RevealCurrentFrame(); // Сразу открываем 1-й фрейм новой страницы
                });
        }
        else
        {
            _isTransitioning = false;
            RevealCurrentFrame();
        }
    }

    private void PlayFrameAudio(AudioClip clip)
    {
        if (audioSource == null) return;

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void FinishComic()
    {
        _isFinished = true;

        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        onComicFinished?.Invoke();
        OnComicFinishedAction?.Invoke();
    }
}