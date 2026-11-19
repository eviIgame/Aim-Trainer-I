using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Tapshot
{
    /// <summary>
    /// Main menu: one drill, one target size and one round length are always selected.
    /// Start saves them to <see cref="SessionSettings"/> and loads the drill scene.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [SerializeField] MenuOption[] drillCards;   // order matches DrillType
        [SerializeField] MenuOption[] sizePills;    // order matches TargetSize
        [SerializeField] MenuOption[] lengthPills;  // order matches SessionSettings.RoundLengths
        [SerializeField] MenuOption startButton;

        int drill, size, length;
        bool loading;

        public void Setup(MenuOption[] drillCards, MenuOption[] sizePills, MenuOption[] lengthPills, MenuOption startButton)
        {
            this.drillCards = drillCards;
            this.sizePills = sizePills;
            this.lengthPills = lengthPills;
            this.startButton = startButton;
        }

        void Awake()
        {
            // Restore the last choices (defaults: Grid, M, 30s).
            drill = Mathf.Clamp((int)SessionSettings.DrillType, 0, drillCards.Length - 1);
            size = Mathf.Clamp((int)SessionSettings.TargetSize, 0, sizePills.Length - 1);
            length = Mathf.Max(0, System.Array.IndexOf(SessionSettings.RoundLengths, SessionSettings.RoundSeconds));

            for (int i = 0; i < drillCards.Length; i++)
            {
                int index = i;
                drillCards[i].Button.onClick.AddListener(() => { drill = index; ApplySelection(); });
            }
            for (int i = 0; i < sizePills.Length; i++)
            {
                int index = i;
                sizePills[i].Button.onClick.AddListener(() => { size = index; ApplySelection(); });
            }
            for (int i = 0; i < lengthPills.Length; i++)
            {
                int index = i;
                lengthPills[i].Button.onClick.AddListener(() => { length = index; ApplySelection(); });
            }
            startButton.Button.onClick.AddListener(StartDrill);

            ApplySelection();
        }

        void Start()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(drillCards[drill].gameObject);
        }

        void Update()
        {
            UIInputMode.Poll();

            var es = EventSystem.current;
            if (es != null && UIInputMode.Navigating &&
                (es.currentSelectedGameObject == null || !es.currentSelectedGameObject.activeInHierarchy))
                es.SetSelectedGameObject(drillCards[drill].gameObject);

            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
                StartDrill();
        }

        void ApplySelection()
        {
            for (int i = 0; i < drillCards.Length; i++) drillCards[i].SetSelected(i == drill);
            for (int i = 0; i < sizePills.Length; i++) sizePills[i].SetSelected(i == size);
            for (int i = 0; i < lengthPills.Length; i++) lengthPills[i].SetSelected(i == length);
            RefreshNavigation();
        }

        // Explicit navigation; vertical moves land on the currently selected item of the target group.
        void RefreshNavigation()
        {
            var card = drillCards[drill].Button;
            var sizeSel = sizePills[size].Button;
            var lengthSel = lengthPills[length].Button;
            var start = startButton.Button;

            for (int i = 0; i < drillCards.Length; i++)
            {
                UnityEngine.UI.Selectable down = i < 2 ? sizeSel : i == 2 ? lengthSel : start;
                SetNav(drillCards[i].Button,
                    left: i > 0 ? drillCards[i - 1].Button : null,
                    right: i < drillCards.Length - 1 ? drillCards[i + 1].Button : null,
                    up: null, down: down);
            }
            for (int i = 0; i < sizePills.Length; i++)
            {
                SetNav(sizePills[i].Button,
                    left: i > 0 ? sizePills[i - 1].Button : null,
                    right: i < sizePills.Length - 1 ? sizePills[i + 1].Button : lengthPills[0].Button,
                    up: card, down: null);
            }
            for (int i = 0; i < lengthPills.Length; i++)
            {
                SetNav(lengthPills[i].Button,
                    left: i > 0 ? lengthPills[i - 1].Button : sizePills[sizePills.Length - 1].Button,
                    right: i < lengthPills.Length - 1 ? lengthPills[i + 1].Button : start,
                    up: card, down: null);
            }
            SetNav(start, left: lengthPills[lengthPills.Length - 1].Button, right: null, up: card, down: null);
        }

        static void SetNav(UnityEngine.UI.Selectable s, UnityEngine.UI.Selectable left, UnityEngine.UI.Selectable right,
            UnityEngine.UI.Selectable up, UnityEngine.UI.Selectable down)
        {
            s.navigation = new UnityEngine.UI.Navigation
            {
                mode = UnityEngine.UI.Navigation.Mode.Explicit,
                selectOnLeft = left,
                selectOnRight = right,
                selectOnUp = up,
                selectOnDown = down,
            };
        }

        public void StartDrill()
        {
            if (loading) return;
            loading = true;

            SessionSettings.DrillType = (DrillType)drill;
            SessionSettings.TargetSize = (TargetSize)size;
            SessionSettings.RoundSeconds = SessionSettings.RoundLengths[length];

            SceneManager.LoadScene(SessionSettings.SceneFor(SessionSettings.DrillType));
        }
    }
}
