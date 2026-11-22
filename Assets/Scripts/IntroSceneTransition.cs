using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public sealed class IntroSceneTransition : MonoBehaviour
{
    private static IntroSceneTransition s_Instance;
    private AudioSource m_Music;
    private Image m_Curtain;
    private RectTransform m_LoadingIcon;
    private bool m_Loading;

    public static IntroSceneTransition Instance
    {
        get
        {
            if (s_Instance == null)
                new GameObject("Intro Scene Transition", typeof(RectTransform)).AddComponent<IntroSceneTransition>();
            return s_Instance;
        }
    }

    private void Awake()
    {
        s_Instance = this;
        DontDestroyOnLoad(gameObject);
        m_Music = gameObject.AddComponent<AudioSource>();
        m_Music.playOnAwake = false;
        m_Music.loop = true;
        m_Music.spatialBlend = 0f;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        gameObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Black", typeof(RectTransform));
        panel.transform.SetParent(transform, false);
        m_Curtain = panel.AddComponent<Image>();
        m_Curtain.rectTransform.anchorMin = Vector2.zero;
        m_Curtain.rectTransform.anchorMax = Vector2.one;
        m_Curtain.rectTransform.offsetMin = Vector2.zero;
        m_Curtain.rectTransform.offsetMax = Vector2.zero;
        m_Curtain.color = Color.clear;
        m_Curtain.enabled = false;
    }

    public void PlayMusic(AudioClip clip, float volume, AudioMixerGroup outputGroup = null)
    {
        m_Music.outputAudioMixerGroup = outputGroup;
        m_Music.volume = volume;
        if (m_Music.isPlaying && m_Music.clip == clip) return;
        m_Music.clip = clip;
        m_Music.Play();
    }

    public void Go(string sceneName, float duration, bool endIntro,
        Camera menuCamera = null, Transform flightTarget = null, float flightSeconds = 1.6f)
    {
        if (m_Loading) return;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"Intro transition: check scene '{sceneName}' in the active Build Profile.");
            return;
        }
        StartCoroutine(ChangeScene(sceneName, Mathf.Max(0.05f, duration), endIntro,
            menuCamera, flightTarget, Mathf.Max(0.05f, flightSeconds)));
    }

    private IEnumerator ChangeScene(string sceneName, float duration, bool endIntro,
        Camera menuCamera, Transform flightTarget, float flightSeconds)
    {
        m_Loading = true;
        m_Curtain.enabled = true;
        if (endIntro)
        {
            yield return FadeScreen(0f, 1f, duration);
            CreateLoadingIcon();
            // Present one fully covered frame before loading can stall the main thread.
            yield return null;
        }

        var load = SceneManager.LoadSceneAsync(sceneName);
        if (!endIntro)
        {
            load.allowSceneActivation = false;
            if (menuCamera != null && flightTarget != null)
                yield return FlyAndFade(menuCamera.transform, flightTarget, flightSeconds);
            else
                yield return FadeScreen(0f, 1f, duration);
            load.allowSceneActivation = true;
        }
        yield return load;

        Coroutine musicTail = null;
        if (endIntro)
        {
            m_LoadingIcon.gameObject.SetActive(false);
            // A new visit to the menu can now create its own music source.
            s_Instance = null;
            musicTail = StartCoroutine(FinishMusic());
        }

        if (endIntro)
            yield return RevealGameplay(duration);
        else
            yield return FadeScreen(1f, 0f, duration);
        m_Curtain.enabled = false;
        if (endIntro)
        {
            yield return musicTail;
            Destroy(gameObject);
        }
        else
        {
            m_Loading = false;
        }
    }

    private IEnumerator FlyAndFade(Transform cameraTransform, Transform target, float seconds)
    {
        Vector3 startPosition = cameraTransform.position;
        Quaternion startRotation = cameraTransform.rotation;
        Vector3 endPosition = target.position;
        Quaternion endRotation = target.rotation;
        for (float elapsed = 0f; elapsed < seconds; elapsed += Time.unscaledDeltaTime)
        {
            float t = Mathf.SmoothStep(0f, 1f, elapsed / seconds);
            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, endPosition, t),
                Quaternion.Slerp(startRotation, endRotation, t));
            m_Curtain.color = new Color(0f, 0f, 0f, t);
            yield return null;
        }
        cameraTransform.SetPositionAndRotation(endPosition, endRotation);
        m_Curtain.color = Color.black;
    }

    private IEnumerator RevealGameplay(float fadeSeconds)
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.TryGetComponent(out UniversalAdditionalCameraData cameraData)
            || cameraData.volumeLayerMask.value == 0)
        {
            yield return FadeScreen(1f, 0f, fadeSeconds);
            yield break;
        }

        // Match a layer already evaluated by the gameplay camera.
        int layer = 0;
        while ((cameraData.volumeLayerMask.value & (1 << layer)) == 0) layer++;
        var holder = new GameObject("Intro soft focus");
        holder.layer = layer;
        var volume = holder.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = float.MaxValue;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.sharedProfile = profile;
        var focus = profile.Add<DepthOfField>();
        focus.mode.Override(DepthOfFieldMode.Gaussian);
        focus.gaussianStart.Override(0f);
        focus.gaussianEnd.Override(camera.nearClipPlane);
        focus.gaussianMaxRadius.Override(1.5f);

        bool hadPostProcessing = cameraData.renderPostProcessing;
        cameraData.renderPostProcessing = true;
        float near = Mathf.Max(0.01f, camera.nearClipPlane);
        float range = Mathf.Max(1f, camera.farClipPlane / near);
        float revealSeconds = fadeSeconds + 0.9f;
        try
        {
            // Give the fully covered camera a frame to evaluate its new volume.
            yield return null;
            for (float elapsed = 0f; elapsed < revealSeconds; elapsed += Time.unscaledDeltaTime)
            {
                m_Curtain.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(1f, 0f, elapsed / fadeSeconds));
                float t = Mathf.InverseLerp(fadeSeconds * 0.35f, revealSeconds, elapsed);
                float clarity = Mathf.SmoothStep(0f, 1f, t);
                float distance = near * Mathf.Pow(range, clarity);
                focus.gaussianStart.value = distance - near;
                focus.gaussianEnd.value = distance * (1f + clarity);
                focus.gaussianMaxRadius.value = Mathf.Lerp(1.5f, 0.5f, clarity);
                yield return null;
            }
            m_Curtain.color = Color.clear;
        }
        finally
        {
            if (cameraData != null) cameraData.renderPostProcessing = hadPostProcessing;
            if (volume != null) volume.enabled = false;
            if (holder != null) Destroy(holder);
            Destroy(profile);
        }
    }

    private void CreateLoadingIcon()
    {
        m_LoadingIcon = new GameObject("Loading", typeof(RectTransform)).GetComponent<RectTransform>();
        m_LoadingIcon.SetParent(m_Curtain.transform, false);
        m_LoadingIcon.sizeDelta = new Vector2(48f, 48f);

        const int marks = 8;
        for (int i = 0; i < marks; i++)
        {
            var mark = new GameObject("Mark", typeof(RectTransform)).AddComponent<Image>();
            var rect = mark.rectTransform;
            rect.SetParent(m_LoadingIcon, false);
            float angle = 360f * i / marks;
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
            rect.anchoredPosition = (Vector2)(rect.localRotation * Vector3.up) * 18f;
            rect.sizeDelta = new Vector2(4f, 9f);
            mark.color = new Color(1f, 1f, 1f, 1f - 0.1f * i);
            mark.raycastTarget = false;
        }
    }

    private void Update()
    {
        if (m_LoadingIcon != null && m_LoadingIcon.gameObject.activeSelf)
            m_LoadingIcon.Rotate(0f, 0f, -220f * Time.unscaledDeltaTime);
    }

    private IEnumerator FinishMusic()
    {
        if (!m_Music.isPlaying) yield break;
        yield return new WaitForSecondsRealtime(5f);

        const float fadeSeconds = 2f;
        float initialVolume = m_Music.volume;
        for (float elapsed = 0f; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            m_Music.volume = Mathf.SmoothStep(initialVolume, 0f, elapsed / fadeSeconds);
            yield return null;
        }
        m_Music.volume = 0f;
        m_Music.Stop();
    }

    private IEnumerator FadeScreen(float from, float to, float duration)
    {
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            m_Curtain.color = new Color(0f, 0f, 0f, Mathf.SmoothStep(from, to, elapsed / duration));
            yield return null;
        }
        m_Curtain.color = new Color(0f, 0f, 0f, to);
    }
}
