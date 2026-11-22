using System.Collections.Generic;
using Noir.Interaction;
using UnityEngine;

namespace Noir.Photo
{
    public class PhotoClue : MonoBehaviour
    {
        private const float OcclusionTolerance = 0.25f;

        private static readonly List<PhotoClue> _all = new();

        [SerializeField] private string _evidenceId;
        [SerializeField] private AudioClip _foundLine;
        [SerializeField] private float _maxDistance = 6f;

        public string EvidenceId => _evidenceId;
        public AudioClip FoundLine => _foundLine;

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        public static PhotoClue FindInShot(
            Camera camera, Rect viewport, EvidenceRegistry registry, LayerMask occluders)
        {
            return FindInShot(camera, viewport, registry, occluders, out _);
        }

        public static PhotoClue FindInShot(
            Camera camera, Rect viewport, EvidenceRegistry registry, LayerMask occluders,
            out bool foundKnownClue)
        {
            foundKnownClue = false;
            if (camera == null || registry == null)
            {
                Debug.LogError($"[PhotoClue] Не назначена ссылка. Camera={camera}, Registry={registry}");
                return null;
            }

            Debug.Log($"[PhotoClue] Снимок. Камера: {camera.name}, активных улик: {_all.Count}");

            PhotoClue best = null;
            float bestOffset = float.MaxValue;

            foreach (var clue in _all)
            {
                string label = $"[PhotoClue] {clue.name} (ID='{clue._evidenceId}')";

                if (string.IsNullOrWhiteSpace(clue._evidenceId))
                {
                    Debug.Log($"{label}: пустой Evidence Id.", clue);
                    continue;
                }

                var evidence = registry.GetEvidence(clue._evidenceId);
                if (evidence == null) continue;

                Vector3 target = clue.transform.position;
                Vector3 point = camera.WorldToViewportPoint(target);

                if (point.z <= 0f || point.z > clue._maxDistance)
                {
                    Debug.Log($"{label}: глубина {point.z:F2}, допустимо от 0 до {clue._maxDistance:F2}.", clue);
                    continue;
                }

                if (!viewport.Contains(point))
                {
                    Debug.Log($"{label}: точка вне кадра. X={point.x:F2}, Y={point.y:F2}, " +
                              $"границы X=[{viewport.xMin:F2}, {viewport.xMax:F2}], " +
                              $"Y=[{viewport.yMin:F2}, {viewport.yMax:F2}].", clue);
                    continue;
                }

                if (Physics.Linecast(camera.transform.position, target, out RaycastHit hit,
                        occluders, QueryTriggerInteraction.Ignore))
                {
                    float gap = Vector3.Distance(hit.point, target);
                    if (gap > OcclusionTolerance)
                    {
                        Debug.Log($"{label}: обзор перекрывает '{hit.collider.name}', " +
                                  $"от поверхности коллайдера до метки {gap:F2}, " +
                                  $"допуск {OcclusionTolerance:F2}.", hit.collider);
                        continue;
                    }
                }

                if (evidence.IsFound)
                {
                    foundKnownClue = true;
                    continue;
                }

                float offset = Vector2.Distance(point, viewport.center);
                if (offset < bestOffset)
                {
                    best = clue;
                    bestOffset = offset;
                }
            }

            if (best != null)
                Debug.Log($"[PhotoClue] Выбрана улика '{best.EvidenceId}'.", best);
            else if (!foundKnownClue)
                Debug.Log("[PhotoClue] Подходящей улики в кадре нет.");

            return best;
        }

        private static bool IsOccluded(Vector3 from, Vector3 to, LayerMask occluders)
        {
            if (!Physics.Linecast(from, to, out RaycastHit hit, occluders, QueryTriggerInteraction.Ignore))
                return false;

            return Vector3.Distance(hit.point, to) > OcclusionTolerance;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.05f);
        }
    }
}
