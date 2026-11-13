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

        public static PhotoClue FindInShot(Camera camera, Rect viewport, EvidenceRegistry registry, LayerMask occluders)
        {
            PhotoClue best = null;
            float bestOffset = float.MaxValue;

            foreach (var clue in _all)
            {
                if (string.IsNullOrWhiteSpace(clue._evidenceId) || registry.IsFound(clue._evidenceId))
                    continue;

                Vector3 point = camera.WorldToViewportPoint(clue.transform.position);
                if (point.z <= 0f || point.z > clue._maxDistance || !viewport.Contains(point))
                    continue;

                if (IsOccluded(camera.transform.position, clue.transform.position, occluders))
                    continue;

                float offset = Vector2.Distance(point, viewport.center);
                if (offset < bestOffset)
                {
                    best = clue;
                    bestOffset = offset;
                }
            }

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
