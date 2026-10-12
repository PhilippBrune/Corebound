using System.Collections.Generic;
using Corebound.Audio;
using Corebound.Gameplay.Bosses;
using Corebound.Gameplay.Shared;
using Corebound.Gameplay.Waves;
using Corebound.MetaProgression;
using UnityEngine;
using UnityEngine.UI;

namespace Corebound.Story
{
    // The in-run half of STORY_2026-09-30 (S1-2, S2-1, S4-1). The Core speaks one line at a time,
    // and only at moments the run already gives the player room: arriving in a stage (the
    // five-second spawn delay), a boss arriving (its two-second hold), a boss falling, the run
    // ending. Never during a wave. At the first full clear the ending card is shown once.
    //
    // Attached by WaveManager like GameplayQuestTracker, so no scene edit is needed. Its two
    // panels live in the authored OverlayCanvas scene; this only finds them and sets text and
    // alpha, never layout.
    public sealed class StoryDirector : MonoBehaviour
    {
        private const float FadeInSeconds = 0.35f;
        private const float FadeOutSeconds = 0.6f;
        private const float MinHoldSeconds = 3.2f;
        private const float HoldSecondsPerCharacter = 0.045f;
        private const float MaxHoldSeconds = 7f;
        private const float EndingFadeSeconds = 1.2f;

        private WaveManager _waveManager;
        private StoryData _story;
        private readonly Queue<string> _voiceQueue = new Queue<string>();

        private CanvasGroup _voiceGroup;
        private Text _voiceText;
        private CanvasGroup _endingGroup;
        private Text _endingTitle;
        private Text _endingText;

        private float _voiceTimer = -1f;
        private float _voiceHold;
        private float _endingTimer = -1f;
        private int _announcedStage;
        private WaveState _lastState = (WaveState)(-1);
        private int _hurtStage = -1;
        private bool _saidHurtFirst;
        private bool _saidHurtSecond;

#if UNITY_EDITOR
        // Autopilot "core" probe.
        public static int DebugHurtLines;
#endif

        public void Bind(WaveManager waveManager)
        {
            _waveManager = waveManager;
            var definition = waveManager != null ? waveManager.MetaProgressionDefinition : null;
            _story = definition != null ? definition.Story : null;
            TryBindOverlay();
        }

        private void OnEnable()
        {
            AdditiveCanvasSceneLoader.CanvasSceneReady += OnCanvasSceneReady;
            BossEvents.OnBossSpawned += OnBossSpawned;
            BossEvents.OnBossDefeated += OnBossDefeated;
            Corebound.Gameplay.Combat.CoreController.CoreHit += OnCoreHit;
        }

        private void OnDisable()
        {
            Corebound.Gameplay.Relics.RelicRevealPresenter.Suppressed = false;
            AdditiveCanvasSceneLoader.CanvasSceneReady -= OnCanvasSceneReady;
            BossEvents.OnBossSpawned -= OnBossSpawned;
            BossEvents.OnBossDefeated -= OnBossDefeated;
            Corebound.Gameplay.Combat.CoreController.CoreHit -= OnCoreHit;
        }

        // CORE_PROTECTION_FEEL B6 (owner decision P2-1): the one exception to "never during a wave".
        // The story made the Core a character; when it is hurt it says so - once below half and
        // once below a quarter, each at most once per stage, so a long fight does not become a
        // monologue. A line spoken while the Core is still falling is the point, so it does not wait.
        private void OnCoreHit(Corebound.Gameplay.Combat.CoreHitInfo hit)
        {
            if (IsSilent)
            {
                return;
            }

            var stage = _waveManager.StageIndex;

            if (stage != _hurtStage)
            {
                _hurtStage = stage;
                _saidHurtFirst = false;
                _saidHurtSecond = false;
            }

            var tuning = Corebound.Gameplay.Combat.CoreController.Tuning;

            if (!_saidHurtSecond && hit.HealthNormalized <= tuning.HurtLineSecondThreshold)
            {
                // Falling past both at once speaks the worse line only.
                _saidHurtSecond = true;
                _saidHurtFirst = true;
                SayHurt(_story.CoreHurtSecond);
            }
            else if (!_saidHurtFirst && hit.HealthNormalized <= tuning.HurtLineFirstThreshold)
            {
                _saidHurtFirst = true;
                SayHurt(_story.CoreHurtFirst);
            }
        }

