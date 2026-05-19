using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EquipmentLootPopup : MonoBehaviour
{
    private static int openCount;
    public static bool IsOpen => openCount > 0;

    public static void ResetForSceneLoad()
    {
        openCount = 0;
        Time.timeScale = 1f;
    }

    [Header("Item 1 UI")]
public Image item1Icon;
    public TMP_Text item1Title;
    public TMP_Text item1Stats;
    public Button item1Button;

    [Header("Item 2 UI")]
    public Image item2Icon;
    public TMP_Text item2Title;
    public TMP_Text item2Stats;
    public Button item2Button;

    [Header("General UI")]
    public TMP_Text headerTitle;
    public Button closeButton;

    private EquipmentItem selectedItem;
    private Action selectedAction;
    private EquipmentItem item1Data;
    private EquipmentItem item2Data;
    private Action on1;
    private Action on2;

    public static void Show(EquipmentItem item1, EquipmentItem item2, Sprite icon1, Sprite icon2, Action onChoice1, Action onChoice2)
    {
        GameObject prefab = Resources.Load<GameObject>("UI/EquipmentLootPopup");
        if (prefab == null)
        {
            Debug.LogError("EquipmentLootPopup prefab not found in Resources/UI/");
            return;
        }

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
            return;
        }

        GameObject instance = Instantiate(prefab, targetCanvas.transform);
        EquipmentLootPopup popup = instance.GetComponent<EquipmentLootPopup>();

        popup.Setup(item1, item2, icon1, icon2, onChoice1, onChoice2);
    }

    private bool countedAsOpen = false;

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
        if (openCount == 0 && !GenericPopup.IsOpen) Time.timeScale = 1f;
    }

    private void Setup(EquipmentItem item1, EquipmentItem item2, Sprite icon1, Sprite icon2, Action onChoice1, Action onChoice2)
    {
        item1Data = item1;
        item2Data = item2;
        on1 = onChoice1;
        on2 = onChoice2;

        if (headerTitle != null) headerTitle.text = "CHEST LOOTED";

        if (item1 != null)
        {
            if (item1Title != null) item1Title.text = item1.name;
            if (item1Stats != null) item1Stats.text = GetStatString(item1);
            if (item1Icon != null) item1Icon.sprite = icon1;
            if (item1Button != null)
            {
                item1Button.onClick.RemoveAllListeners();
                item1Button.onClick.AddListener(() => SelectItem(1));
                SetButtonText(item1Button, "SELECT");
            }
        }

        if (item2 != null)
        {
            if (item2Title != null) item2Title.text = item2.name;
            if (item2Stats != null) item2Stats.text = GetStatString(item2);
            if (item2Icon != null) item2Icon.sprite = icon2;
            if (item2Button != null)
            {
                item2Button.onClick.RemoveAllListeners();
                item2Button.onClick.AddListener(() => SelectItem(2));
                SetButtonText(item2Button, "SELECT");
            }
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => {
                if (EquipmentManager.Instance != null) EquipmentManager.Instance.ClearPreview();
                ClosePopup();
            });
        }
        
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private void SetButtonText(Button btn, string text)
    {
        TMP_Text t = btn.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = text;
    }
private void SelectItem(int index)
{
    EquipmentItem targetItem = (index == 1) ? item1Data : item2Data;
    Action targetAction = (index == 1) ? on1 : on2;

    if (EquipmentManager.Instance != null)
    {
        EquipmentManager.Instance.ClearPreview();
    }

    // If clicking the same item again, equip it immediately
    if (selectedItem == targetItem)
    {
        Debug.Log($"[EquipmentLootPopup] Confirmed selection of {targetItem.name}.");
        ConfirmEquip(targetAction);
        return;
    }

    selectedItem = targetItem;
    selectedAction = targetAction;

    if (EquipmentManager.Instance != null)
    {
        EquipmentManager.Instance.Preview(selectedItem);
    }

    // Visual feedback for selection
    if (item1Button != null)
    {
        item1Button.GetComponent<UnityEngine.UI.Image>().color = (index == 1) ? new Color(0.5f, 1f, 0.5f) : Color.white;
        SetButtonText(item1Button, (index == 1) ? "EQUIP" : "SELECT");
    }
    if (item2Button != null)
    {
        item2Button.GetComponent<UnityEngine.UI.Image>().color = (index == 2) ? new Color(0.5f, 1f, 0.5f) : Color.white;
        SetButtonText(item2Button, (index == 2) ? "EQUIP" : "SELECT");
    }
            
    if (headerTitle != null) headerTitle.text = $"SELECTING: {selectedItem.name.ToUpper()}";
            
    Debug.Log($"[EquipmentLootPopup] Selected {selectedItem.name}. Click EQUIP or Steve to confirm!");
}

private void ConfirmEquip(Action action)
{
    Debug.Log("[EquipmentLootPopup] Confirming Equip.");
    action?.Invoke();
    ClosePopup();
}

private void ClosePopup()
{
    Destroy(gameObject);
}

    private void Update()
    {
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (selectedItem != null && pointer != null && pointer.press.wasPressedThisFrame)
        {
            // Don't raycast if clicking UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            // Raycast into world to see if Steve was clicked
            Vector2 screenPos = pointer.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                EquipmentManager em = hit.transform.GetComponentInParent<EquipmentManager>();
                if (em != null)
                {
                    Debug.Log($"[EquipmentLootPopup] Targeted Steve with {selectedItem.name}!");
                    ConfirmEquip(selectedAction);
                }
            }
        }
    }

    private string GetStatString(EquipmentItem item)
    {
        string s = "";
        if (item.brawnBonus != 0) s += $"Brawn +{item.brawnBonus}\n";
        if (item.finesseBonus != 0) s += $"Finesse +{item.finesseBonus}\n";
        if (item.witBonus != 0) s += $"Wit +{item.witBonus}\n";
        if (item.gritBonus != 0) s += $"Grit +{item.gritBonus}\n";
        if (item.attackBonus != 0) s += $"Attack +{item.attackBonus}\n";
        
        // Add note for items that don't have separate meshes
        if (item.slot == EquipmentSlot.Gloves || item.slot == EquipmentSlot.Boots)
        {
            s += "<size=80%><color=#AAAAAA>(Stat Boost Only)</color></size>\n";
        }
        
        return s.Trim();
    }
}
