using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Cinemachine;
using DG.Tweening;
using Noir.Player;

public class DoorCutsceneSequence : MonoBehaviour
{
    [Header("1. Дверь и силуэт")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private Vector3 openDoorLocalEuler = new Vector3(0f, 95f, 0f);
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private GameObject silhouetteObject;

    [Header("2. Камера и управление")]
    [Tooltip("Перетащи сюда объект с PlayerLook (обычно виртуальная камера игрока).")]
    [SerializeField] private PlayerLook playerLook;
    [Tooltip("Перетащи сюда объект игрока с PlayerMovement.")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private float turnDuration = 0.85f;
    [SerializeField] private float turnYAngle = 180f;

    [Header("3. Фраза")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip voicePhraseClip;
    [SerializeField] private float delayBeforePhrase = 0.15f;
    [SerializeField] private float delayAfterPhrase = 0.2f;

    [Header("4. Вспышка молнии (свет в комнате + 2 окна)")]
    [SerializeField] private List<Light> lightningLights = new List<Light>();
    [SerializeField] private AudioClip lightningSound;
    [SerializeField] private float lightningPeakIntensity = 12f;
    [SerializeField] private float lightningDuration = 0.35f;

    [Header("5. Тёмный экран")]
    [SerializeField] private CanvasGroup blackScreenCanvasGroup;
    [SerializeField] private AudioClip blackScreenSound;
    [SerializeField] private float fadeToBlackDuration = 0.15f;

    [Header("События")]
    public UnityEvent onCutsceneFinished;
    public event Action OnCutsceneFinishedAction;

    private Sequence _cutsceneSequence;
    public bool IsPlaying => _cutsceneSequence.IsActive();

    private void Awake()
    {
        if (silhouetteObject != null)
            silhouetteObject.SetActive(false);

        if (blackScreenCanvasGroup != null)
        {
            blackScreenCanvasGroup.alpha = 0f;
            blackScreenCanvasGroup.gameObject.SetActive(false);
        }
    }

    public void PlayCutscene()
    {
        _cutsceneSequence?.Kill();

        // Выключаем движение и ввод мыши
        if (playerMovement != null)
            playerMovement.enabled = false;

        if (playerLook != null)
        {
            playerLook.enabled = false;
            // PlayerLook.OnDisable показывает курсор — скрываем обратно
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // 1. Мгновенно открываем дверь, включаем силуэт и звук двери
        if (doorPivot != null)
            doorPivot.localEulerAngles = openDoorLocalEuler;

        if (silhouetteObject != null)
            silhouetteObject.SetActive(true);

        if (audioSource != null && doorOpenSound != null)
            audioSource.PlayOneShot(doorOpenSound);

        _cutsceneSequence = DOTween.Sequence();

        // 2. Плавный разворот камеры на 180°
        if (playerLook != null)
        {
            // Если на камере висит CinemachinePanTilt — крутим его ось Pan
            if (playerLook.TryGetComponent<CinemachinePanTilt>(out var panTilt))
            {
                float targetPan = panTilt.PanAxis.Value + turnYAngle;
                _cutsceneSequence.Append(
                    DOTween.To(
                        () => panTilt.PanAxis.Value,
                        x => panTilt.PanAxis.Value = x,
                        targetPan,
                        turnDuration
                    ).SetEase(Ease.InOutQuad)
                );
            }
            else
            {
                // Запасной вариант: крутим сам Transform виртуальной камеры
                Vector3 targetRot = playerLook.transform.eulerAngles + new Vector3(0f, turnYAngle, 0f);
                _cutsceneSequence.Append(
                    playerLook.transform.DORotate(targetRot, turnDuration)
                        .SetEase(Ease.InOutQuad)
                );
            }
        }

        if (delayBeforePhrase > 0f)
            _cutsceneSequence.AppendInterval(delayBeforePhrase);

        // 3. Запуск фразы и ожидание её окончания
        float phraseDuration = voicePhraseClip != null ? voicePhraseClip.length : 0f;
        _cutsceneSequence.AppendCallback(() =>
        {
            if (audioSource != null && voicePhraseClip != null)
            {
                audioSource.clip = voicePhraseClip;
                audioSource.Play();
            }
        });

        if (phraseDuration > 0f)
            _cutsceneSequence.AppendInterval(phraseDuration);

        if (delayAfterPhrase > 0f)
            _cutsceneSequence.AppendInterval(delayAfterPhrase);

        // 4. Звук молнии и вспышка всех источников света
        _cutsceneSequence.AppendCallback(() =>
        {
            if (audioSource != null && lightningSound != null)
                audioSource.PlayOneShot(lightningSound);
        });

        if (lightningLights != null && lightningLights.Count > 0)
        {
            Sequence flashSequence = DOTween.Sequence();

            foreach (var lightSource in lightningLights)
            {
                if (lightSource == null) continue;

                float baseIntensity = lightSource.intensity;

                Sequence singleLightFlash = DOTween.Sequence()
                    .Append(lightSource.DOIntensity(lightningPeakIntensity, lightningDuration * 0.2f).SetEase(Ease.OutExpo))
                    .Append(lightSource.DOIntensity(baseIntensity * 0.5f, lightningDuration * 0.2f).SetEase(Ease.InQuad))
                    .Append(lightSource.DOIntensity(lightningPeakIntensity * 1.2f, lightningDuration * 0.25f).SetEase(Ease.OutExpo))
                    .Append(lightSource.DOIntensity(0f, lightningDuration * 0.35f).SetEase(Ease.InExpo));

                flashSequence.Join(singleLightFlash);
            }

            _cutsceneSequence.Append(flashSequence);
        }

        // 5. Уход в тёмный экран и звук при его появлении
        if (blackScreenCanvasGroup != null)
        {
            _cutsceneSequence.Join(
                blackScreenCanvasGroup.DOFade(1f, fadeToBlackDuration)
                    .SetEase(Ease.InQuad)
                    .OnStart(() =>
                    {
                        blackScreenCanvasGroup.gameObject.SetActive(true);
                        if (audioSource != null && blackScreenSound != null)
                            audioSource.PlayOneShot(blackScreenSound);
                    })
            );
        }
        else
        {
            _cutsceneSequence.AppendCallback(() =>
            {
                if (audioSource != null && blackScreenSound != null)
                    audioSource.PlayOneShot(blackScreenSound);
            });
        }

        _cutsceneSequence.OnComplete(() =>
        {
            onCutsceneFinished?.Invoke();
            OnCutsceneFinishedAction?.Invoke();
        });
    }

    private void OnDestroy()
    {
        _cutsceneSequence?.Kill();
    }
}