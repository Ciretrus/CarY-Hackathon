using UnityEngine;
using UnityEngine.Audio;

public sealed class IntroSceneLink : MonoBehaviour
{
    [SerializeField] private string m_NextScene;
    [SerializeField] private bool m_IsMenu;
    [SerializeField] private AudioClip m_MenuMusic;
    [SerializeField] private AudioMixerGroup m_MusicMixerGroup;
    [SerializeField, Range(0f, 1f)] private float m_MusicVolume = 0.35f;
    [SerializeField, Min(0.05f)] private float m_FadeSeconds = 0.65f;

    [Header("Menu camera flight")]
    [SerializeField] private Camera m_MenuCamera;
    [SerializeField] private Transform m_FlightTarget;
    [SerializeField, Min(0.05f)] private float m_FlightSeconds = 1.6f;

    private void Start()
    {
        if (m_IsMenu && m_MenuMusic != null)
            IntroSceneTransition.Instance.PlayMusic(m_MenuMusic, m_MusicVolume, m_MusicMixerGroup);
    }

    public void LoadNextScene()
    {
        IntroSceneTransition.Instance.Go(m_NextScene, m_FadeSeconds, !m_IsMenu,
            m_MenuCamera, m_FlightTarget, m_FlightSeconds);
    }
}
