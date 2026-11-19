using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Tapshot
{
    /// <summary>
    /// Visual state for a menu card, pill or primary button: selected, hovered, pressed
    /// and keyboard/controller focus.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public class MenuOption : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public enum Style { Card, Pill, Primary }

        [SerializeField] Style style;
        [SerializeField] UnityEngine.UI.Image background;
        [SerializeField] UnityEngine.UI.Image border;
        [SerializeField] UnityEngine.UI.Image focusRing;
        [SerializeField] UnityEngine.UI.Image icon;
        [SerializeField] TMP_Text label;
        [SerializeField] Sprite borderNormal;
        [SerializeField] Sprite borderSelected;

        UnityEngine.UI.Button button;
        bool selected, hovered, pressed, focused;

        public UnityEngine.UI.Button Button => button != null ? button : button = GetComponent<UnityEngine.UI.Button>();
        public bool IsSelected => selected;

        public void Setup(Style style, UnityEngine.UI.Image background, UnityEngine.UI.Image border, UnityEngine.UI.Image focusRing,
            UnityEngine.UI.Image icon, TMP_Text label, Sprite borderNormal, Sprite borderSelected)
        {
            this.style = style;
            this.background = background;
            this.border = border;
            this.focusRing = focusRing;
            this.icon = icon;
            this.label = label;
            this.borderNormal = borderNormal;
            this.borderSelected = borderSelected;
            Refresh();
        }

        void Awake() => Refresh();

        void OnDisable()
        {
            hovered = pressed = focused = false;
            Refresh();
        }

        public void SetSelected(bool value)
        {
            if (selected == value) return;
            selected = value;
            Refresh();
        }

        public void OnPointerEnter(PointerEventData e) { hovered = true; Refresh(); }
        public void OnPointerExit(PointerEventData e) { hovered = false; pressed = false; Refresh(); }
        public void OnPointerDown(PointerEventData e) { pressed = true; Refresh(); }
        public void OnPointerUp(PointerEventData e) { pressed = false; Refresh(); }

        void LateUpdate()
        {
            var es = EventSystem.current;
            bool f = UIInputMode.Navigating && es != null && es.currentSelectedGameObject == gameObject;
            if (f == focused) return;
            focused = f;
            Refresh();
        }

        void Refresh()
        {
            bool lit = hovered || focused;
            if (focusRing != null) focusRing.enabled = focused;

            switch (style)
            {
                case Style.Card:
                    if (background != null) background.color = selected || pressed ? Palette.CardSelected : Palette.Card;
                    if (border != null)
                    {
                        border.sprite = selected && borderSelected != null ? borderSelected : borderNormal;
                        border.color = selected ? Palette.Accent : lit ? Palette.BorderHover : Palette.Border;
                    }
                    if (icon != null) icon.color = selected ? Palette.Accent : lit ? Palette.Text : Palette.Muted;
                    break;

                case Style.Pill:
                    if (background != null) background.color = selected ? Palette.Text : pressed ? Palette.CardSelected : Palette.Card;
                    if (border != null) border.color = selected ? Palette.Text : lit ? Palette.BorderHover : Palette.Border;
                    if (label != null)
                    {
                        label.color = selected ? Palette.Background : Palette.Text;
                        label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
                    }
                    break;

                case Style.Primary:
                    if (background != null) background.color = pressed ? Palette.AccentPressed : lit ? Palette.AccentHover : Palette.Accent;
                    if (label != null) label.color = Palette.Background;
                    break;
            }
        }
    }
}
