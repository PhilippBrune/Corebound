using System.Collections.Generic;
using Corebound.Gameplay.Cards;

namespace Corebound.UI.MainMenu
{
    public readonly struct RecordsCardEntry
    {
        public RecordsCardEntry(CardData card, bool collected)
        {
            Card = card;
            Collected = collected;
        }

        public CardData Card { get; }

        public bool Collected { get; }
    }

    // What the route panel needs to draw the Records screen's two tabs. Separate from
    // IMainMenuRouteTargetController so the other panels do not grow a Records vocabulary.
    public interface IRecordsViewSource
    {
        MainMenuRoute? ActivePanelRoute { get; }

        // 0 = runs, 1 = cards, 2 = Core Logs, 3 = credits (STORY_2026-09-30).
        int RecordsTab { get; }

        int RecordsLogCount { get; }

        int RecordsLogUnlockedCount { get; }

        // The selected Core Log's text, shown under the list; empty unless the Logs tab shows.
        string RecordsSelectedLogText { get; }

        IReadOnlyList<RecordsCardEntry> RecordsCards { get; }

        int RecordsCollectedCount { get; }

        // -1 unless the Cards tab is showing.
        int RecordsSelectedCard { get; }

        bool TrySelectRecordsTab(int tab);

        bool TrySelectRecordsCard(int index);
    }
}
