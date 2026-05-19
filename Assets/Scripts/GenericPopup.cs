using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class GenericPopup : MonoBehaviour
{
    private static int openCount;
    public static bool IsOpen => openCount > 0;

    public static void ResetForSceneLoad()
    {
        openCount = 0;
        Time.timeScale = 1f;
    }

    [Header("UI Elements")]
    public TMP_Text titleText;
    public TMP_Text messageText;
    public Button closeButton;
    public Button confirmButton;
    public Button cancelButton;
    public Button thirdButton;
    public TMP_Text confirmButtonText;
    public TMP_Text cancelButtonText;
    public TMP_Text thirdButtonText;

    private Action onConfirm;
    private Action onCancel;
    private Action onThird;

    private bool countedAsOpen;

    private void OnEnable()
    {
        if (countedAsOpen) return;
        countedAsOpen = true;
        openCount++;
        Time.timeScale = 0f;
    }

    private void OnDestroy()
    {
        if (!countedAsOpen) return;
        countedAsOpen = false;
        openCount = Mathf.Max(0, openCount - 1);
        if (openCount == 0 && !EquipmentLootPopup.IsOpen) Time.timeScale = 1f;
    }

    private void Awake()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (confirmButton != null) confirmButton.onClick.AddListener(OnConfirmClick);
        if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelClick);
        if (thirdButton != null) thirdButton.onClick.AddListener(OnThirdClick);
    }

    public void Setup(string title, string message, string confirmLabel = "OK", string cancelLabel = null, string thirdLabel = null, Action confirmAction = null, Action cancelAction = null, Action thirdAction = null)
    {
        if (titleText != null) titleText.text = title;
        
        if (messageText != null) 
        {
            messageText.text = message;
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(message));
        }
        
        if (confirmButtonText != null) confirmButtonText.text = confirmLabel;
        if (cancelButtonText != null) cancelButtonText.text = cancelLabel;
        if (thirdButtonText != null) thirdButtonText.text = thirdLabel;

        onConfirm = confirmAction;
        onCancel = cancelAction;
        onThird = thirdAction;

        if (confirmButton != null) confirmButton.gameObject.SetActive(confirmLabel != null);
        if (cancelButton != null) cancelButton.gameObject.SetActive(cancelLabel != null);
        if (thirdButton != null) thirdButton.gameObject.SetActive(thirdLabel != null);
    }

    [Header("Structured Layout")]
    public Transform statsContainer;
    public GameObject statRowPrefab; // Optional

    public void AddStat(string label, string value)
    {
        if (statsContainer != null)
        {
            // Ensure the ScrollView is active
            ScrollRect sr = statsContainer.GetComponentInParent<ScrollRect>(true);
            if (sr != null) sr.gameObject.SetActive(true);
        }

        // Use structured container if available
        if (statsContainer != null && statRowPrefab != null)
        {
            GameObject row = Instantiate(statRowPrefab, statsContainer);
            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>();
            if (texts.Length >= 2)
            {
                texts[0].text = label;
                texts[1].text = value;
            }
            return;
        }

        // Fallback: Create a dedicated row with two text objects for proper column alignment
        if (statsContainer != null)
        {
            GameObject rowObj = new GameObject("Stat_" + label, typeof(RectTransform));
            rowObj.transform.SetParent(statsContainer, false);
            
            // Add LayoutElement to row to ensure it takes space in VerticalLayoutGroup
            float fontSize = messageText != null ? messageText.fontSize : 22;
            rowObj.AddComponent<LayoutElement>().preferredHeight = fontSize * 1.5f;

            // Label (Left)
            GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObj.transform.SetParent(rowObj.transform, false);
            TextMeshProUGUI l = labelObj.GetComponent<TextMeshProUGUI>();
            SetupStatText(l, label, TextAlignmentOptions.Left, fontSize);
            l.rectTransform.anchorMin = Vector2.zero;
            l.rectTransform.anchorMax = new Vector2(0.6f, 1f); // Give label 60% width
            l.rectTransform.offsetMin = Vector2.zero;
            l.rectTransform.offsetMax = Vector2.zero;

            // Value (Right)
            GameObject valueObj = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            valueObj.transform.SetParent(rowObj.transform, false);
            TextMeshProUGUI v = valueObj.GetComponent<TextMeshProUGUI>();
            SetupStatText(v, value, TextAlignmentOptions.Right, fontSize);
            v.rectTransform.anchorMin = new Vector2(0.6f, 0f);
            v.rectTransform.anchorMax = Vector2.one; // Give value remaining 40% width
            v.rectTransform.offsetMin = Vector2.zero;
            v.rectTransform.offsetMax = Vector2.zero;
        }
        else if (messageText != null)
        {
            // Absolute fallback to the old way if no container exists
            messageText.richText = true;
            if (string.IsNullOrEmpty(messageText.text)) messageText.text = "";
            else messageText.text += "\n";
            messageText.text += $"{label}<pos=90%>{value}";
        }
        }

        private void SetupStatText(TextMeshProUGUI t, string text, TextAlignmentOptions align, float fontSize)
        {
        t.font = messageText != null ? messageText.font : null;
        t.fontSize = fontSize;
        t.color = messageText != null ? messageText.color : new Color(0.9f, 0.9f, 0.9f);
        t.richText = true;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.alignment = align;
        t.text = text;
        }

    public void ClearStats()
    {
        if (statsContainer != null)
        {
            foreach (Transform child in statsContainer) Destroy(child.gameObject);

            // Hide ScrollView initially, AddStat will show it
            ScrollRect sr = statsContainer.GetComponentInParent<ScrollRect>(true);
            if (sr != null) sr.gameObject.SetActive(false);
        }
        if (messageText != null) messageText.text = "";
    }

    private void OnConfirmClick()
    {
        onConfirm?.Invoke();
        Close();
    }

    private void OnCancelClick()
    {
        onCancel?.Invoke();
        Close();
    }

    private void OnThirdClick()
    {
        onThird?.Invoke();
        Close();
    }

    public void Close()
    {
        Destroy(gameObject);
    }

    public static GenericPopup Show(string title, string message, string confirmLabel = "OK", string cancelLabel = null, string thirdLabel = null, Action confirmAction = null, Action cancelAction = null, Action thirdAction = null)
    {
        GameObject prefab = Resources.Load<GameObject>("UI/GenericPopup");

        if (prefab == null)
        {
            Debug.LogError("GenericPopup prefab not found in Resources/UI/GenericPopup");
            return null;
        }

        // Find the best canvas (ScreenSpaceOverlay preferred)
        Canvas[] canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude);
        Canvas targetCanvas = null;
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetCanvas = c;
                break;
            }
        }

        if (targetCanvas == null && canvases.Length > 0) targetCanvas = canvases[0];

        if (targetCanvas == null)
        {
            Debug.LogError("No Canvas found in scene!");
            return null;
        }

        GameObject instance = UnityEngine.Object.Instantiate(prefab, targetCanvas.transform);
        Debug.Log($"[GenericPopup] Instantiated on Canvas: {targetCanvas.name}");
        
        GenericPopup popup = instance.GetComponent<GenericPopup>();
        popup.Setup(title, message, confirmLabel, cancelLabel, thirdLabel, confirmAction, cancelAction, thirdAction);
        
        // Ensure it's centered and has correct scale
        RectTransform rt = instance.GetComponent<RectTransform>();
        rt.anchoredPosition = Vector2.zero;
        rt.localPosition = Vector3.zero;
        rt.localScale = Vector3.one;

        return popup;
    }
}
