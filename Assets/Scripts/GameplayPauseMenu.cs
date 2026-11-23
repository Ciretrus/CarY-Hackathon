using System.Collections.Generic;
using Noir.Interaction;
using Noir.Player;
using Noir.UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(-500)]
[RequireComponent(typeof(PlayerModes))]
public sealed class GameplayPauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject m_Menu;
    [SerializeField] private MasterVolumeSettings m_SettingsButton;

    private readonly List<Behaviour> m_Suspended = new();
    private PlayerModes m_Modes;
    private PlayerLook m_Look;
    private PlayerMovement m_Movement;
    private bool m_Open, m_ResumeRequested;
    private bool m_ReturningToMenu;
    private float m_TimeScale;


    private void Start()
    {
        m_Modes = GetComponent<PlayerModes>();
        m_Look = FindFirstObjectByType<PlayerLook>();
        m_Movement = FindFirstObjectByType<PlayerMovement>();
        if (!m_Menu || !m_SettingsButton || !m_Look || !m_Movement)
        {
            Debug.LogError("GameplayPauseMenu: проверь панель, кнопку настроек и игрока.", this);
            enabled = false;
            return;
        }
        m_Menu.SetActive(false);
        m_SettingsButton.ConfigurePause(m_Menu, m_Look);
    }

    private void Update()
    {
        if (m_ReturningToMenu || Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (m_Open)
        {
            if (m_SettingsButton.IsOpen) m_SettingsButton.Close();
            else Resume();
            return;
        }
        if (m_Modes.Current || !m_Movement.isActiveAndEnabled || !m_Look.isActiveAndEnabled) return;
        foreach (var cutscene in FindObjectsByType<DoorCutsceneSequence>(FindObjectsSortMode.None))
            if (cutscene.IsPlaying) return;
        foreach (var flash in FindObjectsByType<FlashbackScreen>(FindObjectsSortMode.None))
            if (flash.IsPlaying) return;
        foreach (var intro in FindObjectsByType<IntroSceneTransition>(FindObjectsSortMode.None))
            if (intro.IsTransitioning) return;
        foreach (var inspect in GetComponentsInChildren<InspectMode>(true))
            if (inspect.IsReturning) return;

        m_TimeScale = Time.timeScale;
        m_Open = true;
        Suspend(m_Look);
        Suspend(m_Movement);
        foreach (var c in FindObjectsByType<Interactor>(FindObjectsSortMode.None)) Suspend(c);
        foreach (var c in FindObjectsByType<ModeHotkey>(FindObjectsSortMode.None)) Suspend(c);
        foreach (var c in FindObjectsByType<LocationExit>(FindObjectsSortMode.None)) Suspend(c);
        foreach (var c in FindObjectsByType<CutsceneDebugTrigger>(FindObjectsSortMode.None)) Suspend(c);
        Time.timeScale = 0;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        m_Menu.SetActive(true);
    }

    private void Suspend(Behaviour component)
    {
        if (!component.isActiveAndEnabled) return;
        m_Suspended.Add(component);
        component.enabled = false;
    }

    public void Resume() => m_ResumeRequested = m_Open && !m_ReturningToMenu;
    private void LateUpdate()
    {
        if (m_ResumeRequested && !(Keyboard.current?.escapeKey.isPressed ?? false))
            Restore();
    }

    private void Restore()
    {
        if (!m_Open) return;
        m_Open = m_ResumeRequested = false;
        m_Menu.SetActive(false);
        Time.timeScale = m_TimeScale;
        foreach (var c in m_Suspended)
            if (c) c.enabled = true;
        m_Suspended.Clear();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        PlayerPrefs.Save();
    }

    private void OnDisable()
    {
        if (!m_ReturningToMenu)
        {
            Restore();
            return;
        }
        Time.timeScale = m_TimeScale;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Quit()
    {
        const string sceneName = "Main Menu";
        if (!m_Open || m_ReturningToMenu) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError("Добавь сцену Main Menu в Scene List активного Build Profile.", this);
            return;
        }
        m_ReturningToMenu = true;
        m_ResumeRequested = false;
        m_Menu.SetActive(false);
        PlayerPrefs.Save();
        IntroSceneTransition.Instance.Go(sceneName, 0.35f, false);
    }
}
