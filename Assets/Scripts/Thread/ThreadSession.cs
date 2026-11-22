using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Noir.Interaction
{
    public class ThreadSession : MonoBehaviour
    {
        private enum State { Idle, Threading, Stopped, Suspended }

        [Header("Pin stock")]
        [SerializeField] private Transform[] _pinStock = new Transform[6];

        [Header("String")]
        [SerializeField] private Material _stringMaterial;
        [SerializeField] private float _stringWidth = 0.004f;
        [SerializeField] private int _segments = 12;
        [SerializeField] private float _sag = 0.05f;
        [SerializeField] private float _stringOffset = 0.02f;

        [Header("Logic")]
        [SerializeField] private EvidenceRegistry _registry;
        [SerializeField] private int _requiredClues = 5;

        [Header("Feedback")]
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioClip _pinSound;
        [SerializeField] private AudioClip _failSound;
        [SerializeField] private AudioClip _missingLine;
        [SerializeField] private AudioClip _wrongLine;
        [SerializeField] private UnityEvent onMissing = new();
        [SerializeField] private UnityEvent onWrong = new();

        private sealed class Pin
        {
            public BoardEvidence Evidence;
            public Transform Node;
            public LineRenderer Line;
            public Vector3 Point;
            public Vector3 Normal;
        }

        private readonly List<Pin> _chain = new();
        private State _state;
        private BoardSuspect _suspect;
        private Transform _suspectNode;
        private Vector3 _suspectPoint;
        private int _used;
        private Vector3[] _homePos;
        private Quaternion[] _homeRot;
        private VerletRope _liveRope;

        public bool IsThreading => _state == State.Threading;
        public int ChainCount => _chain.Count;

        void Awake()
        {
            if (_registry == null) _registry = GetComponent<EvidenceRegistry>();
            if (_stringMaterial == null) _stringMaterial = new Material(Shader.Find("Sprites/Default"));
            if (_pinStock == null) _pinStock = new Transform[0];
            _segments = Mathf.Max(2, _segments);

            _homePos = new Vector3[_pinStock.Length];
            _homeRot = new Quaternion[_pinStock.Length];
            for (int i = 0; i < _pinStock.Length; i++)
            {
                var t = _pinStock[i];
                if (t == null)
                {
                    Debug.LogError($"ThreadSession: слот {i} в _pinStock пустой", this);
                    continue;
                }
                _homePos[i] = t.position;
                _homeRot[i] = t.rotation;
                foreach (var c in t.GetComponentsInChildren<Collider>()) Destroy(c);
                SetLayerRecursively(t, 0);
            }

            var ropeGo = new GameObject("LiveThread");
            ropeGo.transform.SetParent(transform, false);
            var lr = ropeGo.AddComponent<LineRenderer>();
            lr.material = _stringMaterial;
            lr.startWidth = _stringWidth;
            lr.endWidth = _stringWidth;
            lr.useWorldSpace = true;
            _liveRope = ropeGo.AddComponent<VerletRope>();
        }

        public void OnBoardEnter() => _state = _suspect != null ? State.Suspended : State.Idle;
        public void OnBoardExit() => _state = _suspect != null ? State.Suspended : State.Idle;

        public bool TryStopThread()
        {
            if (_state != State.Threading) return false;
            _state = State.Stopped;
            return true;
        }

        public void ClickSuspect(BoardSuspect suspect, Vector3 point, Vector3 normal)
        {
            if (suspect == null) return;

            if (_state == State.Suspended && suspect == _suspect)
            {
                _state = State.Threading;
                return;
            }

            ResetChain();

            var node = TakePin(point, normal);
            if (node == null)
            {
                Debug.LogError("ThreadSession: не удалось взять гвоздик из запаса", this);
                return;
            }

            _suspect = suspect;
            _suspect.SetSelected(true);
            _suspectPoint = point;
            _suspectNode = node;
            _state = State.Threading;
            Play(_pinSound);
        }

        public void ClickEvidence(BoardEvidence evidence, Vector3 point, Vector3 normal)
        {
            if (_state != State.Threading || evidence == null) return;

            int index = _chain.FindIndex(p => p.Evidence == evidence);
            if (index >= 0)
            {
                if (index < _chain.Count - 1) { Play(_failSound); return; }
                Reposition(_chain[index], point, normal);
                return;
            }

            if (_chain.Count >= _requiredClues) return;
            if (_used >= _pinStock.Length) return;

            Append(evidence, point, normal);
        }

        void Append(BoardEvidence evidence, Vector3 point, Vector3 normal)
        {
            var node = TakePin(point, normal);
            if (node == null) return;

            var prev = _chain.Count > 0 ? _chain[_chain.Count - 1].Point : _suspectPoint;
            var pin = new Pin
            {
                Evidence = evidence,
                Point = point,
                Normal = normal,
                Node = node,
                Line = CreateLine(prev, point)
            };
            _chain.Add(pin);
            Play(_pinSound);
            if (_chain.Count == _requiredClues){
                Evaluate();
                _state = State.Stopped;
            }
            
        }

        void Reposition(Pin pin, Vector3 point, Vector3 normal)
        {
            pin.Point = point;
            pin.Normal = normal;
            SetNodeTransform(pin.Node, point, normal);
            int index = _chain.IndexOf(pin);
            var prev = index > 0 ? _chain[index - 1].Point : _suspectPoint;
            if (pin.Line != null) SetLine(pin.Line, prev, point);
        }

        void ResetChain()
        {
            foreach (var pin in _chain)
                if (pin.Line != null) Destroy(pin.Line.gameObject);
            _chain.Clear();

            for (int i = 0; i < _used && i < _pinStock.Length; i++)
            {
                if (_pinStock[i] == null) continue;
                _pinStock[i].position = _homePos[i];
                _pinStock[i].rotation = _homeRot[i];
            }
            _used = 0;
            _suspectNode = null;

            if (_suspect != null) _suspect.SetSelected(false);
            _suspect = null;
        }

        Transform TakePin(Vector3 point, Vector3 normal)
        {
            if (_used >= _pinStock.Length) return null;
            var node = _pinStock[_used++];
            if (node == null) return null;
            SetNodeTransform(node, point, normal);
            return node;
        }

        void Evaluate()
        {
            if (_suspect == null || _registry == null) return;
            var required = _suspect.RequiredIds;
            if (required == null || required.Length == 0) return;

            foreach (var id in required)
            {
                var evidence = _registry.GetEvidence(id);
                if (evidence == null)
                {
                    Debug.LogError($"ThreadSession: у '{_suspect.SuspectName}' id '{id}' не существует на доске", this);
                    return;
                }
                if (!evidence.IsFound)
                {
                    onMissing?.Invoke();
                    Play(_missingLine);
                    return;
                }
            }

            var placed = new HashSet<string>();
            foreach (var pin in _chain) placed.Add(pin.Evidence.ID);

            if (placed.SetEquals(new HashSet<string>(required)))
                _suspect.TriggerSolved(_audio);
            else
            {
                onWrong?.Invoke();
                Play(_wrongLine);
            }
        }

        void LateUpdate()
        {
            if (_liveRope == null) return;

            bool on = _state == State.Threading
                      && _suspectNode != null
                      && _chain.Count < _requiredClues
                      && _used < _pinStock.Length;

            if (!on)
            {
                if (_liveRope.Active) _liveRope.End();
                return;
            }

            Vector3 anchor = _chain.Count > 0 ? _chain[_chain.Count - 1].Point : _suspectPoint;
            anchor = WithOffset(anchor);
            Vector3 cursor = CursorPoint(anchor); // курсор автоматически попадёт в ту же смещённую плоскость
            if (!_liveRope.Active) _liveRope.Begin(anchor);
            _liveRope.SetEndpoints(anchor, cursor);
        }

        Vector3 CursorPoint(Vector3 anchor)
        {
            var mouse = Mouse.current;
            var cam = Camera.main;
            if (mouse == null || cam == null) return anchor;

            var ray = cam.ScreenPointToRay(mouse.position.ReadValue());
            Vector3 n = cam.transform.forward;
            float denom = Vector3.Dot(n, ray.direction);
            if (Mathf.Abs(denom) < 1e-4f) return anchor;

            float t = Vector3.Dot(n, anchor - ray.origin) / denom;
            if (t <= 0f) return anchor;

            return ray.GetPoint(t);
        }

        static void SetLayerRecursively(Transform t, int layer)
        {
            t.gameObject.layer = layer;
            foreach (Transform child in t) SetLayerRecursively(child, layer);
        }

        void SetNodeTransform(Transform node, Vector3 point, Vector3 normal)
        {
            node.position = point;
            node.rotation = normal.sqrMagnitude > 0.001f ? Quaternion.LookRotation(normal) : Quaternion.identity;
        }

        LineRenderer CreateLine(Vector3 a, Vector3 b)
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

        void SetLine(LineRenderer lr, Vector3 a, Vector3 b)
        {
            a = WithOffset(a);
            b = WithOffset(b);
            Vector3 mid = (a + b) * 0.5f + Vector3.down * _sag;
            for (int i = 0; i < _segments; i++)
            {
                float t = i / (float)(_segments - 1);
                float u = 1f - t;
                lr.SetPosition(i, u * u * a + 2f * u * t * mid + t * t * b);
            }
        }

        void Play(AudioClip clip)
        {
            if (_audio != null && clip != null) _audio.PlayOneShot(clip);
        }

        Vector3 WithOffset(Vector3 point)
        {
            var cam = Camera.main;
            return cam != null ? point - cam.transform.forward * _stringOffset : point;
        }

    }
}