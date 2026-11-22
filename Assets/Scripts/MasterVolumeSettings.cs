using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class MasterVolumeSettings : MonoBehaviour
{
    private const string Preference = "Audio.MasterVolumePercent";
    private const string SensitivityKey = "MouseSensitivity";
    [SerializeField] private AudioMixer m_Mixer;
    [SerializeField] private string m_Parameter = "MasterVolume";

    [Header("Settings camera")]
    [SerializeField] private Camera m_MenuCamera;
    [SerializeField] private Transform m_SettingsTarget;
    [SerializeField, Min(0f)] private float m_FlightSeconds = 1.2f;
    [SerializeField, Min(0f)] private float m_PanelFadeSeconds = 0.35f;

    private CanvasGroup m_PanelGroup;
    private readonly Dictionary<CanvasGroup, float> m_MenuAlphas = new Dictionary<CanvasGroup, float>();
    private Pose m_Home;
    private bool m_Moving;
    private bool m_HasCameraPose;
    private Button m_Open;
    private GameObject m_Panel;
    private Slider m_Slider;
    private TMP_Text m_Label;
    private Slider m_SensitivitySlider;
    private TMP_Text m_SensitivityLabel;
    private TMP_FontAsset m_Font;
    private readonly List<Selectable> m_Blocked = new List<Selectable>();
    private int m_Percent = 100;
    private bool m_Ready;

    private void Start()
    {
        m_Open = GetComponent<Button>();
        var caption = GetComponentInChildren<TMP_Text>();
        if (caption != null) m_Font = caption.font;
        m_Open.onClick.AddListener(Open);
        if (m_Mixer == null || !m_Mixer.GetFloat(m_Parameter, out float currentDb))
        {
            Debug.LogWarning($"Settings: assign the mixer and expose its master volume as '{m_Parameter}'.", this);
            return;
        }
        int initial = currentDb <= -80f ? 0 : Mathf.RoundToInt(100f * Mathf.Pow(10f, currentDb / 20f));
        m_Percent = Mathf.Clamp(PlayerPrefs.GetInt(Preference, initial), 0, 100);
        m_Ready = true;
        if (PlayerPrefs.HasKey(Preference)) Apply(m_Percent);
    }

    private void Open()
    {
        if (m_Moving || (m_Panel != null && m_Panel.activeSelf)) return;
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        if (m_Panel == null) BuildPanel(canvas.transform);
        foreach (Selectable control in canvas.GetComponentsInChildren<Selectable>())
        {
            if (!control.interactable || control.transform.IsChildOf(m_Panel.transform)) continue;
            m_Blocked.Add(control);
            control.interactable = false;
        }
        foreach (Transform child in canvas.transform)
        {
            if (child.gameObject == m_Panel || !child.gameObject.activeSelf) continue;
            var group = child.GetComponent<CanvasGroup>();
            if (group == null) group = child.gameObject.AddComponent<CanvasGroup>();
            m_MenuAlphas.Add(group, group.alpha);
        }
        m_HasCameraPose = m_MenuCamera != null && m_SettingsTarget != null;
        if (m_HasCameraPose)
            m_Home = new Pose(m_MenuCamera.transform.position, m_MenuCamera.transform.rotation);
        m_PanelGroup.alpha = 0f;
        m_PanelGroup.interactable = false;
        m_Panel.SetActive(true);
        m_Panel.transform.SetAsLastSibling();
        m_Slider.SetValueWithoutNotify(m_Percent);
        RefreshLabel();
        float sensitivity = PlayerPrefs.GetFloat(SensitivityKey, 0.1f);
        m_SensitivitySlider.onValueChanged.RemoveListener(ChangeSensitivity);
        m_SensitivitySlider.minValue = Mathf.Min(0.02f, sensitivity);
        m_SensitivitySlider.maxValue = Mathf.Max(0.50f, sensitivity);
        m_SensitivitySlider.SetValueWithoutNotify(sensitivity);
        m_SensitivitySlider.onValueChanged.AddListener(ChangeSensitivity);
        ShowSensitivity(sensitivity);
        StartCoroutine(ShowSettings());
    }

    private IEnumerator ShowSettings()
    {
        m_Moving = true;
        Pose destination = m_HasCameraPose
            ? new Pose(m_SettingsTarget.position, m_SettingsTarget.rotation) : default;
        yield return Travel(destination, true);
        yield return FadePanel(0f, 1f);
        m_PanelGroup.interactable = true;
        m_Moving = false;
        if (m_Ready) m_Slider.Select();
        else m_SensitivitySlider.Select();
    }

    private void Close()
    {
        if (m_Moving) return;
        PlayerPrefs.Save();
        StartCoroutine(ReturnToMenu());
    }

    private IEnumerator ReturnToMenu()
    {
        m_Moving = true;
        m_PanelGroup.interactable = false;
        yield return FadePanel(1f, 0f);
        yield return Travel(m_Home, false);
        RestoreMenu();
        m_Open.Select();
    }

    private void RestoreMenu()
    {
        if (m_HasCameraPose && m_MenuCamera != null)
            m_MenuCamera.transform.SetPositionAndRotation(m_Home.position, m_Home.rotation);
        m_HasCameraPose = false;
        foreach (var entry in m_MenuAlphas)
            if (entry.Key != null) entry.Key.alpha = entry.Value;
        m_MenuAlphas.Clear();
        m_Panel.SetActive(false);
        foreach (Selectable control in m_Blocked)
            if (control != null) control.interactable = true;
        m_Blocked.Clear();
        m_Moving = false;
    }

    private IEnumerator Travel(Pose destination, bool opening)
    {
        Transform view = m_HasCameraPose ? m_MenuCamera.transform : null;
        Pose start = view != null ? new Pose(view.position, view.rotation) : default;
        float seconds = view != null ? m_FlightSeconds : m_PanelFadeSeconds;
        float elapsed = 0f;
        while (true)
        {
            float t = seconds > 0f ? Mathf.Clamp01(elapsed / seconds) : 1f;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            if (view != null)
                view.SetPositionAndRotation(Vector3.Lerp(start.position, destination.position, eased),
                    Quaternion.Slerp(start.rotation, destination.rotation, eased));
            foreach (var entry in m_MenuAlphas)
                if (entry.Key != null) entry.Key.alpha = entry.Value * (opening ? 1f - eased : eased);
            if (t >= 1f) yield break;
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
    }

    private IEnumerator FadePanel(float from, float to)
    {
        for (float elapsed = 0f; elapsed < m_PanelFadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            m_PanelGroup.alpha = Mathf.SmoothStep(from, to, elapsed / m_PanelFadeSeconds);
            yield return null;
        }
        m_PanelGroup.alpha = to;
    }

    private void OnDisable()
    {
        if (m_Panel == null || !m_Panel.activeSelf) return;
        StopAllCoroutines();
        RestoreMenu();
    }

    private void Apply(float value)
    {
        int percent = Mathf.RoundToInt(value);
        float db = percent == 0 ? -80f : 20f * Mathf.Log10(percent / 100f);
        if (!m_Mixer.SetFloat(m_Parameter, db)) return;
        m_Percent = percent;
        PlayerPrefs.SetInt(Preference, percent);
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        if (m_Label != null)
            m_Label.text = m_Ready ? $"Master volume   {m_Percent}%" : "Master volume unavailable";
    }

    private void BuildPanel(Transform parent)
    {
        RectTransform root = Place("Settings panel", parent, 0f, 0f, 1f, 1f);
        m_Panel = root.gameObject;
        m_PanelGroup = root.gameObject.AddComponent<CanvasGroup>();
        root.gameObject.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.05f, 0.52f);
        Label(root, "Settings", 0.77f, 0.90f, 46);
        m_Label = Label(root, "", 0.64f, 0.73f, 32);

        m_Slider = CreateSlider(root, "Master volume", 0.59f);
        m_Slider.maxValue = 100;
        m_Slider.wholeNumbers = true;
        m_Slider.interactable = m_Ready;
        m_Slider.onValueChanged.AddListener(Apply);

        m_SensitivityLabel = Label(root, "", 0.43f, 0.52f, 32);
        m_SensitivitySlider = CreateSlider(root, "Mouse sensitivity", 0.38f);

        RectTransform back = Place("Back", root, 0.42f, 0.17f, 0.58f, 0.26f);
        var background = back.gameObject.AddComponent<Image>();
        background.color = new Color(0.22f, 0.22f, 0.24f);
        var button = back.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(Close);
        Label(back, "Back", 0f, 1f, 28);
        m_Panel.SetActive(false);
    }

    private void ChangeSensitivity(float value)
    {
        float sensitivity = Mathf.Round(value * 100f) / 100f;
        m_SensitivitySlider.SetValueWithoutNotify(sensitivity);
        PlayerPrefs.SetFloat(SensitivityKey, sensitivity);
        ShowSensitivity(sensitivity);
    }

    private void ShowSensitivity(float sensitivity)
    {
        m_SensitivityLabel.text = $"Mouse sensitivity   {sensitivity:0.00}";
    }

    private static Slider CreateSlider(Transform root, string name, float height)
    {
        var sliderObject = DefaultControls.CreateSlider(new DefaultControls.Resources());
        sliderObject.name = name;
        var rect = (RectTransform)sliderObject.transform;
        rect.SetParent(root, false);
        rect.anchorMin = new Vector2(0.30f, height);
        rect.anchorMax = new Vector2(0.70f, height);
        rect.sizeDelta = new Vector2(0f, 28f);
        rect.anchoredPosition = Vector2.zero;
        var track = sliderObject.transform.Find("Background");
        if (track != null && track.TryGetComponent(out Image trackImage))
            trackImage.color = new Color(0.25f, 0.25f, 0.27f);
        return sliderObject.GetComponent<Slider>();
    }

    private TMP_Text Label(Transform parent, string caption, float bottom, float top, int size)
    {
        var label = Place("Label", parent, 0f, bottom, 1f, top).gameObject.AddComponent<TextMeshProUGUI>();
        if (m_Font != null) label.font = m_Font;
        label.text = caption;
        label.fontSize = size;
        label.enableAutoSizing = true;
        label.fontSizeMin = 12;
        label.fontSizeMax = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static RectTransform Place(string name, Transform parent, float left, float bottom, float right, float top)
    {
        var rect = (RectTransform)new GameObject(name, typeof(RectTransform)).transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private void OnDestroy()
    {
        if (m_Panel != null) Destroy(m_Panel);
    }
}
