using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class EquipmentLootPopup : MonoBehaviour
{
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

    public static void Show(EquipmentItem item1, EquipmentItem item2, Sprite icon1, Sprite icon2, Action onChoice1, Action onChoice2)
    {
        GameObject prefab = Resources.Load<GameObject>("UI/EquipmentLootPopup");
        if (prefab == null)
        {
            Debug.LogError("EquipmentLootPopup prefab not found in Resources/UI/");
            return;
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
            return;
        }

        GameObject instance = Instantiate(prefab, targetCanvas.transform);
        EquipmentLootPopup popup = instance.GetComponent<EquipmentLootPopup>();

        popup.Setup(item1, item2, icon1, icon2, onChoice1, onChoice2);
    }

    private void Setup(EquipmentItem item1, EquipmentItem item2, Sprite icon1, Sprite icon2, Action onChoice1, Action onChoice2)
    {
        if (headerTitle != null) headerTitle.text = "CHEST LOOTED";

        if (item1 != null)
        {
            if (item1Title != null) item1Title.text = item1.name;
            if (item1Stats != null) item1Stats.text = GetStatString(item1);
            if (item1Icon != null) item1Icon.sprite = icon1;
            if (item1Button != null)
            {
                item1Button.onClick.RemoveAllListeners();
                item1Button.onClick.AddListener(() => {
                    Debug.Log($"[EquipmentLootPopup] Item 1 Button Clicked: {item1.name}");
                    onChoice1?.Invoke();
                    Destroy(gameObject);
                });
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
                item2Button.onClick.AddListener(() => {
                    Debug.Log($"[EquipmentLootPopup] Item 2 Button Clicked: {item2.name}");
                    onChoice2?.Invoke();
                    Destroy(gameObject);
                });
                }
                }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => Destroy(gameObject));
        }
        
        // Ensure scale is correct and centered
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
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
