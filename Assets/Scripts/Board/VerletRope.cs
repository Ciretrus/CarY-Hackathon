using UnityEngine;

namespace Noir.Interaction
{
    [RequireComponent(typeof(LineRenderer))]
    public class VerletRope : MonoBehaviour
    {
        [SerializeField] private int _points = 14;
        [SerializeField] private float _slack = 1.2f;
        [SerializeField] private Vector3 _gravity = new Vector3(0, -9.8f, 0);
        [SerializeField] private float _damping = 0.9f;
        [SerializeField] private int _iterations = 4;

        private LineRenderer _line;
        private Vector3[] _pos;
        private Vector3[] _prev;
        private Vector3 _anchor;
        private Vector3 _target;
        private bool _active;

        public bool Active => _active;

        void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.positionCount = _points;
            _pos = new Vector3[_points];
            _prev = new Vector3[_points];
            _line.enabled = false;
        }

        public void Begin(Vector3 anchor)
        {
            _anchor = anchor;
            _target = anchor;
            for (int i = 0; i < _points; i++)
            {
                _pos[i] = anchor;
                _prev[i] = anchor;
            }
            _active = true;
            _line.enabled = true;
            Write();
        }

        public void SetEndpoints(Vector3 anchor, Vector3 target)
        {
            _anchor = anchor;
            _target = target;
        }

        public void End()
        {
            _active = false;
            _line.enabled = false;
        }

        void LateUpdate()
        {
            if (!_active)
                return;

            float rest = Vector3.Distance(_anchor, _target) * _slack / (_points - 1);
            float dt2 = Time.deltaTime * Time.deltaTime;

            for (int i = 1; i < _points - 1; i++)
            {
                Vector3 v = (_pos[i] - _prev[i]) * _damping;
                _prev[i] = _pos[i];
                _pos[i] += v + _gravity * dt2;
            }

            for (int k = 0; k < _iterations; k++)
            {
                _pos[0] = _anchor;
                _pos[_points - 1] = _target;
                for (int i = 0; i < _points - 1; i++)
                {
                    Vector3 d = _pos[i + 1] - _pos[i];
                    float len = d.magnitude;
                    if (len < 1e-5f)
                        continue;
                    float diff = (len - rest) / len;
                    bool firstPinned = i == 0;
                    bool secondPinned = i + 1 == _points - 1;
                    if (firstPinned && secondPinned)
                        continue;
                    if (firstPinned)
                        _pos[i + 1] -= d * diff;
                    else if (secondPinned)
                        _pos[i] += d * diff;
                    else
                    {
                        _pos[i] += d * (diff * 0.5f);
                        _pos[i + 1] -= d * (diff * 0.5f);
                    }
                }
            }

            _pos[0] = _anchor;
            _pos[_points - 1] = _target;
            Write();
        }

        void Write()
        {
            for (int i = 0; i < _points; i++)
                _line.SetPosition(i, _pos[i]);
        }
    }
}