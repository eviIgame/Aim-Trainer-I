using UnityEngine;
using UnityEngine.UI;

namespace Tapshot
{
    /// <summary>
    /// Crosshair made of standard uGUI Images (four bars and a centre dot). Flashes on a hit.
    /// </summary>
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] Image[] parts;
        [SerializeField] Color normalColor = new Color(0.91f, 0.925f, 0.949f, 1f);
        [SerializeField] Color hitColor = new Color(1f, 0.478f, 0.102f, 1f);
        [SerializeField] float flashSpeed = 8f;

        float flash;

        public void Setup(Image[] parts)
        {
            this.parts = parts;
            Apply(0f);
        }

        public void Flash() => flash = 1f;

        void Update()
        {
            if (flash <= 0f) return;
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime * flashSpeed);
            Apply(flash);
        }

        void Apply(float t)
        {
            if (parts == null) return;
            Color c = Color.Lerp(normalColor, hitColor, t);
            foreach (var p in parts) if (p != null) p.color = c;
        }
    }
}
