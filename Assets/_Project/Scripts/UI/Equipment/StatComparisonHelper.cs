using UnityEngine;

namespace ProjectName.UI
{
    /// <summary>
    /// Compares two equipment items and produces colored TMP rich-text stat deltas.
    /// Used by the character menu's inventory hover compare panel.
    /// </summary>
    public static class StatComparisonHelper
    {
        public struct StatDelta
        {
            public string statName;
            public int currentValue;
            public int newValue;
            public int delta;
        }

        private static readonly Color FadedMoss = new Color(0.357f, 0.549f, 0.353f, 1f);
        private static readonly Color DeepCrimson = new Color(0.545f, 0f, 0f, 1f);
        private static readonly Color BoneWhite = new Color(0.961f, 0.961f, 0.863f, 1f);

        /// <summary>
        /// Compares a candidate item against the currently equipped item in that slot.
        /// Pass null for current if the slot is empty.
        /// </summary>
        public static StatDelta[] Compare(EquipmentData current, EquipmentData candidate)
        {
            int curSTR = current != null ? current.bonusSTR : 0;
            int curINT = current != null ? current.bonusINT : 0;
            int curAGI = current != null ? current.bonusAGI : 0;

            int newSTR = candidate != null ? candidate.bonusSTR : 0;
            int newINT = candidate != null ? candidate.bonusINT : 0;
            int newAGI = candidate != null ? candidate.bonusAGI : 0;

            return new StatDelta[]
            {
                new StatDelta { statName = "STR", currentValue = curSTR, newValue = newSTR, delta = newSTR - curSTR },
                new StatDelta { statName = "INT", currentValue = curINT, newValue = newINT, delta = newINT - curINT },
                new StatDelta { statName = "AGI", currentValue = curAGI, newValue = newAGI, delta = newAGI - curAGI },
            };
        }

        /// <summary>
        /// Returns the appropriate color for a stat delta value.
        /// </summary>
        public static Color GetDeltaColor(int delta)
        {
            if (delta > 0) return FadedMoss;
            if (delta < 0) return DeepCrimson;
            return BoneWhite;
        }

        /// <summary>
        /// Formats a stat delta as TMP rich text with color tags.
        /// Example: "STR +2 > +4  <color=#5B8C5A>(+2)</color>"
        /// </summary>
        public static string FormatDelta(StatDelta d)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(GetDeltaColor(d.delta));
            string deltaStr = d.delta > 0 ? $"+{d.delta}" : d.delta.ToString();

            if (d.delta == 0 && d.currentValue == 0 && d.newValue == 0)
                return null; // Skip stats with no relevance

            string current = d.currentValue >= 0 ? $"+{d.currentValue}" : d.currentValue.ToString();
            string next = d.newValue >= 0 ? $"+{d.newValue}" : d.newValue.ToString();

            return $"{d.statName} {current} > {next}  <color=#{colorHex}>({deltaStr})</color>";
        }

        /// <summary>
        /// Builds a full multi-line comparison string for the compare panel.
        /// Returns empty string if candidate is null.
        /// </summary>
        public static string BuildCompareText(EquipmentData current, EquipmentData candidate)
        {
            if (candidate == null) return "";

            var deltas = Compare(current, candidate);
            var sb = new System.Text.StringBuilder();

            foreach (var d in deltas)
            {
                string line = FormatDelta(d);
                if (line != null)
                {
                    sb.AppendLine(line);
                }
            }

            return sb.ToString().TrimEnd();
        }
    }
}
