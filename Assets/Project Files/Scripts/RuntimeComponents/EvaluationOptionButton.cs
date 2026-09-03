using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace TutorialFramework.RuntimeComponents
{
    /// <summary>
    /// Component placed on each pre-placed option button in the scene hierarchy.
    /// Manages selection state, child correct (✔) and wrong (✖) feedback images.
    /// </summary>
    [DisallowMultipleComponent]
    public class EvaluationOptionButton : MonoBehaviour
    {
        [Header("Identity")]
        [Tooltip("Stable unique ID of this option matching the ScriptableObject (e.g. 'F1', 'opt_convex', 'opt_a').")]
        [SerializeField] private string optionID = "Option_1";

        [Header("UI Components")]
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI optionText;
        [SerializeField] private Image buttonImage;
        [SerializeField] private Image iconImage;

        [Header("Feedback Icons & Halo")]
        [Tooltip("Green checkmark icon enabled when this option is correct.")]
        [SerializeField] private GameObject correctIcon;

        [Tooltip("Red cross icon enabled when this option is wrong.")]
        [SerializeField] private GameObject wrongIcon;

        [Tooltip("Halo, border, or glow indicator shown when this option is selected.")]
        [SerializeField] private GameObject selectedHalo;

        [Header("Color Highlights (Optional)")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color correctColor = new Color(0.2f, 0.85f, 0.4f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.95f, 0.3f, 0.3f, 1f);

        public string OptionID => optionID;
        public Button Button => EnsureButton();

        private Action<string> onClickCallback;

        private void Awake()
        {
            EnsureButton();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClickCallback?.Invoke(optionID));
            }
            ResetVisuals();
        }

        private Button EnsureButton()
        {
            if (button == null) button = GetComponent<Button>();
            return button;
        }

        public void Initialize(Action<string> onClick)
        {
            onClickCallback = onClick;
            SetInteractable(true);
            ResetVisuals();
        }

        public void SetInteractable(bool interactable)
        {
            EnsureButton();
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        public void SetText(string text)
        {
            if (optionText == null) optionText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (optionText != null && !string.IsNullOrEmpty(text))
            {
                optionText.text = text;
            }
        }

        public void SetImage(Sprite sprite)
        {
            if (iconImage == null && buttonImage != null) iconImage = buttonImage;
            if (iconImage != null && sprite != null)
            {
                iconImage.sprite = sprite;
                iconImage.gameObject.SetActive(true);
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (selectedHalo != null) selectedHalo.SetActive(isSelected);

            if (buttonImage != null)
            {
                buttonImage.color = isSelected ? selectedColor : normalColor;
            }
        }

        public void ShowFeedback(bool isCorrect)
        {
            if (selectedHalo != null) selectedHalo.SetActive(true);

            if (buttonImage != null)
            {
                buttonImage.color = isCorrect ? correctColor : wrongColor;
            }

            if (isCorrect)
            {
                if (correctIcon != null)
                {
                    correctIcon.SetActive(true);
                    correctIcon.transform.DOKill();
                    correctIcon.transform.localScale = Vector3.zero;
                    correctIcon.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
                }
                if (wrongIcon != null) wrongIcon.SetActive(false);
            }
            else
            {
                if (wrongIcon != null)
                {
                    wrongIcon.SetActive(true);
                    wrongIcon.transform.DOKill();
                    wrongIcon.transform.localScale = Vector3.zero;
                    wrongIcon.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
                }
                if (correctIcon != null) correctIcon.SetActive(false);
            }
        }

        public void ResetVisuals()
        {
            if (correctIcon != null) correctIcon.SetActive(false);
            if (wrongIcon != null) wrongIcon.SetActive(false);
            if (selectedHalo != null) selectedHalo.SetActive(false);

            if (buttonImage != null)
            {
                buttonImage.color = normalColor;
            }
        }
    }
}
