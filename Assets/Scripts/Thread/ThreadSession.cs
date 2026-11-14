using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Noir.Interaction
{
    public class ThreadSession : MonoBehaviour
    {
        [Header("Pin")]
        [SerializeField] private GameObject _pinPrefab;
        [SerializeField] private float _pinScale = 0.02f;

        [Header("String")]
        [SerializeField] private Material _stringMaterial;
        [SerializeField] private float _stringWidth = 0.004f;
        [SerializeField] private int _segments = 12;
        [SerializeField] private float _sag = 0.05f;

        [Header("Logic")]
        [SerializeField] private EvidenceRegistry _registry;
        [SerializeField] private Solution[] _solutions = { };
        [SerializeField] private float _eraseStep = 0.08f;

        [Header("Feedback")]
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _failSound;
        [SerializeField] private AudioClip _missingLine;
        [SerializeField] private AudioClip _wrongLine;
        [SerializeField] private AudioClip _solvedLine;
        [SerializeField] private UnityEvent onMissing = new();
        [SerializeField] private UnityEvent onWrong = new();
        [SerializeField] private UnityEvent onSolved = new();

        [Serializable]
        public class Solution
        {
           public string suspect;
           public string[] ids;
        }
        private sealed class Pin
        {
            public BoardEvidence Evidence;
            public Transform Node;
            public LineRenderer Line;
            public Vector3 Point;
            public Vector3 Normal;
        }

        private readonly List<Pin> _chain = new();
        private bool _busy;
        private VerletRope _liveRope;

        public bool IsBusy => _busy;
        public int ChainCount => _chain.Count;
        public bool ThreadsActive { get; set; }

        void Awake()
        {
            if (_registry == null)
                _registry = GetComponent<EvidenceRegistry>();
            if (_stringMaterial == null)
                _stringMaterial = new Material(Shader.Find("Sprites/Default"));
            _segments = Mathf.Max(2, _segments);

            var ropeGo = new GameObject("LiveThread");
            ropeGo.transform.SetParent(transform, false);
            var lr = ropeGo.AddComponent<LineRenderer>();
            lr.material = _stringMaterial;
            lr.startWidth = _stringWidth;
            lr.endWidth = _stringWidth;
            lr.useWorldSpace = true;
            _liveRope = ropeGo.AddComponent<VerletRope>();
        }

        public void PlacePin(BoardEvidence evidence, Vector3 point, Vector3 normal)
        {
            if (_busy || evidence == null)
                return;

            int index = _chain.FindIndex(p => p.Evidence == evidence);

            if (index >= 0)
            {
                if (index < _chain.Count - 1)
                {
                    Play(_failSound);
                    return;
                }

                Reposition(_chain[index], point, normal);
                return;
            }

            Append(evidence, point, normal);
        }

        public void EraseAll()
        {
            if (_busy || _chain.Count == 0)
                return;
            _busy = true;

            var seq = DOTween.Sequence();
            for (int i = _chain.Count - 1; i >= 0; i--)
            {
                var pin = _chain[i];
                seq.AppendInterval(_eraseStep);
                seq.AppendCallback(() => DestroyPin(pin));
            }
            seq.AppendCallback(() =>
            {
                _chain.Clear();
                _busy = false;
            });
            seq.SetLink(gameObject);
        }

        private void LateUpdate()
        {
            if (_liveRope == null)
                return;

            bool on = ThreadsActive && !_busy && _chain.Count > 0;
            if (!on)
            {
                if (_liveRope.Active)
                    _liveRope.End();
                return;
            }

            var last = _chain[_chain.Count - 1];
            Vector3 cursor = CursorPoint(last.Point);

            if (!_liveRope.Active)
                _liveRope.Begin(last.Point);
            _liveRope.SetEndpoints(last.Point, cursor);
        }

        private Vector3 CursorPoint(Vector3 fallback)
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null)
                return fallback;

            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            Vector3 n = transform.forward;
            float denom = Vector3.Dot(n, ray.direction);
            if (Mathf.Abs(denom) < 1e-4f)
                return fallback;
            float t = Vector3.Dot(n, transform.position - ray.origin) / denom;
            if (t <= 0f)
                return fallback;
            return ray.GetPoint(t);
        }

        private void Append(BoardEvidence evidence, Vector3 point, Vector3 normal)
        {
            var pin = new Pin
            {
                Evidence = evidence,
                Point = point,
                Normal = normal,
                Node = CreateNode(point, normal)
            };

            if (_chain.Count > 0)
                pin.Line = CreateLine(_chain[_chain.Count - 1].Point, point);

            _chain.Add(pin);

            if (_registry != null && _chain.Count == _registry.FoundCount)
                Evaluate();
        }

        private void Reposition(Pin pin, Vector3 point, Vector3 normal)
        {
            pin.Point = point;
            pin.Normal = normal;
            SetNodeTransform(pin.Node, point, normal);

            int index = _chain.IndexOf(pin);
            if (pin.Line != null && index > 0)
                SetLine(pin.Line, _chain[index - 1].Point, point);
        }

        private void Evaluate()
        {
            if (_solutions.Length == 0)
                return;

            int available = _registry != null ? _registry.FoundCount : _chain.Count;
            if (_chain.Count != available || available == 0)
                return;

            if (available < _solutions[0].ids.Length)
            {
                onMissing?.Invoke();
                Play(_missingLine);
                return;
            }

            if (FindSolution() >= 0)
            {
                onSolved?.Invoke();
                Play(_solvedLine);
            }
            else
            {
                onWrong?.Invoke();
                Play(_wrongLine);
            }
        }

        private int FindSolution()
        {
            for (int i = 0; i < _solutions.Length; i++)
                if (Matches(_solutions[i].ids))
                    return i;
            return -1;
        }

        private bool Matches(string[] ids)
        {
            if (ids.Length != _chain.Count)
                return false;
            for (int i = 0; i < ids.Length; i++)
                if (_chain[i].Evidence.ID != ids[i])
                    return false;
            return true;
        }

        private Transform CreateNode(Vector3 point, Vector3 normal)
        {
            GameObject node;
            if (_pinPrefab != null)
                node = Instantiate(_pinPrefab, point, Quaternion.identity);
            else
            {
                node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                node.transform.localScale = Vector3.one * _pinScale;
            }

            foreach (var c in node.GetComponentsInChildren<Collider>())
                Destroy(c);
            SetLayerRecursively(node.transform, 0);

            node.name = "Pin";
            node.transform.SetParent(transform, true);
            SetNodeTransform(node.transform, point, normal);
            return node.transform;
        }

        private static void SetLayerRecursively(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t)
                SetLayerRecursively(child, layer);
        }

        private void SetNodeTransform(Transform node, Vector3 point, Vector3 normal)
        {
            node.position = point;
            node.rotation = normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
        }

        private LineRenderer CreateLine(Vector3 a, Vector3 b)
        {
            var go = new GameObject("ThreadString");
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.material = _stringMaterial;
            lr.startWidth = _stringWidth;
            lr.endWidth = _stringWidth;
            lr.positionCount = _segments;
            lr.useWorldSpace = true;
            SetLine(lr, a, b);
            return lr;
        }

        private void SetLine(LineRenderer lr, Vector3 a, Vector3 b)
        {
            Vector3 mid = (a + b) * 0.5f + Vector3.down * _sag;
            for (int i = 0; i < _segments; i++)
            {
                float t = i / (float)(_segments - 1);
                float u = 1f - t;
                lr.SetPosition(i, u * u * a + 2f * u * t * mid + t * t * b);
            }
        }

        private void DestroyPin(Pin pin)
        {
            if (pin.Line != null)
                Destroy(pin.Line.gameObject);
            if (pin.Node != null)
                Destroy(pin.Node.gameObject);
        }

        private void Play(AudioClip clip)
        {
            if (_audio != null && clip != null)
                _audio.PlayOneShot(clip);
        }
        private void OnValidate()
        {
            if (_solutions == null || _solutions.Length == 0)
                return;

            int length = _solutions[0].ids.Length;
            var seen = new HashSet<string>();
            foreach (var s in _solutions)
            {
                if (s.ids.Length != length)
                    Debug.LogWarning($"Solution '{s.suspect}': длина отличается от первого решения", this);
                if (new HashSet<string>(s.ids).Count != s.ids.Length)
                    Debug.LogWarning($"Solution '{s.suspect}': id повторяются", this);
                if (!seen.Add(string.Join(",", s.ids)))
                    Debug.LogWarning($"Solution '{s.suspect}': дубликат другого решения", this);
            }
        }
    }
}