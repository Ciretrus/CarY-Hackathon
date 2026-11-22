using DG.Tweening;
using Noir.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using static Unity.Cinemachine.CinemachineOrbitalTransposer;

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
        [SerializeField] LayerMask obstacles = ~0;
        [SerializeField] Volume globalVolume;
        [SerializeField] VolumeProfile inspectProfile;

        Camera cameraView;
        Transform item;
        Pose home;
        Bounds localBounds;
        BoxCollider probe;
        Tween returnMotion;
        float distance, requested, velocity, elapsed;
        bool taken, swapped, acquiring;
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
            acquiring = true;
            localBounds = MeasureItem();
            if (probe == null)
            {
                var holder = new GameObject("Inspect bounds") { hideFlags = HideFlags.HideAndDontSave };
                probe = holder.AddComponent<BoxCollider>();
                probe.enabled = false;
            }
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
            Pose start = new Pose(item.position, item.rotation);
            int steps = Mathf.Max(1, Mathf.CeilToInt(Quaternion.Angle(start.rotation, rotation) / 4f));
            for (int i = 1; i <= steps; i++)
            {
                Quaternion next = Quaternion.Slerp(start.rotation, rotation, (float)i / steps);
                Pose current = new Pose(item.position, item.rotation);
                item.rotation = next;
                Box(out Vector3 centre, out Vector3 half);
                float safe = MinimumDistance(view, ray, next, half);
                Vector3 dest = view.position + ray * Mathf.Max(distance, safe) - (centre - item.position);
                item.rotation = current.rotation;
                Pose candidate = new Pose(Vector3.Lerp(home.position, dest, t * (2f - t)), next);
                if (TryPose(candidate)) continue;
                // Keep the reachable portion of a wheel step near a wall.
                float low = 0f, high = 1f;
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    float mid = (low + high) * 0.5f;
                    item.SetPositionAndRotation(current.position, current.rotation);
                    if (TryPose(new Pose(Vector3.Lerp(current.position, candidate.position, mid),
                        Quaternion.Slerp(current.rotation, next, mid)))) low = mid;
                    else high = mid;
                }
                item.SetPositionAndRotation(Vector3.Lerp(current.position, candidate.position, low),
                    Quaternion.Slerp(current.rotation, next, low));
                break;
            }
            if (acquiring && t >= 1f)
            {
                acquiring = false;
                // A desk edge or an enclosing furniture collider can block the
                // straight trip from the authored pose. Finish on the camera side.
                Box(out Vector3 centre, out Vector3 half);
                float near = MinimumDistance(view, ray, item.rotation, half);
                float desired = Mathf.Max(distance, near);
                Vector3 endCentre = view.position + ray * desired;
                if ((centre - endCentre).sqrMagnitude > 0.0001f &&
                    !PlaceInFreeSpace(view.position, ray, near, desired, centre, half))
                {
                    Debug.LogWarning($"Inspect: no free holding position for {item.name}.", item);
                    Close();
                    return;
                }
            }
            detector.Tick();
        }

        float MinimumDistance(Transform view, Vector3 ray, Quaternion rotation, Vector3 half)
        {
            Vector3 f = Quaternion.Inverse(rotation) * view.forward;
            float depth = Mathf.Abs(f.x) * half.x + Mathf.Abs(f.y) * half.y + Mathf.Abs(f.z) * half.z;
            return (cameraView.nearClipPlane + depth + 0.025f) / Vector3.Dot(ray, view.forward);
        }

        bool PlaceInFreeSpace(Vector3 eye, Vector3 ray, float near, float desired, Vector3 centre, Vector3 half)
        {
            // Exclude positions behind a wall even if the space beyond it is empty.
            foreach (var hit in Physics.RaycastAll(eye, ray, desired, obstacles, QueryTriggerInteraction.Ignore))
                if (IsObstacle(hit.collider)) desired = Mathf.Min(desired, hit.distance - 0.002f);
            if (desired < near) return false;

            for (int step = 0; step <= 16; step++)
            {
                Vector3 candidate = eye + ray * Mathf.Lerp(desired, near, step / 16f);
                bool clear = true;
                foreach (var other in Physics.OverlapBox(candidate, half, item.rotation, obstacles,
                    QueryTriggerInteraction.Ignore))
                {
                    if (!IsObstacle(other)) continue;
                    clear = false;
                    break;
                }
                if (!clear) continue;
                item.position += candidate - centre;
                return true;
            }
            return false;
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

        bool IsObstacle(Collider other) =>
            !other.transform.IsChildOf(item) && !other.transform.IsChildOf(Modes.transform);

        float Penetration(Collider other, Vector3 centre, Quaternion rotation, Vector3 half, out Vector3 away)
        {
            probe.size = half * 2f;
            return Physics.ComputePenetration(probe, centre, rotation, other, other.transform.position,
                other.transform.rotation, out away, out float depth) ? depth : 0f;
        }

        bool TryPose(Pose next)
        {
            Pose old = new Pose(item.position, item.rotation);
            Box(out Vector3 from, out Vector3 oldHalf);
            item.SetPositionAndRotation(next.position, next.rotation);
            Box(out Vector3 to, out Vector3 half);
            Vector3 travel = to - from;
            float length = travel.magnitude;
            if (length > 0.00001f)
            {
                foreach (var hit in Physics.BoxCastAll(from, oldHalf, travel / length, old.rotation,
                    length, obstacles, QueryTriggerInteraction.Ignore))
                {
                    if (!IsObstacle(hit.collider)) continue;
                    // Props may begin resting on, or slightly embedded in, a tabletop.
                    if (Penetration(hit.collider, from, old.rotation, oldHalf, out var away) > 0f &&
                        Vector3.Dot(away, travel) >= 0f) continue;
                    if (hit.distance <= 0f)
                    {
                        Vector3 surface = hit.collider.ClosestPointOnBounds(from);
                        if (Vector3.Dot(from - surface, travel) > 0f) continue;
                    }
                    item.SetPositionAndRotation(old.position, old.rotation);
                    return false;
                }
            }
            foreach (Collider other in Physics.OverlapBox(to, half, next.rotation, obstacles, QueryTriggerInteraction.Ignore))
            {
                if (!IsObstacle(other)) continue;
                float after = Penetration(other, to, next.rotation, half, out _);
                if (after <= 0f || after <= Penetration(other, from, old.rotation, oldHalf, out _) + 0.00001f) continue;
                item.SetPositionAndRotation(old.position, old.rotation);
                return false;
            }
            return true;
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

        void OnDisable() => RestoreVolume();

        protected override void OnExit()
        {
            RestoreVolume();
            detector.End();
            if (item == null) return;
            Transform target = item;
            if (taken)
            {
                Vector3 scale = target.localScale;
                target.DOScale(0f, takeDuration).SetEase(Ease.InBack).OnComplete(() =>
                {
                    target.gameObject.SetActive(false);
                    target.localScale = scale;
                });
                item = null;
                return;
            }

            Pose start = new Pose(target.position, target.rotation);
            // Check the return path as well. The authored resting pose is authoritative.
            returnMotion = DOVirtual.Float(0f, 1f, moveDuration, t =>
            {
                if (target == null || item != target) return;
                TryPose(new Pose(Vector3.Lerp(start.position, home.position, t),
                    Quaternion.Slerp(start.rotation, home.rotation, t)));
            }).SetEase(Ease.OutQuad).SetLink(target.gameObject).OnComplete(() =>
            {
                if (target != null) target.SetPositionAndRotation(home.position, home.rotation);
                item = null;
            }).OnKill(() => returnMotion = null);
        }

        void OnDestroy()
        {
            returnMotion?.Complete();
            RestoreVolume();
            if (probe != null) Destroy(probe.gameObject);
        }
    }
}
