using System;
using UnityEngine;
using UnityEngine.Serialization;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource m_AudioSource;
    [SerializeField, Range(0f, 1f)] private float m_Volume = 0.65f;

    [Header("Footstep Clips")]
    [FormerlySerializedAs("m_StepClips")]
    [SerializeField] private AudioClip[] m_TileClips;
    [SerializeField] private AudioClip[] m_WoodClips;
    [SerializeField] private AudioClip[] m_WetAsphaltClips;

    [Header("Ground")]
    [SerializeField] private LayerMask m_GroundLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private FootstepSurfaceType m_DefaultSurface = FootstepSurfaceType.Tile;

    [Header("Walking")]
    [SerializeField, Min(0.1f)] private float m_StepDistance = 0.75f;

    private CharacterController m_Controller;
    private Vector3 m_PreviousPosition;
    private float m_DistanceSinceStep;
    private AudioClip m_LastClip;

    private void Awake()
    {
        m_Controller = GetComponent<CharacterController>();

        if (m_AudioSource == null)
            m_AudioSource = gameObject.AddComponent<AudioSource>();

        m_AudioSource.playOnAwake = false;
        m_AudioSource.loop = false;
        m_AudioSource.spatialBlend = 0f;

        // ѕустые €чейки в Inspector не должны давать пропуски при ходьбе.
        m_TileClips = GetAssignedClips(m_TileClips);
        m_WoodClips = GetAssignedClips(m_WoodClips);
        m_WetAsphaltClips = GetAssignedClips(m_WetAsphaltClips);
    }

    private void OnEnable()
    {
        m_PreviousPosition = transform.position;
        m_DistanceSinceStep = 0f;
    }

    private void LateUpdate()
    {
        Vector3 position = transform.position;
        Vector3 delta = position - m_PreviousPosition;
        m_PreviousPosition = position;

        if (!m_Controller.enabled || !m_Controller.isGrounded)
        {
            m_DistanceSinceStep = 0f;
            return;
        }

        delta.y = 0f;

        // Warp между локаци€ми не должен звучать как шаг.
        // ”читываем только путь, который мог пройти CharacterController.
        Vector3 velocity = m_Controller.velocity;
        velocity.y = 0f;

        float walkedDistance = Mathf.Min(
            delta.magnitude,
            velocity.magnitude * Time.deltaTime
        );

        m_DistanceSinceStep += walkedDistance;

        if (m_DistanceSinceStep < m_StepDistance)
            return;

        m_DistanceSinceStep %= m_StepDistance;
        PlayStep(GetSurfaceUnderPlayer());
    }

    private FootstepSurfaceType GetSurfaceUnderPlayer()
    {
        Bounds bounds = m_Controller.bounds;
        float rayLength = bounds.extents.y + m_Controller.stepOffset + 0.1f;

        if (Physics.Raycast(bounds.center, Vector3.down, out RaycastHit hit,
                rayLength, m_GroundLayers, QueryTriggerInteraction.Ignore))
        {
            FootstepSurface surface = hit.collider.GetComponentInParent<FootstepSurface>();

            if (surface != null)
                return surface.SurfaceType;
        }

        return m_DefaultSurface;
    }

    private void PlayStep(FootstepSurfaceType surface)
    {
        AudioClip[] clips = surface switch
        {
            FootstepSurfaceType.Wood => m_WoodClips,
            FootstepSurfaceType.WetAsphalt => m_WetAsphaltClips,
            _ => m_TileClips
        };

        if (clips.Length == 0)
            return;

        int previous = Array.IndexOf(clips, m_LastClip);
        int index;

        if (clips.Length > 1 && previous >= 0)
        {
            index = UnityEngine.Random.Range(0, clips.Length - 1);
            if (index >= previous)
                index++;
        }
        else
        {
            index = UnityEngine.Random.Range(0, clips.Length);
        }

        m_LastClip = clips[index];
        m_AudioSource.pitch = UnityEngine.Random.Range(0.97f, 1.03f);
        m_AudioSource.PlayOneShot(m_LastClip, m_Volume);
    }

    private static AudioClip[] GetAssignedClips(AudioClip[] clips)
    {
        if (clips == null)
            return Array.Empty<AudioClip>();

        return Array.FindAll(clips, clip => clip != null);
    }
}