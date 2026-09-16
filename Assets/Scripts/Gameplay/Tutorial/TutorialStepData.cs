using UnityEngine;

namespace Corebound.Gameplay.Tutorial
{
    [CreateAssetMenu(menuName = "Corebound/Gameplay/Tutorial/Tutorial Step Data", fileName = "TutorialStepData")]
    public sealed class TutorialStepData : ScriptableObject
    {
        [SerializeField] private TutorialStepType _stepType;
        [SerializeField] [TextArea(2, 3)] private string _promptText = "Do something.";
        [SerializeField] private bool _isBlocking = true;
        [SerializeField] private string _beatTitle = string.Empty;
        [SerializeField] [TextArea(2, 4)] private string _beatBody = string.Empty;

        public TutorialStepType StepType => _stepType;

        public string PromptText => _promptText;

        // Playtest feedback (2026-09-16): "the Tutorial feels like a normal playthrough" - a
        // first-time player had no scripted moment telling them what was about to happen, only
        // this thin always-on hint text competing with real-time combat pressure. BeatTitle/
        // BeatBody feed a paused, dismiss-on-Confirm explanation panel (see
        // WaveStatePresenter.TryShowTutorialBeat) shown once when a step first becomes current,
        // BEFORE the player is expected to act - PromptText remains the persistent in-combat hint
        // shown afterward, unchanged.
        public string BeatTitle => string.IsNullOrWhiteSpace(_beatTitle) ? _promptText : _beatTitle;

        public string BeatBody => string.IsNullOrWhiteSpace(_beatBody) ? _promptText : _beatBody;

        // When true (the default), this step's completion event is only accepted once it's the
        // sequence's current step - TutorialService ignores it if the player performs the action
        // early, so the scripted move -> shoot -> collect -> build -> card -> defend flow can't be
        // skipped out of order. A non-blocking step can complete whenever its event fires,
        // regardless of sequence position (for an optional/background objective).
        public bool IsBlocking => _isBlocking;
    }
}
