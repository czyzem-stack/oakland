using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

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

    [Header("Item 3 UI (Dynamic)")]
    public Image item3Icon;
    public TMP_Text item3Title;
    public TMP_Text item3Stats;
    public Button item3Button;
    private GameObject item3Root;

    [Header("General UI")]
    public TMP_Text headerTitle;
    public Button closeButton;

    private EquipmentItem item1Data;
    private EquipmentItem item2Data;
    private EquipmentItem item3Data;
    private Action on1;
    private Action on2;
    private Action on3;
    private Action onComplete;

    private EquipmentItem selectedItem;
    private Action selectedAction;
    private int selectedIndex = -1;

    private int maxPicks = 1;
    private int currentPicks = 0;
    private List<int> pickedIndices = new List<int>();

    public static void Show(EquipmentItem item1, EquipmentItem item2, Sprite icon1, Sprite icon2, Action onChoice1, Action onChoice2, 
                          EquipmentItem item3 = null, Sprite icon3 = null, Action onChoice3 = null, int picks = 1, Action onComplete = null)
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
        popup.Setup(item1, item2, icon1, icon2, onChoice1, onChoice2, item3, icon3, onChoice3, picks, onComplete);
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
        
        onComplete?.Invoke();
    }

    public void Setup(EquipmentItem item1, EquipmentItem item2, Sprite icon1, Sprite icon2, Action onChoice1, Action onChoice2, 
                       EquipmentItem item3, Sprite icon3, Action onChoice3, int picks, Action onCompleteAction)
    {
        item1Data = item1;
        item2Data = item2;
        item3Data = item3;
        on1 = onChoice1;
        on2 = onChoice2;
        on3 = onChoice3;
        onComplete = onCompleteAction;
        maxPicks = picks;
        currentPicks = 0;
        pickedIndices.Clear();

        if (headerTitle != null) headerTitle.text = maxPicks > 1 ? $"CHEST LOOTED (PICK {maxPicks})" : "CHEST LOOTED";

        if (item1 != null)
        {
            if (item1Title != null) item1Title.text = item1.name;
            if (item1Stats != null) item1Stats.text = GetStatString(item1);
            if (item1Icon != null) item1Icon.sprite = icon1;
            item1Button.onClick.RemoveAllListeners();
            item1Button.onClick.AddListener(() => SelectItem(1));
            SetButtonText(item1Button, "SELECT");
        }

        if (item2 != null)
        {
            if (item2Title != null) item2Title.text = item2.name;
            if (item2Stats != null) item2Stats.text = GetStatString(item2);
            if (item2Icon != null) item2Icon.sprite = icon2;
            item2Button.onClick.RemoveAllListeners();
            item2Button.onClick.AddListener(() => SelectItem(2));
            SetButtonText(item2Button, "SELECT");
        }

        if (item3 != null)
        {
            EnsureItem3UI();
            if (item3Title != null) item3Title.text = item3.name;
            if (item3Stats != null) item3Stats.text = GetStatString(item3);
            if (item3Icon != null) item3Icon.sprite = icon3;
            if (item3Button != null)
            {
                item3Button.onClick.RemoveAllListeners();
                item3Button.onClick.AddListener(() => SelectItem(3));
                SetButtonText(item3Button, "SELECT");
            }
            if (item3Root != null) item3Root.SetActive(true);
        }
        else if (item3Root != null)
        {
            item3Root.SetActive(false);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(ClosePopup);
            closeButton.gameObject.SetActive(maxPicks == 1);
        }
        
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
        }
    }

    private void EnsureItem3UI()
    {
        if (item3Button != null && item3Root != null) return;

        Transform splitContent = transform.Find("Container/SplitContent");
        if (splitContent == null) return;

        Transform item2Transform = splitContent.Find("Item2");
        if (item2Transform == null) return;

        GameObject item3Go = Instantiate(item2Transform.gameObject, splitContent);
        item3Go.name = "Item3";
        item3Root = item3Go;

        item3Icon = item3Go.transform.Find("Icon")?.GetComponent<Image>();
        item3Title = item3Go.transform.Find("Title")?.GetComponent<TMP_Text>();
        item3Stats = item3Go.transform.Find("Stats")?.GetComponent<TMP_Text>();
        item3Button = item3Go.transform.Find("EquipButton")?.GetComponent<Button>();

        HorizontalLayoutGroup hlg = splitContent.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null)
        {
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
        }
    }

    private void SetButtonText(Button btn, string text)
    {
        TMP_Text t = btn.GetComponentInChildren<TMP_Text>();
        if (t != null) t.text = text;
    }

    private void SelectItem(int index)
    {
        if (pickedIndices.Contains(index)) return;

        EquipmentItem targetItem = (index == 1) ? item1Data : (index == 2 ? item2Data : item3Data);
        Action targetAction = (index == 1) ? on1 : (index == 2 ? on2 : on3);

        if (EquipmentManager.Instance != null) EquipmentManager.Instance.ClearPreview();

        if (selectedItem == targetItem)
        {
            ConfirmEquip(index, targetAction);
            return;
        }

        selectedItem = targetItem;
        selectedAction = targetAction;
        selectedIndex = index;

        if (EquipmentManager.Instance != null) EquipmentManager.Instance.Preview(selectedItem);

        RefreshButtonStates(index);
        
        if (headerTitle != null) 
        {
            string picksPart = maxPicks > 1 ? $" (PICK {currentPicks + 1}/{maxPicks})" : "";
            headerTitle.text = $"SELECTING: {selectedItem.name.ToUpper()}{picksPart}";
        }
    }

    private void RefreshButtonStates(int currentSelection)
    {
        UpdateButton(1, item1Button, currentSelection == 1);
        UpdateButton(2, item2Button, currentSelection == 2);
        if (item3Button != null) UpdateButton(3, item3Button, currentSelection == 3);
    }

    private void UpdateButton(int index, Button btn, bool isSelected)
    {
        if (btn == null) return;
        bool isPicked = pickedIndices.Contains(index);
        btn.image.color = isPicked ? Color.gray : (isSelected ? new Color(0.5f, 1f, 0.5f) : Color.white);
        SetButtonText(btn, isPicked ? "PICKED" : (isSelected ? "EQUIP" : "SELECT"));
        btn.interactable = !isPicked;
    }

    private void ConfirmEquip(int index, Action action)
    {
        action?.Invoke();
        pickedIndices.Add(index);
        currentPicks++;
        selectedItem = null;
        selectedAction = null;
        selectedIndex = -1;

        if (currentPicks >= maxPicks)
        {
            ClosePopup();
        }
        else
        {
            RefreshButtonStates(-1);
            if (headerTitle != null) headerTitle.text = $"CHEST LOOTED (PICK {currentPicks + 1}/{maxPicks})";
        }
    }

    private void ClosePopup()
    {
        if (EquipmentManager.Instance != null) EquipmentManager.Instance.ClearPreview();
        Destroy(gameObject);
    }

    private void Update()
    {
        var pointer = UnityEngine.InputSystem.Pointer.current;
        if (selectedItem != null && pointer != null && pointer.press.wasPressedThisFrame)
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 screenPos = pointer.position.ReadValue();
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.GetComponentInParent<EquipmentManager>() != null)
                {
                    ConfirmEquip(selectedIndex, selectedAction);
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
        if (item.slot == EquipmentSlot.Gloves || item.slot == EquipmentSlot.Boots)
            s += "<size=80%><color=#AAAAAA>(Stat Boost Only)</color></size>\n";
        return s.Trim();
    }
}
