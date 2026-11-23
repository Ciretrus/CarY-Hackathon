using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;

namespace Noir.Interaction
{
    [Serializable]
    public struct HighlightStyle
    {
        [ColorUsage(false, true)] public Color color;
        [Range(0f, 1f)] public float intensity;
        [Tooltip("Толщина линии, мировые единицы")][Min(0f)] public float lineWidth;
        [Tooltip("Ширина мягкого свечения, мировые единицы (только для Card)")][Min(0f)] public float haloWidth;
        [Range(0f, 1f)] public float haloStrength;
        [Range(0f, 1f)] public float flicker;

        public static HighlightStyle Hover => new HighlightStyle
        {
            color = new Color(0.82f, 0.88f, 1f),
            intensity = 0.6f,
            lineWidth = 0.003f,
            haloWidth = 0.02f,
            haloStrength = 0.3f,
            flicker = 0f
        };

        public static HighlightStyle Selected => new HighlightStyle
        {
            color = new Color(1.6f, 1.05f, 0.4f),
            intensity = 1f,
            lineWidth = 0.006f,
            haloWidth = 0.045f,
            haloStrength = 0.55f,
            flicker = 0.18f
        };

        public HighlightStyle WithIntensity(float value)
        {
            var s = this;
            s.intensity = value;
            return s;
        }

        public static HighlightStyle Lerp(HighlightStyle a, HighlightStyle b, float t) => new HighlightStyle
        {
            color = Color.Lerp(a.color, b.color, t),
            intensity = Mathf.Lerp(a.intensity, b.intensity, t),
            lineWidth = Mathf.Lerp(a.lineWidth, b.lineWidth, t),
            haloWidth = Mathf.Lerp(a.haloWidth, b.haloWidth, t),
            haloStrength = Mathf.Lerp(a.haloStrength, b.haloStrength, t),
            flicker = Mathf.Lerp(a.flicker, b.flicker, t)
        };
    }

    public enum OutlineShape
    {
        Card, 
        Box,   
        Round,  
        Smooth  
    }

    public class BoardHighlight : MonoBehaviour
    {
        [Tooltip("Если пусто, берутся все MeshRenderer на объекте и в детях")]
        [SerializeField] private MeshRenderer[] _targets;
        [Tooltip("Материал с шейдером Noir/ObjectOutline")]
        [SerializeField] private Material _template;
        [SerializeField] private OutlineShape _shape = OutlineShape.Card;
        [SerializeField] private HighlightStyle _hover = HighlightStyle.Hover;
        [SerializeField] private HighlightStyle _selected = HighlightStyle.Selected;
        [SerializeField] private float _duration = 0.15f;
        [SerializeField] private Ease _ease = Ease.OutQuad;

        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int IntensityId = Shader.PropertyToID("_Intensity");
        static readonly int LineId = Shader.PropertyToID("_LineWidth");
        static readonly int HaloId = Shader.PropertyToID("_HaloWidth");
        static readonly int HaloStrengthId = Shader.PropertyToID("_HaloStrength");
        static readonly int FlickerId = Shader.PropertyToID("_Flicker");
        static readonly int PadId = Shader.PropertyToID("_Pad");
        static readonly int SolidId = Shader.PropertyToID("_Solid");
        static readonly int RadialId = Shader.PropertyToID("_Radial");
        static readonly int CullId = Shader.PropertyToID("_Cull");
        static readonly int CenterId = Shader.PropertyToID("_BoundsCenter");
        static readonly int ExtentsId = Shader.PropertyToID("_BoundsExtents");

        private readonly List<Material> _mats = new();
        private readonly List<Mesh> _bakedMeshes = new();
        private Tween _tween;
        private HighlightStyle _current;
        private bool _isHovered, _isSelected;

