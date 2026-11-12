using UnityEngine;

namespace Noir.Player
{
    public class PlayerModes : MonoBehaviour
    {
        [SerializeField] private Behaviour[] _explorationControls;
        [SerializeField] private GameObject[] _explorationHud;

        public PlayerMode Current { get; private set; }

        public T Get<T>() where T : PlayerMode => GetComponentInChildren<T>(true);

        public bool Enter(PlayerMode mode)
        {
            if (Current != null) return false;

            Current = mode;
            SetExploration(false);
            mode.Activate();
            return true;
        }

        public void Exit()
        {
            if (Current == null) return;

            var mode = Current;
            Current = null;
            mode.Deactivate();
            SetExploration(true);
        }

        void SetExploration(bool active)
        {
            foreach (var c in _explorationControls) c.enabled = active;
            foreach (var h in _explorationHud) h.SetActive(active);
        }
    }
}
