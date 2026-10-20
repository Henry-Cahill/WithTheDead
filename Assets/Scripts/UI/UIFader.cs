using UnityEngine;

namespace CloneZ
{
    /// <summary>
    /// Fades a panel's <see cref="CanvasGroup"/> in and out so it opens and closes
    /// smoothly instead of popping. Interaction/raycast state flips immediately with
    /// intent (so gameplay resumes the instant a panel is dismissed) while the alpha
    /// eases. The host GameObject stays active so this keeps ticking during a fade.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIFader : MonoBehaviour
    {
        [SerializeField] private float _speed = 9f;

        private CanvasGroup _group;
        private float _target;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            enabled = false;
            _group.alpha = 0f;
            _group.interactable = false;
            _group.blocksRaycasts = false;
        }

public void Show()
            {
                _target = 1f;
                enabled = true;
                if (_group != null)
                {
                    _group.interactable = true;
                    _group.blocksRaycasts = true;
                }
            }

        public void Hide()
        {
            _target = 0f;
            enabled = true;
            if (_group != null)
            {
                _group.interactable = false;
                _group.blocksRaycasts = false;
            }
        }

        private void Update()
        {
            if (_group == null) return;
            if (_group.alpha == _target)
            {
                // Already at target, disable per‑frame updates.
                enabled = false;
                return;
            }
            _group.alpha = Mathf.MoveTowards(_group.alpha, _target, _speed * Time.unscaledDeltaTime);
            if (_group.alpha == _target)
            {
                // Reached target after moving, stop updating.
                enabled = false;
            }
        }
    }
}
