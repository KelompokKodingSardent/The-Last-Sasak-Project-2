using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using TheLastSasak.Items.Definitions;
using TheLastSasak.Items.Modifiers;
using TheLastSasak.Player.Stats;

namespace TheLastSasak.UI.Equipment
{
    public class StatComparisonUI : MonoBehaviour
    {
        [Serializable]
        public struct StatRowView
        {
            public StatType statType;
            public string displayLabel;
            public bool isPercentage;
            public GameObject rowRoot;
            public TMP_Text labelText;
            public TMP_Text currentValueText;
            public TMP_Text newValueText;
            public TMP_Text deltaText;
        }

        [Header("Domain Dependency")]
        [SerializeField] private PlayerStatController playerStatController;

        [Header("Stat Rows")]
        [SerializeField] private List<StatRowView> statRows = new List<StatRowView>();

        [Header("Delta Colors")]
        [SerializeField] private Color positiveColor = new Color(0.35f, 0.88f, 0.45f, 1f);
        [SerializeField] private Color negativeColor = new Color(0.92f, 0.36f, 0.32f, 1f);
        [SerializeField] private Color neutralColor = new Color(0.75f, 0.75f, 0.75f, 1f);

        private EquipmentDefinitionSO _candidateItem;

        private void Awake()
        {
            if (playerStatController == null)
            {
                playerStatController = FindFirstObjectByType<PlayerStatController>();
            }
        }

        private void OnEnable()
        {
            if (playerStatController != null)
            {
                playerStatController.OnStatsRecalculated += RefreshDisplay;
            }

            RefreshDisplay();
        }

        private void OnDisable()
        {
            if (playerStatController != null)
            {
                playerStatController.OnStatsRecalculated -= RefreshDisplay;
            }
        }

        public void ShowComparison(EquipmentDefinitionSO candidate)
        {
            _candidateItem = candidate;
            RefreshDisplay();
        }

        public void RefreshDisplay()
        {
            if (playerStatController == null)
            {
                return;
            }

            Dictionary<StatType, float> previewStats = _candidateItem != null
                ? playerStatController.PreviewStatsWithCandidate(_candidateItem)
                : null;

            for (int i = 0; i < statRows.Count; i++)
            {
                StatRowView row = statRows[i];
                float currentVal = playerStatController.GetStat(row.statType);
                float nextVal = previewStats != null && previewStats.TryGetValue(row.statType, out float pVal)
                    ? pVal
                    : currentVal;

                float delta = nextVal - currentVal;
                bool hasChange = Mathf.Abs(delta) > 0.001f;

                if (row.labelText != null && !string.IsNullOrEmpty(row.displayLabel))
                {
                    row.labelText.text = row.displayLabel;
                }

                if (row.currentValueText != null)
                {
                    row.currentValueText.text = FormatStatValue(currentVal, row.isPercentage);
                }

                if (row.newValueText != null)
                {
                    row.newValueText.text = previewStats != null
                        ? FormatStatValue(nextVal, row.isPercentage)
                        : "-";
                }

                if (row.deltaText != null)
                {
                    if (!hasChange || previewStats == null)
                    {
                        row.deltaText.text = "-";
                        row.deltaText.color = neutralColor;
                    }
                    else
                    {
                        string sign = delta > 0f ? "+" : "";
                        row.deltaText.text = $"{sign}{FormatStatValue(delta, row.isPercentage)}";
                        row.deltaText.color = delta > 0f ? positiveColor : negativeColor;
                    }
                }
            }
        }

        private static string FormatStatValue(float value, bool isPercentage)
        {
            if (isPercentage)
            {
                return $"{value * 100f:0.#}%";
            }

            return Mathf.Approximately(value % 1f, 0f)
                ? $"{value:0}"
                : $"{value:0.##}";
        }
    }
}