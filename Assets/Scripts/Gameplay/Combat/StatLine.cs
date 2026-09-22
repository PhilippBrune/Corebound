using UnityEngine;

namespace Corebound.Gameplay.Combat
{
    // One row of the pause menu's stat tabs (playtest 2026-09-22: "a tab with overall stats which
    // shows the basic stat, in brackets the plus of the perks - one tab for the player, one for
    // towers"). Built by the systems that own the numbers, so the pause menu only prints them.
    public readonly struct StatLine
    {
        private StatLine(string label, string text, bool isHeader)
        {
            Label = label;
            Text = text;
            IsHeader = isHeader;
        }

        public string Label { get; }

        public string Text { get; }

        public bool IsHeader { get; }

        public static StatLine Header(string label)
        {
            return new StatLine(label, string.Empty, true);
        }

        // "base (+bonus)": the base value, and what perks, skills, upgrades, tiers and set bonuses
        // add on top. lowerIsBetter flips the sign convention for intervals and reload times, so a
        // faster weapon reads as a gain.
        public static StatLine Value(string label, float baseValue, float totalValue, string format, string unit = "", bool lowerIsBetter = false)
        {
            var text = baseValue.ToString(format) + unit;
            var delta = totalValue - baseValue;

            if (Mathf.Abs(delta) > 0.0005f)
            {
                var percent = Mathf.Abs(baseValue) > 0.0001f ? delta / baseValue * 100f : 0f;
                var isGain = lowerIsBetter ? delta < 0f : delta > 0f;
                var color = isGain ? "#8CE38C" : "#FF9A8A";
                text += "  <color=" + color + ">(" + (delta > 0f ? "+" : string.Empty) + delta.ToString(format) + unit
                    + (Mathf.Abs(baseValue) > 0.0001f ? ", " + (percent > 0f ? "+" : string.Empty) + percent.ToString("0") + "%" : string.Empty) + ")</color>";
            }

            return new StatLine(label, text, false);
        }

        public static StatLine Plain(string label, string text)
        {
            return new StatLine(label, text, false);
        }
    }
}