        void Awake()
        {
            if (_template == null)
            {
                Debug.LogError($"BoardHighlight '{name}': не назначен Template", this);
                return;
            }

            if (_targets == null || _targets.Length == 0)
                _targets = GetComponentsInChildren<MeshRenderer>(true);

            bool card = _shape == OutlineShape.Card;

            foreach (var r in _targets)
            {
                if (r == null)
                    continue;
                var filter = r.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                    continue;

                if (_shape == OutlineShape.Smooth)
                {
                    var baked = BuildSmoothedMesh(filter.sharedMesh);
                    if (baked != null)
                    {
                        filter.sharedMesh = baked;
                        _bakedMeshes.Add(baked);
                    }
                }

                var mat = new Material(_template);
                Bounds b = filter.sharedMesh.bounds;
                mat.SetVector(CenterId, b.center);
                mat.SetVector(ExtentsId, b.extents);
                mat.SetFloat(SolidId, card ? 0f : 1f);
                mat.SetFloat(RadialId, _shape == OutlineShape.Smooth ? 2f : _shape == OutlineShape.Round ? 1f : 0f);
                mat.SetFloat(CullId, card ? (float)CullMode.Off : (float)CullMode.Front);

                var list = new List<Material>(r.sharedMaterials) { mat };
                r.sharedMaterials = list.ToArray();
                _mats.Add(mat);
            }

            _current = _hover.WithIntensity(0f);
            ApplyStyle(_current);
        }

        void OnDisable()
        {
            _tween?.Kill();
            _isHovered = false;
            _isSelected = false;
            ApplyStyle(_current.WithIntensity(0f));
        }

        void OnDestroy()
        {
            foreach (var m in _mats)
                if (m != null)
                    Destroy(m);
            foreach (var mesh in _bakedMeshes)
                if (mesh != null)
                    Destroy(mesh);
        }

        Mesh BuildSmoothedMesh(Mesh source)
        {
            if (!source.isReadable)
            {
                Debug.LogWarning($"BoardHighlight '{name}': у меша '{source.name}' выключен Read/Write, " +
                                 "контур по сглаженным нормалям недоступен, используются обычные нормали", this);
                return null;
            }

            var mesh = Instantiate(source);
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            if (normals == null || normals.Length != vertices.Length)
                return mesh;

            var sums = new Dictionary<Vector3Int, Vector3>();
            for (int i = 0; i < vertices.Length; i++)
            {
                var key = Vector3Int.RoundToInt(vertices[i] * 10000f);
                sums[key] = sums.TryGetValue(key, out var sum) ? sum + normals[i] : normals[i];
            }

            var smooth = new List<Vector3>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
                smooth.Add(sums[Vector3Int.RoundToInt(vertices[i] * 10000f)].normalized);

            mesh.SetUVs(3, smooth);
            return mesh;
        }

        public void SetHovered(bool value)
        {
            if (_isHovered == value)
                return;
            _isHovered = value;
            Refresh();
        }

        public void SetSelected(bool value)
        {
            if (_isSelected == value)
                return;
            _isSelected = value;
            Refresh();
        }

        void Refresh()
        {
            if (_mats.Count == 0)
                return;

            HighlightStyle target = _isSelected ? _selected : _isHovered ? _hover : _current.WithIntensity(0f);

            _tween?.Kill();
            var from = _current;
            _tween = DOVirtual.Float(0f, 1f, _duration, t => ApplyStyle(HighlightStyle.Lerp(from, target, t))).SetEase(_ease).SetLink(gameObject);
        }

        void ApplyStyle(HighlightStyle s)
        {
            _current = s;
            float pad = _shape == OutlineShape.Card ? Mathf.Max(s.lineWidth, s.haloWidth) * 1.05f : s.lineWidth;

            foreach (var m in _mats)
            {
                m.SetColor(ColorId, s.color);
                m.SetFloat(IntensityId, s.intensity);
                m.SetFloat(LineId, s.lineWidth);
                m.SetFloat(HaloId, s.haloWidth);
                m.SetFloat(HaloStrengthId, s.haloStrength);
                m.SetFloat(FlickerId, s.flicker);
                m.SetFloat(PadId, pad);
            }
        }
    }
}