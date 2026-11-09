using UnityEngine;
using UnityEngine.UI;

namespace Prototypes.MedievalChess
{
    /// <summary>The panels and buttons: the start screen, the top bar, the promotion choice and the result. It holds no rules.</summary>
    public sealed class UiView : MonoBehaviour
    {
        public GameObject startPanel, hud, promotionPanel, resultPanel;
        public Text statusText, resultText, difficultyDescription;
        /// <summary>Squire, Knight, Marshal.</summary>
        public Button[] difficultyButtons;
        /// <summary>White, Black, Random.</summary>
        public Button[] sideButtons;
        /// <summary>Full, Quick, Off.</summary>
        public Button[] speedButtons;
        /// <summary>Queen, Rook, Bishop, Knight.</summary>
        public Button[] promotionButtons;
        public Button startButton, resignButton, menuButton, playAgainButton, changeDifficultyButton;
        public Color normalColor = Color.gray;
        public Color selectedColor = Color.yellow;

        public bool MenuVisible => startPanel.activeSelf;
        public bool HudVisible => hud.activeSelf;
        public bool PromotionVisible => promotionPanel.activeSelf;
        public bool ResultVisible => resultPanel.activeSelf;
        public string Status => statusText.text;
        public string Result => resultText.text;

        public void ShowMenu()
        {
            startPanel.SetActive(true);
            hud.SetActive(false);
            promotionPanel.SetActive(false);
            resultPanel.SetActive(false);
        }

        public void ShowHud()
        {
            startPanel.SetActive(false);
            hud.SetActive(true);
            promotionPanel.SetActive(false);
            resultPanel.SetActive(false);
        }

        public void ShowPromotion(bool visible) => promotionPanel.SetActive(visible);

        public void ShowResult(string text)
        {
            resultText.text = text;
            resultPanel.SetActive(true);
            promotionPanel.SetActive(false);
        }

        public void SetStatus(string text) => statusText.text = text;
        public void SetResignAvailable(bool available) => resignButton.interactable = available;

        public void MarkSelection(int difficulty, int side, int speed, string description)
        {
            Mark(difficultyButtons, difficulty);
            Mark(sideButtons, side);
            Mark(speedButtons, speed);
            difficultyDescription.text = description;
        }

        void Mark(Button[] buttons, int selected)
        {
            for (int i = 0; i < buttons.Length; i++)
                buttons[i].GetComponent<Image>().color = i == selected ? selectedColor : normalColor;
        }
    }
}
