using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class RandomSfxEmitter : MonoBehaviour
{
    [SerializeField] private AudioClip[] m_Clips;

    [Tooltip("Диапазон паузы в секундах перед первым и каждым следующим звуком.")]
    [SerializeField] private Vector2 m_DelayRange = new Vector2(4f, 12f);

    private AudioSource m_Source;
    private int m_LastIndex = -1;

    private void Awake()
    {
        m_Source = GetComponent<AudioSource>();
        m_Source.playOnAwake = false;
        m_Source.loop = false;

        m_Clips = System.Array.FindAll(
            m_Clips ?? System.Array.Empty<AudioClip>(),
            clip => clip != null
        );
    }

    private void OnEnable()
    {
        if (m_Clips.Length == 0)
        {
            Debug.LogWarning($"{name}: добавь хотя бы один звук в Clips.", this);
            return;
        }

        StartCoroutine(PlaySounds());
    }

    private IEnumerator PlaySounds()
    {
        while (true)
        {
            float delay = Random.Range(m_DelayRange.x, m_DelayRange.y);
            yield return new WaitForSeconds(delay);

            m_LastIndex = ChooseClipIndex();
            m_Source.clip = m_Clips[m_LastIndex];
            m_Source.Play();

            while (m_Source.isPlaying)
                yield return null;
        }
    }

    private int ChooseClipIndex()
    {
        int count = m_Clips.Length;

        if (count == 1 || m_LastIndex < 0)
            return Random.Range(0, count);

        int index = Random.Range(0, count - 1);
        return index >= m_LastIndex ? index + 1 : index;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        m_Source.Stop();
    }

    private void OnValidate()
    {
        m_DelayRange.x = Mathf.Max(0f, m_DelayRange.x);
        m_DelayRange.y = Mathf.Max(m_DelayRange.x, m_DelayRange.y);
    }
}