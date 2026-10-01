using UnityEngine;

namespace Corebound.Gameplay.Tutorial
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Tutorial/Tutorial Sequence Data", fileName = "TutorialSequenceData")]
    public sealed class TutorialSequenceData : ScriptableObject
    {
        [SerializeField] private TutorialStepData[] _steps;

        // The button glyphs the beat panel shows (Asset 28). Referenced here so the sheet is in a
        // build: SpriteSheetData.Find only sees loaded assets.
        [SerializeField] private Corebound.Gameplay.Shared.SpriteSheetData _glyphSheet;

        public TutorialStepData[] Steps => _steps;

        public Corebound.Gameplay.Shared.SpriteSheetData GlyphSheet => _glyphSheet;
    }
}
