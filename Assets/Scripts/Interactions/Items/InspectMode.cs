using System.Collections.Generic;
using DG.Tweening;
using Noir.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Noir.Interaction
{
    public class InspectMode : PlayerMode
    {
        [SerializeField] Transform inspectPoint;
        [SerializeField] ClueDetector detector;
        [SerializeField] float rotateSpeed = 0.3f;
        [SerializeField] float moveDuration = 0.25f;
        [SerializeField] float takeDuration = 0.2f;
        [SerializeField] Camera inspectCamera;
        [SerializeField, Min(0.05f)] float holdDistance = 0.38f;
        [SerializeField, Min(0.05f)] float minDistance = 0.18f;
        [SerializeField, Min(0.05f)] float maxDistance = 0.75f;
        [SerializeField, Min(0.01f)] float distanceStep = 0.05f;
        [SerializeField, Min(0f)] float zoomSmoothTime = 0.08f;
        [SerializeField] Volume globalVolume;
        [SerializeField] VolumeProfile inspectProfile;

        Camera cameraView;
        Transform item;
        Pose home;
        Bounds localBounds;
        readonly List<Collider> suspended = new List<Collider>();
        readonly List<Rigidbody> dynamicBodies = new List<Rigidbody>();
        int reservedLayer = -1;
        Camera overlay;
        UniversalAdditionalCameraData baseData;
        int foregroundMask;
        bool baseIncludedLayer;
        readonly Dictionary<GameObject, int> originalLayers = new Dictionary<GameObject, int>();
        Tween returnMotion;
        public bool IsReturning => returnMotion.IsActive();
        float distance, requested, velocity, elapsed;
        bool taken, swapped;
        VolumeProfile previousShared, previousInstance;

        public void Inspect(Transform target)
        {
            if (target == null || Modes.Current != null) return;
            returnMotion?.Complete();
            item = target;
            if (!Open()) item = null;
        }

        protected override void OnEnter()
        {
            item.DOKill();
            cameraView = inspectCamera != null ? inspectCamera : Camera.main;
            home = new Pose(item.position, item.rotation);
            distance = requested = Mathf.Clamp(holdDistance, minDistance, Mathf.Max(minDistance, maxDistance));
            elapsed = velocity = 0f;
            taken = false;
            localBounds = MeasureItem();
            foreach (var collider in item.GetComponentsInChildren<Collider>(true))
            {
                if (!collider.enabled) continue;
                suspended.Add(collider);
                collider.enabled = false;
            }
            foreach (var body in item.GetComponentsInChildren<Rigidbody>(true))
            {
                if (body.isKinematic) continue;
                dynamicBodies.Add(body);
                body.isKinematic = true;
            }
            if (cameraView != null) BeginForeground();
            if (globalVolume != null && inspectProfile != null)
            {
                previousShared = globalVolume.sharedProfile;
                previousInstance = globalVolume.HasInstantiatedProfile() ? globalVolume.profile : null;
                globalVolume.profile = null;
                globalVolume.sharedProfile = inspectProfile;
                swapped = true;
            }
            detector.Begin(item);
        }

        Bounds MeasureItem()
        {
            Bounds result = default;
            bool found = false;
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))) continue;
                Bounds b = renderer.localBounds;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = new Vector3((i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y, (i & 4) == 0 ? b.min.z : b.max.z);
                    Vector3 p = item.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                    if (found) result.Encapsulate(p);
                    else { result = new Bounds(p, Vector3.zero); found = true; }
                }
            }
            return found ? result : new Bounds(Vector3.zero, Vector3.one * 0.1f);
        }

        protected override void Tick()
        {
            if (item == null || cameraView == null) { Close(); return; }
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            if (mouse.rightButton.wasPressedThisFrame) { Close(); return; }
            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame &&
                detector.TryCollect() && !detector.HasRemaining)
            {
                taken = true;
                Close();
                return;
            }

            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
                requested = Mathf.Clamp(requested - Mathf.Sign(scroll) * distanceStep, minDistance, Mathf.Max(minDistance, maxDistance));
            distance = zoomSmoothTime <= 0f ? requested : Mathf.SmoothDamp(distance, requested, ref velocity, zoomSmoothTime);
            Transform view = cameraView.transform;
            Transform anchor = inspectPoint != null ? inspectPoint : view;
            Vector3 ray = (anchor.position - view.position).normalized;
            if (Vector3.Dot(ray, view.forward) < 0.2f) ray = view.forward;
            Vector2 delta = mouse.delta.ReadValue() * rotateSpeed;
            Quaternion rotation = Quaternion.AngleAxis(delta.y, anchor.right) *
                Quaternion.AngleAxis(-delta.x, anchor.up) * item.rotation;

            // Move the visual centre, even on props whose pivot is at one edge.
            elapsed += Time.deltaTime;
            float t = moveDuration > 0f ? Mathf.Clamp01(elapsed / moveDuration) : 1f;
            item.rotation = rotation;
            Box(out Vector3 centre, out Vector3 half);
            float safe = MinimumDistance(view, ray, rotation, half);
            Vector3 dest = view.position + ray * Mathf.Max(distance, safe) - (centre - item.position);
            item.position = Vector3.Lerp(home.position, dest, t * (2f - t));
            detector.Tick();
        }

        float MinimumDistance(Transform view, Vector3 ray, Quaternion rotation, Vector3 half)
        {
            Vector3 f = Quaternion.Inverse(rotation) * view.forward;
            float depth = Mathf.Abs(f.x) * half.x + Mathf.Abs(f.y) * half.y + Mathf.Abs(f.z) * half.z;
            return (cameraView.nearClipPlane + depth + 0.025f) / Vector3.Dot(ray, view.forward);
        }

        // Project the scaled local box into the item's rotation frame. This also
        // encloses a sheared mesh under a non-uniformly scaled parent.
        void Box(out Vector3 centre, out Vector3 half)
        {
            centre = item.TransformPoint(localBounds.center);
            Quaternion inverse = Quaternion.Inverse(item.rotation);
            Vector3 e = localBounds.extents;
            Vector3 x = inverse * item.TransformVector(Vector3.right * e.x);
            Vector3 y = inverse * item.TransformVector(Vector3.up * e.y);
            Vector3 z = inverse * item.TransformVector(Vector3.forward * e.z);
            half = new Vector3(Mathf.Abs(x.x)+Mathf.Abs(y.x)+Mathf.Abs(z.x),
                Mathf.Abs(x.y)+Mathf.Abs(y.y)+Mathf.Abs(z.y), Mathf.Abs(x.z)+Mathf.Abs(y.z)+Mathf.Abs(z.z));
            half = Vector3.Max(half, Vector3.one * 0.001f);
        }

        void BeginForeground()
        {
            baseData = cameraView.GetUniversalAdditionalCameraData();
            var stack = baseData.cameraStack;
            if (stack == null) return;

            // Reserve an unused rendering layer for this inspection only.
            if (reservedLayer < 0)
            {
                int occupied = 0;
                foreach (var renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    occupied |= 1 << renderer.gameObject.layer;
                for (int candidate = 31; candidate >= 8; candidate--)
                {
                    if ((occupied & (1 << candidate)) != 0 || !string.IsNullOrEmpty(LayerMask.LayerToName(candidate))) continue;
                    reservedLayer = candidate;
                    break;
                }
            }
            if (reservedLayer < 0) return;
            int layer = reservedLayer;
            foregroundMask = 1 << layer;
            foreach (var renderer in item.GetComponentsInChildren<Renderer>(true))
            {
                GameObject part = renderer.gameObject;
                if (!originalLayers.ContainsKey(part))
                {
                    originalLayers.Add(part, part.layer);
                    part.layer = layer;
                }
            }
            baseIncludedLayer = (cameraView.cullingMask & foregroundMask) != 0;
            cameraView.cullingMask &= ~foregroundMask;
            var holder = new GameObject("Inspection foreground");
            holder.transform.SetParent(cameraView.transform, false);
            overlay = holder.AddComponent<Camera>();
            overlay.CopyFrom(cameraView);
            overlay.cullingMask = foregroundMask;
            overlay.useOcclusionCulling = false;
            var data = overlay.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Overlay;            
            stack.Add(overlay);
        }

        void LateUpdate()
        {
            if (overlay != null && cameraView != null)
            {
                overlay.nearClipPlane = cameraView.nearClipPlane;
                overlay.farClipPlane = cameraView.farClipPlane;
                overlay.projectionMatrix = cameraView.projectionMatrix;
            }
        }

        void EndForeground()
        {
            if (overlay != null)
            {
                overlay.enabled = false;
                if (baseData != null) baseData.cameraStack?.Remove(overlay);
                if (cameraView != null && baseIncludedLayer) cameraView.cullingMask |= foregroundMask;
                Destroy(overlay.gameObject);
            }
            overlay = null;
            foreach (var pair in originalLayers)
                if (pair.Key != null) pair.Key.layer = pair.Value;
            originalLayers.Clear();
        }

        void RestoreVolume()
        {
            if (!swapped) return;
            swapped = false;
            if (globalVolume == null) return;
            var temporary = globalVolume.HasInstantiatedProfile() ? globalVolume.profile : null;
            globalVolume.sharedProfile = previousShared;
            globalVolume.profile = previousInstance;
            if (temporary != null && temporary != previousInstance && temporary != inspectProfile) Destroy(temporary);
        }

        void RestorePhysics()
        {
            foreach (var collider in suspended)
                if (collider != null) collider.enabled = true;
            suspended.Clear();
            foreach (var body in dynamicBodies)
                if (body != null) body.isKinematic = false;
            dynamicBodies.Clear();
        }

        void OnDisable()
        {
            RestoreVolume();
            // PlayerModes.Exit clears Current before normal Deactivate/OnExit.
            if (item != null && Modes != null && Modes.Current == this)
            {
                item.SetPositionAndRotation(home.position, home.rotation);
                EndForeground();
                RestorePhysics();
                detector.End();
                item = null;
                Close();
            }
        }

        protected override void OnExit()
        {
            RestoreVolume();
            detector.End();
            if (item == null)
            {
                EndForeground();
                RestorePhysics();
                return;
            }
            Transform target = item;
            Pose start = new Pose(target.position, target.rotation);
            Vector3 scale = target.localScale;
            bool collected = taken;
            float duration = collected ? takeDuration : moveDuration;

            returnMotion = DOVirtual.Float(0f, 1f, duration, t =>
            {
                if (target == null) return;
                if (collected) target.localScale = Vector3.LerpUnclamped(scale, Vector3.zero, t);
                else target.SetPositionAndRotation(Vector3.Lerp(start.position, home.position, t),
                    Quaternion.Slerp(start.rotation, home.rotation, t));
            }).SetEase(collected ? Ease.InBack : Ease.OutQuad).SetAutoKill(true)
                .SetLink(target.gameObject).OnKill(() =>
                {
                    // Autokill runs after completion, and also handles an interrupted return.
                    if (target != null)
                    {
                        if (collected) target.gameObject.SetActive(false);
                        target.SetPositionAndRotation(home.position, home.rotation);
                        target.localScale = scale;
                    }
                    EndForeground();
                    RestorePhysics();
                    item = null;
                    returnMotion = null;
                });
        }

        void OnDestroy()
        {
            returnMotion?.Kill(true);
            EndForeground();
            RestoreVolume();
            RestorePhysics();
        }
    }
}
