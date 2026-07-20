using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ExiledAlvaston.Data;
using ExiledAlvaston.Vibe;
using System.Collections.Generic;

namespace ExiledAlvaston.UI
{
    /// <summary>
    /// Fullscreen parchment inventory: left stats, center paper doll + tooltip, right backpack.
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        [Header("Overlay Root")]
        public GameObject InventoryUIPanel;

        [Header("Left Panel: Stats & Identity")]
        public Image CharacterPortrait;
        public TextMeshProUGUI LevelText;
        public TextMeshProUGUI CoreTraitsText;
        public TextMeshProUGUI AttackStatsText;
        public TextMeshProUGUI ResistancesText;
        public TextMeshProUGUI CharacterNameText;

        [Header("Center Panel: Paper Doll + Tooltip")]
        public Transform PaperDollContainer;
        public Dictionary<ItemType, Image> EquipmentSlots = new Dictionary<ItemType, Image>();
        public GameObject TooltipPanel;
        public Image TooltipIcon;
        public TextMeshProUGUI TooltipTitle;
        public TextMeshProUGUI TooltipBody;
        public Button UnequipButton;

        [Header("Right Panel: Backpack")]
        public Transform BackpackGridContainer;
        public GameObject InventorySlotPrefab;
        public TextMeshProUGUI CurrencyText;

        private CharacterData _boundCharacter;

        private void Awake()
        {
            if (InventoryUIPanel == null) return;
            Transform back = InventoryUIPanel.transform.Find("BackButton");
            if (back != null)
            {
                Button btn = back.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(ToggleInventory);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.I))
                ToggleInventory();
        }

        public void BindCharacter(CharacterData data)
        {
            _boundCharacter = data;
            if (InventoryUIPanel != null && InventoryUIPanel.activeSelf)
                RefreshUI();
        }

        public void ToggleInventory()
        {
            if (InventoryUIPanel == null) return;

            bool isActive = !InventoryUIPanel.activeSelf;
            InventoryUIPanel.SetActive(isActive);
            if (isActive) ExiledAlvaston.Systems.PauseManager.Push();
            else ExiledAlvaston.Systems.PauseManager.Pop();

            if (isActive)
                RefreshUI();
        }

        private void RefreshUI()
        {
            if (_boundCharacter == null) return;

            if (CharacterNameText != null)
                CharacterNameText.text = _boundCharacter.CharacterName;

            if (CharacterPortrait != null && _boundCharacter.Portrait != null)
                CharacterPortrait.sprite = _boundCharacter.Portrait;

            if (CoreTraitsText != null)
            {
                var t = _boundCharacter.BaseTraits;
                CoreTraitsText.text =
                    $"STR  {t.Strength}\nEND  {t.Endurance}\nAGI  {t.Agility}\n" +
                    $"INT  {t.Intelligence}\nAWA  {t.Awareness}\nPER  {t.Perception}";
            }

            if (ResistancesText != null)
            {
                var r = _boundCharacter.BaseResistances;
                ResistancesText.text =
                    $"Armor {r.Physical}\nFire {r.Fire}  Cold {r.Cold}\n" +
                    $"Poison {r.Poison}  Magic {r.Magic}";
            }
        }

        public void ShowTooltip(ItemData item)
        {
            if (TooltipPanel == null || item == null) return;

            TooltipPanel.SetActive(true);
            if (TooltipIcon != null)
            {
                TooltipIcon.sprite = item.Icon;
                TooltipIcon.enabled = item.Icon != null;
            }
            if (TooltipTitle != null)
                TooltipTitle.text = item.ItemName;
            if (TooltipBody != null)
            {
                string stats = "";
                if (item.Armor > 0) stats += $"+{item.Armor} Armor\n";
                if (item.Damage > 0) stats += $"+{item.Damage} Damage\n";
                TooltipBody.text = $"{item.Description}\n{stats}".Trim();
            }
        }

        public void HideTooltip()
        {
            if (TooltipPanel != null)
                TooltipPanel.SetActive(false);
        }

        public void EquipItem(ItemData item)
        {
            if (item == null || !EquipmentSlots.ContainsKey(item.Type)) return;

            Image slotImage = EquipmentSlots[item.Type];
            if (slotImage != null)
            {
                slotImage.sprite = item.Icon;
                slotImage.enabled = item.Icon != null;
            }
            ShowTooltip(item);
        }

        public void PopulateBackpack(List<ItemData> items)
        {
            if (BackpackGridContainer == null || InventorySlotPrefab == null) return;

            foreach (Transform child in BackpackGridContainer)
                Destroy(child.gameObject);

            int maxSlots = 20; // 4x5 like EK screenshot
            for (int i = 0; i < maxSlots; i++)
            {
                GameObject slot = Instantiate(InventorySlotPrefab, BackpackGridContainer);
                Image slotIcon = slot.GetComponentInChildren<Image>();
                if (slotIcon == null) continue;

                if (items != null && i < items.Count && items[i] != null)
                {
                    slotIcon.sprite = items[i].Icon;
                    slotIcon.enabled = items[i].Icon != null;
                    slotIcon.color = Color.white;
                }
                else
                {
                    slotIcon.sprite = null;
                    slotIcon.color = EKVibe.SlotEmpty;
                }
            }
        }
    }
}
