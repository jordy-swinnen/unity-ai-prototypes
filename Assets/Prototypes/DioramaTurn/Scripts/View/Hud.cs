using UnityEngine;
using UnityEngine.UI;

namespace Prototypes.DioramaTurn
{
    /// <summary>The uGUI panels and labels. The game shows and hides them; the builder makes them.</summary>
    public sealed class Hud : MonoBehaviour
    {
        public GameObject titlePanel;
        public GameObject playingPanel;
        public GameObject completePanel;
        public GameObject finishPanel;
        public Text roomNameText;
        public Text turnText;
        public Text completeText;
        public Text hintText;
        public Button turnLeftButton;
        public Button turnRightButton;
        public Button restartButton;
        public Button nextButton;
        public Button playAgainButton;

        public void ShowTitle()
        {
            SetPanels(title: true, playing: false, complete: false, finish: false);
        }

        public void ShowPlaying(string roomName)
        {
            SetPanels(title: false, playing: true, complete: false, finish: false);
            if (roomNameText != null) roomNameText.text = roomName;
        }

        public void SetTurns(int turns)
        {
            if (turnText != null) turnText.text = $"Turns: {turns}";
        }

        public void ShowComplete(string roomName, int turns, int par, int stars)
        {
            SetPanels(title: false, playing: false, complete: true, finish: false);
            if (completeText != null)
                completeText.text = $"{roomName} complete\n{turns} turns — par {par}\n{stars} of 3 stars";
        }

        public void ShowFinish()
        {
            SetPanels(title: false, playing: false, complete: false, finish: true);
        }

        void SetPanels(bool title, bool playing, bool complete, bool finish)
        {
            if (titlePanel != null) titlePanel.SetActive(title);
            if (playingPanel != null) playingPanel.SetActive(playing);
            if (completePanel != null) completePanel.SetActive(complete);
            if (finishPanel != null) finishPanel.SetActive(finish);
        }
    }
}
