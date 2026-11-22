using System;
using System.Collections.Generic;
using UnityEngine;
namespace Noir.Interaction
{
    public class EvidenceRegistry : MonoBehaviour
    {
        private BoardEvidence[] _all;
        private Dictionary<string, BoardEvidence> _byId;
        public event Action<BoardEvidence> FoundChanged;
        public int FoundCount
        {
            get
            {
                int count = 0;
                foreach (var evidence in _all)
                {
                    if (evidence.IsFound)
                        count++;
                }
                return count;
            }
        }
        public int TotalCount => _all.Length;
        public IReadOnlyList<BoardEvidence> All => _all;
        public IEnumerable<BoardEvidence> Found
        {
            get
            {
                foreach (var evidence in _all)
                {
                    if (evidence.IsFound)
                        yield return evidence;
                }
            }
        }

        private void Awake()
        {
            _all = GetComponentsInChildren<BoardEvidence>(true);
            _byId = new Dictionary<string, BoardEvidence>();
            foreach (var evidence in _all)
            {
                if (string.IsNullOrWhiteSpace(evidence.ID))
                    continue;

                if (!_byId.ContainsKey(evidence.ID))
                {
                    _byId.Add(evidence.ID, evidence);
                }
                else
                {
                    Debug.LogWarning($"Дупликат ID: {evidence.ID} у {evidence.gameObject.name}");
                }
            }
        }
        public bool Collect(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (!_byId.TryGetValue(id, out BoardEvidence evidence))
            {
                Debug.LogWarning($"EvidenceRegistry: неизвестный id '{id}'");
                return false;
            }

            if (evidence.IsFound)
                return false;

            evidence.MarkFound();
            FoundChanged?.Invoke(evidence);
            return true;
        }
        public bool Collect(string id, Texture2D photo)
        {
            var evidence = GetEvidence(id);
            if (evidence != null && !evidence.IsFound)
                evidence.SetPhoto(photo);

            return Collect(id);
        }
        public bool IsFound(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (_byId.TryGetValue(id, out BoardEvidence evidence))
            {
                return evidence.IsFound;
            }
            return false;
        }
        public BoardEvidence GetEvidence(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            if (_byId.TryGetValue(id, out BoardEvidence evidence))
            {
                return evidence;
            }
            return null;
        }
    }
}