        private void SayHurt(StoryLine line)
        {
            if (line == null || line.IsEmpty)
            {
                return;
            }

            Say(line);
#if UNITY_EDITOR
            DebugHurtLines++;
#endif
        }

        private void OnCanvasSceneReady(string sceneName)
        {
            if (sceneName == AdditiveCanvasSceneLoader.OverlayCanvasSceneName)
            {
                TryBindOverlay();
            }
        }

        private void TryBindOverlay()
        {
            if (_voiceGroup == null)
            {
                var voiceRoot = AdditiveCanvasSceneLoader.FindRoot(AdditiveCanvasSceneLoader.OverlayCanvasSceneName, "CoreVoiceCanvas");
                var panel = voiceRoot != null ? voiceRoot.transform.Find("CoreVoicePanel") : null;
                _voiceGroup = panel != null ? panel.GetComponent<CanvasGroup>() : null;
                var text = panel != null ? panel.Find("CoreVoiceText") : null;
                _voiceText = text != null ? text.GetComponent<Text>() : null;

                if (_voiceGroup != null)
                {
                    _voiceGroup.alpha = 0f;
                }
            }

            if (_endingGroup == null)
            {
                var endingRoot = AdditiveCanvasSceneLoader.FindRoot(AdditiveCanvasSceneLoader.OverlayCanvasSceneName, "EndingCanvas");
                var panel = endingRoot != null ? endingRoot.transform.Find("EndingPanel") : null;
                _endingGroup = panel != null ? panel.GetComponent<CanvasGroup>() : null;
                var title = panel != null ? panel.Find("EndingTitle") : null;
                var text = panel != null ? panel.Find("EndingText") : null;
                _endingTitle = title != null ? title.GetComponent<Text>() : null;
                _endingText = text != null ? text.GetComponent<Text>() : null;

                if (_endingGroup != null)
                {
                    _endingGroup.alpha = 0f;
                }
            }
        }

        private bool IsSilent => _story == null || _waveManager == null || _waveManager.IsTutorialRun;

        private void Update()
        {
            if (IsSilent)
            {
                return;
            }

            WatchStage();
            WatchRunEnd();
            TickVoice();
            TickEnding();
        }

        // Arrival: at the start of every stage, whether the session began there or the Works
        // just rearranged itself around the Core. Watching the index covers both without caring
        // which path changed it.
        private void WatchStage()
        {
            var stage = _waveManager.StageIndex;
            var state = _waveManager.State;

            if (stage == _announcedStage || (state != WaveState.Combat && state != WaveState.Intermission))
            {
                return;
            }

            _announcedStage = stage;
            Say(_story.FindStage(stage)?.Arrival);
        }

        private void WatchRunEnd()
        {
            var state = _waveManager.State;

            if (state == _lastState)
            {
                return;
            }

            _lastState = state;

            if (state == WaveState.Failed)
            {
                var lines = _story.RunLostLines;

                if (lines != null && lines.Length > 0)
                {
                    Say(lines[Random.Range(0, lines.Length)]);
                }
            }
            else if (state == WaveState.Completed)
            {
                OnRunCompleted();
            }
        }

