using DG.Tweening;
using Noir.Interaction;
using UnityEngine;

namespace Noir.UI
{
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] Interactor interactor;
        [SerializeField] float hoverScale = 1.8f;
        [SerializeField] float duration = 0.15f;
        [SerializeField] Ease ease = Ease.OutQuad;

        Tween tween;

        void OnEnable() => interactor.CurrentChanged += OnCurrentChanged;

        void OnDisable()
        {
            if (interactor != null) interactor.CurrentChanged -= OnCurrentChanged;
            tween?.Kill();
            transform.localScale = Vector3.one;
        }

        void OnCurrentChanged(IInteractable target)
        {
            tween?.Kill();
            tween = transform
                .DOScale(target != null ? hoverScale : 1f, duration)
                .SetEase(ease)
                .SetLink(gameObject);
        }
    }
}