using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace TutorialFramework.RuntimeComponents
{
    [DisallowMultipleComponent]
    public class TableCalculatorKeypad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("Draggable RectTransform")]
        [SerializeField] private RectTransform keypadRect;
        [SerializeField] private RectTransform dragHandleBar;
        [SerializeField] private RectTransform boundaryContainer;
        [SerializeField] private float boundaryPadding = 15f;

        [Header("Global Character Limit")]
        [SerializeField] private int maxCharacterLimit = 6;

        [Header("Keypad Buttons (0-9, ., Backspace, Clear, Submit)")]
        [SerializeField] private List<Button> numberButtons = new List<Button>();
        [SerializeField] private Button dotButton;
        [SerializeField] private Button backspaceButton;
        [SerializeField] private Button clearButton;
        [SerializeField] private Button submitButton;

        [Header("Autofill Button")]
        [SerializeField] private Button autofillButton;

        private TMP_InputField activeTargetInput;
        private Action onSubmitted;
        private Action onAutofillClicked;
        private Canvas rootCanvas;
        private Camera uiCamera;

        private bool isPointerHeld;
        private Vector2 previousPointerPos;

        private void Awake()
        {
            if (keypadRect == null) keypadRect = GetComponent<RectTransform>();
            rootCanvas = GetComponentInParent<Canvas>();

            if (rootCanvas != null)
            {
                uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
                if (boundaryContainer == null)
                {
                    boundaryContainer = rootCanvas.GetComponent<RectTransform>();
                }
            }

            var img = GetComponent<Image>();
            if (img == null)
            {
                img = gameObject.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
            }
            img.raycastTarget = true;

            for (int i = 0; i < numberButtons.Count; i++)
            {
                int digit = i;
                if (numberButtons[i] != null)
                {
                    numberButtons[i].onClick.RemoveAllListeners();
                    numberButtons[i].onClick.AddListener(() => AppendText(digit.ToString()));
                }
            }

            if (dotButton != null)
            {
                dotButton.onClick.RemoveAllListeners();
                dotButton.onClick.AddListener(() => AppendText("."));
            }
            if (backspaceButton != null)
            {
                backspaceButton.onClick.RemoveAllListeners();
                backspaceButton.onClick.AddListener(OnBackspace);
            }
            if (clearButton != null)
            {
                clearButton.onClick.RemoveAllListeners();
                clearButton.onClick.AddListener(OnClear);
            }
            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmit);
            }

            if (autofillButton != null)
            {
                autofillButton.onClick.RemoveAllListeners();
                autofillButton.onClick.AddListener(OnAutofillPressed);
                autofillButton.gameObject.SetActive(false);
            }
        }

        public void BindToInput(TMP_InputField targetField, Action submitCallback, Action autofillCallback)
        {
            activeTargetInput = targetField;
            onSubmitted = submitCallback;
            onAutofillClicked = autofillCallback;

            if (activeTargetInput != null)
            {
                activeTargetInput.characterLimit = maxCharacterLimit;
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(OnSubmit);
            }

            SetAutofillVisible(false);

            gameObject.SetActive(true);
            transform.DOKill();
            transform.localScale = Vector3.zero;
            transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);

            Debug.Log($"[TableCalculatorKeypad] Successfully bound to target input: '{targetField?.name}' (interactable={targetField?.interactable})");
        }

        public void SetAutofillVisible(bool visible)
        {
            if (autofillButton != null)
            {
                autofillButton.gameObject.SetActive(visible);
            }
        }

        private void AppendText(string str)
        {
            if (activeTargetInput == null)
            {
                Debug.LogWarning("[TableCalculatorKeypad] Cannot type: activeTargetInput is NULL!");
                return;
            }

            if (activeTargetInput.text.Length >= maxCharacterLimit) return;
            if (str == "." && activeTargetInput.text.Contains(".")) return;

            activeTargetInput.text += str;
            activeTargetInput.caretPosition = activeTargetInput.text.Length;
            Debug.Log($"[TableCalculatorKeypad] Typed '{str}' -> Result: '{activeTargetInput.text}' on '{activeTargetInput.name}'");
        }

        private void OnBackspace()
        {
            if (activeTargetInput == null) return;

            if (activeTargetInput.text.Length > 0)
            {
                activeTargetInput.text = activeTargetInput.text.Substring(0, activeTargetInput.text.Length - 1);
                activeTargetInput.caretPosition = activeTargetInput.text.Length;
            }
        }

        private void OnClear()
        {
            if (activeTargetInput == null) return;
            activeTargetInput.text = string.Empty;
        }

        public void OnSubmit()
        {
            Debug.Log("[TableCalculatorKeypad] Check / Submit button CLICKED!");
            onSubmitted?.Invoke();
        }

        private void OnAutofillPressed()
        {
            Debug.Log("[TableCalculatorKeypad] Autofill button CLICKED!");
            onAutofillClicked?.Invoke();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (keypadRect != null)
            {
                keypadRect.SetAsLastSibling();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            ApplyDragDelta(eventData.delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerHeld = false;
        }

        private void Update()
        {
            Vector2 currentPointerPos = Vector2.zero;
            bool isDown = false;
            bool isHeld = false;
            bool isUp = false;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                currentPointerPos = Mouse.current.position.ReadValue();
                isDown = Mouse.current.leftButton.wasPressedThisFrame;
                isHeld = Mouse.current.leftButton.isPressed;
                isUp = Mouse.current.leftButton.wasReleasedThisFrame;
            }
            else if (Touchscreen.current != null)
            {
                currentPointerPos = Touchscreen.current.primaryTouch.position.ReadValue();
                isDown = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                isHeld = Touchscreen.current.primaryTouch.press.isPressed;
                isUp = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
#else
            currentPointerPos = Input.mousePosition;
            isDown = Input.GetMouseButtonDown(0);
            isHeld = Input.GetMouseButton(0);
            isUp = Input.GetMouseButtonUp(0);
#endif

            RectTransform targetDragArea = dragHandleBar != null ? dragHandleBar : keypadRect;

            if (isDown && targetDragArea != null)
            {
                if (RectTransformUtility.RectangleContainsScreenPoint(targetDragArea, currentPointerPos, uiCamera))
                {
                    isPointerHeld = true;
                    previousPointerPos = currentPointerPos;
                    if (keypadRect != null) keypadRect.SetAsLastSibling();
                }
            }
            else if (isHeld && isPointerHeld)
            {
                Vector2 delta = currentPointerPos - previousPointerPos;
                previousPointerPos = currentPointerPos;

                if (delta.sqrMagnitude > 0.001f)
                {
                    ApplyDragDelta(delta);
                }
            }
            else if (isUp)
            {
                isPointerHeld = false;
            }
        }

        private void ApplyDragDelta(Vector2 screenDelta)
        {
            if (keypadRect == null) return;

            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
            float scale = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            if (scale <= 0) scale = 1f;

            Vector2 newPos = keypadRect.anchoredPosition + (screenDelta / scale);
            keypadRect.anchoredPosition = ClampWithinBounds(newPos);
        }

        private Vector2 ClampWithinBounds(Vector2 pos)
        {
            RectTransform container = boundaryContainer != null ? boundaryContainer : (rootCanvas != null ? rootCanvas.GetComponent<RectTransform>() : null);
            if (container == null || keypadRect == null) return pos;

            Rect containerRect = container.rect;
            Rect keypadBounds = keypadRect.rect;

            float minX = containerRect.xMin + (keypadRect.pivot.x * keypadBounds.width) + boundaryPadding;
            float maxX = containerRect.xMax - ((1f - keypadRect.pivot.x) * keypadBounds.width) - boundaryPadding;

            float minY = containerRect.yMin + (keypadRect.pivot.y * keypadBounds.height) + boundaryPadding;
            float maxY = containerRect.yMax - ((1f - keypadRect.pivot.y) * keypadBounds.height) - boundaryPadding;

            if (minX > maxX) { float temp = minX; minX = maxX; maxX = temp; }
            if (minY > maxY) { float temp = minY; minY = maxY; maxY = temp; }

            float clampedX = Mathf.Clamp(pos.x, minX, maxX);
            float clampedY = Mathf.Clamp(pos.y, minY, maxY);

            return new Vector2(clampedX, clampedY);
        }
    }
}