        // State Completed is only ever observed on the final stage: an earlier stage's completion
        // advances to the next stage inside the same call.
        private void OnRunCompleted()
        {
            if (_story.VictorySting != null)
            {
                AudioManager.PlayCueStatic(_story.VictorySting);
            }

            var saveManager = _waveManager.SaveManagerSource;
            var definition = _waveManager.MetaProgressionDefinition;
            var save = saveManager != null && definition != null ? saveManager.LoadMetaProgressionSaveData(definition.CreateDefaultSaveData()) : null;

            if (save == null || save.HasSeenEnding)
            {
                Say(_story.RunWonLine);
                return;
            }

            save.MarkEndingSeen();
            saveManager.SaveMetaProgressionSaveData(save);
            ShowEnding();
        }

        private void OnBossSpawned(BossEventArgs eventArgs)
        {
            if (!IsSilent)
            {
                Say(_story.FindStage(_waveManager.StageIndex)?.BossArrival);
            }
        }

        private void OnBossDefeated(BossEventArgs eventArgs)
        {
            if (!IsSilent)
            {
                Say(_story.FindStage(_waveManager.StageIndex)?.BossDefeat);
            }
        }

        private void Say(StoryLine line)
        {
            if (line == null || line.IsEmpty)
            {
                return;
            }

            // At most one line waits. A run moves on faster than a line is read - a boss can fall
            // before the line about its arrival has faded - and an old line spoken late is worse
            // than one never spoken, so the newest replaces whatever was still waiting.
            _voiceQueue.Clear();
            _voiceQueue.Enqueue(line.Text);
        }

        // Unscaled: the boss's defeat line is spoken while the reward screen may have paused time.
        private void TickVoice()
        {
            if (_voiceGroup == null || _voiceText == null)
            {
                return;
            }

            if (_voiceTimer < 0f)
            {
                if (_voiceQueue.Count == 0)
                {
                    return;
                }

                var text = _voiceQueue.Dequeue();
                _voiceText.text = "“" + text + "”";
                _voiceHold = Mathf.Clamp(MinHoldSeconds + text.Length * HoldSecondsPerCharacter, MinHoldSeconds, MaxHoldSeconds);
                _voiceTimer = 0f;
            }

            _voiceTimer += Time.unscaledDeltaTime;
            var fadeOutStart = FadeInSeconds + _voiceHold;

            if (_voiceTimer < FadeInSeconds)
            {
                _voiceGroup.alpha = _voiceTimer / FadeInSeconds;
            }
            else if (_voiceTimer < fadeOutStart)
            {
                _voiceGroup.alpha = 1f;
            }
            else if (_voiceTimer < fadeOutStart + FadeOutSeconds)
            {
                _voiceGroup.alpha = 1f - (_voiceTimer - fadeOutStart) / FadeOutSeconds;
            }
            else
            {
                _voiceGroup.alpha = 0f;
                _voiceTimer = -1f;
            }
        }

        private void ShowEnding()
        {
            if (_endingGroup == null)
            {
                return;
            }

            // The ending has the screen to itself: no Core line under it, and the final boss's
            // relic reveal - held back until "Boss Defeated" faded - does not land on top of it.
            _voiceQueue.Clear();
            _voiceTimer = -1f;

            if (_voiceGroup != null)
            {
                _voiceGroup.alpha = 0f;
            }

            Corebound.Gameplay.Relics.RelicRevealPresenter.Suppressed = true;

            if (_endingTitle != null)
            {
                _endingTitle.text = _story.EndingTitle != null ? _story.EndingTitle.Text : string.Empty;
            }

            if (_endingText != null)
            {
                var builder = new System.Text.StringBuilder();

                foreach (var line in _story.Ending)
                {
                    if (line != null && !line.IsEmpty)
                    {
                        builder.AppendLine(line.Text);
                    }
                }

                _endingText.text = builder.ToString().TrimEnd();
            }

            _endingTimer = 0f;
        }

        private void TickEnding()
        {
            if (_endingTimer < 0f || _endingGroup == null)
            {
                return;
            }

            _endingTimer += Time.unscaledDeltaTime;
            _endingGroup.alpha = Mathf.Clamp01(_endingTimer / EndingFadeSeconds);
        }
    }
}
