using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpaceExplorer
{
    /// <summary>
    /// Hover / select / press feedback for menu buttons, replacing Unity's default
    /// color tint:
    ///   - highlighted (mouse over or keyboard-selected): colors brighten, button grows 4%
    ///   - pressed: button shrinks slightly
    ///   - click: plays a short UI sound
    /// Mouse hover also selects the button, so mouse and keyboard (arrows + Enter)
    /// always agree on which button is active. Uses unscaled time so it also
    /// works on the pause screen (Time.timeScale = 0).
    /// </summary>
    public class ButtonFx : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler, IPointerClickHandler, ISubmitHandler
    {
        [SerializeField] private Image fill;
        [SerializeField] private Image border;
        [SerializeField] private Graphic label;

        [SerializeField] private Color fillNormal = Color.white;
        [SerializeField] private Color fillHighlight = Color.white;
        [SerializeField] private Color borderNormal = Color.white;
        [SerializeField] private Color borderHighlight = Color.white;
        [SerializeField] private Color labelNormal = Color.white;
        [SerializeField] private Color labelHighlight = Color.white;

        [Tooltip("Select this button when the mouse hovers it (off for in-game HUD buttons).")]
        [SerializeField] private bool selectOnHover = true;

        private bool hovered;
        private bool selected;
        private bool pressed;
        private float highlight; // 0..1, animated

        /// <summary>Editor-time setup used by the scene builder.</summary>
        public void Setup(Image fillImage, Image borderImage, Graphic labelGraphic,
            Color fillN, Color fillH, Color borderN, Color borderH, Color labelN, Color labelH, bool hoverSelects)
        {
            fill = fillImage;
            border = borderImage;
            label = labelGraphic;
            fillNormal = fillN;
            fillHighlight = fillH;
            borderNormal = borderN;
            borderHighlight = borderH;
            labelNormal = labelN;
            labelHighlight = labelH;
            selectOnHover = hoverSelects;
            Apply(0f);
        }

        private void OnEnable()
        {
            hovered = pressed = false;
            highlight = 0f;
            Apply(0f);
        }

        private void Update()
        {
            float target = hovered || selected ? 1f : 0f;
            highlight = Mathf.MoveTowards(highlight, target, Time.unscaledDeltaTime * 8f);

            float scale = 1f + 0.04f * highlight - (pressed ? 0.05f : 0f);
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * scale, Time.unscaledDeltaTime * 18f);
            Apply(highlight);
        }

        private void Apply(float h)
        {
            if (fill != null) fill.color = Color.Lerp(fillNormal, fillHighlight, h);
            if (border != null) border.color = Color.Lerp(borderNormal, borderHighlight, h);
            if (label != null) label.color = Color.Lerp(labelNormal, labelHighlight, h);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            if (selectOnHover && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            pressed = false;
        }

        public void OnPointerDown(PointerEventData eventData) => pressed = true;

        public void OnPointerUp(PointerEventData eventData) => pressed = false;

        public void OnSelect(BaseEventData eventData) => selected = true;

        public void OnDeselect(BaseEventData eventData) => selected = false;

        public void OnPointerClick(PointerEventData eventData) => SoundManager.Play(Sfx.Click, 0.6f);

        public void OnSubmit(BaseEventData eventData) => SoundManager.Play(Sfx.Click, 0.6f);
    }
}
