using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Tapshot
{
    /// <summary>
    /// Stand-in for a drill scene: shows the settings it received and returns to the menu.
    /// </summary>
    public class DrillPlaceholder : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text details;
        [SerializeField] MenuOption backButton;

        bool loading;

        public void Setup(TMP_Text title, TMP_Text details, MenuOption backButton)
        {
            this.title = title;
            this.details = details;
            this.backButton = backButton;
        }

        void Awake()
        {
            if (title != null) title.text = SceneManager.GetActiveScene().name;
            if (details != null)
            {
                details.text =
                    $"<color=#9AA4B2>DrillType</color><pos=45%>{SessionSettings.DisplayName(SessionSettings.DrillType)}\n" +
                    $"<color=#9AA4B2>TargetSize</color><pos=45%>{SessionSettings.TargetSize}\n" +
                    $"<color=#9AA4B2>RoundSeconds</color><pos=45%>{SessionSettings.RoundSeconds}s";
            }
            if (backButton != null) backButton.Button.onClick.AddListener(BackToMenu);
        }

        void Start()
        {
            if (EventSystem.current != null && backButton != null)
                EventSystem.current.SetSelectedGameObject(backButton.gameObject);
        }

        void Update()
        {
            UIInputMode.Poll();

            var es = EventSystem.current;
            if (es != null && backButton != null && UIInputMode.Navigating && es.currentSelectedGameObject == null)
                es.SetSelectedGameObject(backButton.gameObject);

            if (UIInputMode.CancelPressed()) BackToMenu();
        }

        public void BackToMenu()
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene(SessionSettings.MenuScene);
        }
    }
}
