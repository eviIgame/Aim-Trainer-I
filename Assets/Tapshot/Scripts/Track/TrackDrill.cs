using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Tapshot
{
    /// <summary>
    /// Runs one round of the Track drill: sizes the ball from the menu's target-size choice,
    /// counts down the round length, scores hits and shows a results screen.
    /// Esc returns to the menu; click after the round restarts.
    /// </summary>
    public class TrackDrill : MonoBehaviour
    {
        [SerializeField] PlayerController player;
        [SerializeField] TrackTarget target;
        [SerializeField] Crosshair crosshair;
        [SerializeField] TMP_Text timeText, scoreText, accuracyText, resultText;
        [SerializeField] Vector3 planeCenter = new Vector3(0f, 1.6f, 14f);
        [SerializeField] Vector2 halfExtents = new Vector2(6f, 3f);
        [SerializeField] float[] diameters = { 0.6f, 1.0f, 1.6f };   // S, M, L

        int hits, shots;
        float timeLeft;
        bool finished, loading;

        public void Setup(PlayerController player, TrackTarget target, Crosshair crosshair,
            TMP_Text time, TMP_Text score, TMP_Text accuracy, TMP_Text result)
        {
            this.player = player;
            this.target = target;
            this.crosshair = crosshair;
            timeText = time;
            scoreText = score;
            accuracyText = accuracy;
            resultText = result;
        }

        void Awake()
        {
            timeLeft = SessionSettings.RoundSeconds;
            int size = Mathf.Clamp((int)SessionSettings.TargetSize, 0, diameters.Length - 1);
            target.Configure(planeCenter, halfExtents, diameters[size]);
            resultText.gameObject.SetActive(false);
            player.Shot += OnShot;
            Refresh();
        }

        void OnDestroy()
        {
            if (player != null) player.Shot -= OnShot;
        }

        void OnShot(bool hit, TrackTarget t)
        {
            if (finished) return;
            shots++;
            if (hit)
            {
                hits++;
                t.OnHit();
                if (crosshair != null) crosshair.Flash();
            }
            Refresh();
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                BackToMenu();
                return;
            }

            if (finished)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) Restart();
                return;
            }

            timeLeft -= Time.deltaTime;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                Finish();
            }
            Refresh();
        }

        void Refresh()
        {
            timeText.text = Mathf.CeilToInt(timeLeft).ToString();
            scoreText.text = $"<color=#9AA4B2>HITS</color>  {hits}";
            float acc = shots > 0 ? 100f * hits / shots : 0f;
            accuracyText.text = $"<color=#9AA4B2>ACC</color>  {acc:0}%";
        }

        void Finish()
        {
            finished = true;
            target.Moving = false;
            player.InputEnabled = false;
            PlayerController.SetCursorLocked(false);
            float acc = shots > 0 ? 100f * hits / shots : 0f;
            resultText.text =
                $"ROUND COMPLETE\n<size=48><color=#FF7A1A>{hits}</color> hits  ·  {acc:0}% accuracy</size>\n" +
                "<size=18><color=#9AA4B2>Click to play again  ·  Esc for menu</color></size>";
            resultText.gameObject.SetActive(true);
        }

        void Restart()
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void BackToMenu()
        {
            if (loading) return;
            loading = true;
            SceneManager.LoadScene(SessionSettings.MenuScene);
        }
    }
}
