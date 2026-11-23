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
    [SerializeField] private Transform lookTarget;
    [Tooltip("Средняя скорость поворота в градусах в секунду.")]
    [SerializeField, Min(1f)] private float turnSpeed = 180f;
    [SerializeField] private Ease turnEase = Ease.InOutQuad;

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
    [SerializeField] private CanvasGroup thankYouPanel;
    [SerializeField] private float thankYouFadeDuration = 1f;
    [SerializeField] private AudioClip blackScreenSound;
    [SerializeField] private float fadeToBlackDuration = 0.15f;

    [Header("События")]
    public UnityEvent onCutsceneFinished;
    public event Action OnCutsceneFinishedAction;

    private Sequence _cutsceneSequence;
    public bool IsPlaying => _cutsceneSequence.IsActive();

    private void Awake()
    {
        if (thankYouPanel != null)
        {
            thankYouPanel.alpha = 0f;
            thankYouPanel.gameObject.SetActive(false);
        }

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
        if (IsPlaying) return;

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

        AppendCameraTurn(_cutsceneSequence);

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

        if (thankYouPanel != null)
        {
            _cutsceneSequence.Append(
                thankYouPanel.DOFade(1f, thankYouFadeDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnStart(() =>
                    {
                        thankYouPanel.alpha = 0f;
                        thankYouPanel.gameObject.SetActive(true);
                    })
            );
        }
        _cutsceneSequence.AppendInterval(5f);
        _cutsceneSequence.OnComplete(() =>
        {
            onCutsceneFinished?.Invoke();
            OnCutsceneFinishedAction?.Invoke();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            IntroSceneTransition.Instance.Go("Main Menu", 0.35f, false);
        });
    }

    private void ShowThankYou()
    {
        if (thankYouPanel != null)
            thankYouPanel.gameObject.SetActive(true);
        else
            Debug.LogWarning("Назначь Thank You Panel для финального экрана.", this);
    }

    private void AppendCameraTurn(Sequence sequence)
    {
        if (lookTarget == null || playerLook == null)
        {
            Debug.LogWarning("DoorCutsceneSequence: назначь Look Target и Player Look для поворота камеры.", this);
            return;
        }

        Transform cameraRig = playerLook.transform;
        Vector3 direction = lookTarget.position - cameraRig.position;
        if (direction.sqrMagnitude < 0.000001f) return;

        // Финальная катсцена управляет ориентацией виртуальной камеры до затемнения.
        if (playerLook.TryGetComponent<CinemachinePanTilt>(out var panTilt))
            panTilt.enabled = false;

        Vector3 flat = Vector3.ProjectOnPlane(direction, Vector3.up);
        float yaw = flat.sqrMagnitude > 0.000001f
            ? Mathf.Atan2(flat.x, flat.z) * Mathf.Rad2Deg
            : cameraRig.eulerAngles.y;
        float pitch = -Mathf.Atan2(direction.y, flat.magnitude) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(pitch, yaw, 0f);
        float duration = Quaternion.Angle(cameraRig.rotation, targetRotation) / Mathf.Max(1f, turnSpeed);
        if (duration > 0f)
            sequence.Append(cameraRig.DORotateQuaternion(targetRotation, duration).SetEase(turnEase));
    }

    private void OnDestroy()
    {
        _cutsceneSequence?.Kill();
    }
}