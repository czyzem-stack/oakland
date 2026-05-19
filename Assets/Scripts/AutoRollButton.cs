using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class AutoRollButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public DiceRollSystem diceSystem;
    public TMP_Text buttonText;
    public float longPressThreshold = 0.8f;
    
    [Header("Visuals")]
    public Color normalColor = Color.white;
    public Color autoColor = new Color(1f, 0.8f, 0.2f); // Golden/Yellow for Auto

    private bool isPointerDown = false;
    private float pointerDownTime = 0f;
    private bool longPressTriggered = false;
    private Image buttonImage;

    private void Start()
    {
        if (diceSystem == null) diceSystem = Object.FindAnyObjectByType<DiceRollSystem>();
        buttonImage = GetComponent<Image>();
        UpdateVisuals();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (GenericPopup.IsOpen) return;
        isPointerDown = true;
        pointerDownTime = Time.time;
        longPressTriggered = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (GenericPopup.IsOpen)
        {
            isPointerDown = false;
            return;
        }

        if (isPointerDown && !longPressTriggered)
        {
            // Single Click
            if (diceSystem != null) diceSystem.Roll();
        }
        isPointerDown = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerDown = false;
    }

    private void Update()
    {
        if (GenericPopup.IsOpen) return;

        if (isPointerDown && !longPressTriggered)
        {
            if (Time.time - pointerDownTime >= longPressThreshold)
            {
                longPressTriggered = true;
                ToggleAutoRoll();
            }
        }
    }

    private void ToggleAutoRoll()
    {
        if (diceSystem != null)
        {
            diceSystem.autoRoll = !diceSystem.autoRoll;
            UpdateVisuals();
            Debug.Log("[AutoRollButton] Auto-Roll toggled: " + diceSystem.autoRoll);
        }
    }

    private void UpdateVisuals()
    {
        if (diceSystem == null) return;

        if (buttonText != null)
        {
            buttonText.text = diceSystem.autoRoll ? "AUTO ROLL" : "ROLL";
        }

        if (buttonImage != null)
        {
            buttonImage.color = diceSystem.autoRoll ? autoColor : normalColor;
        }
    }
}
