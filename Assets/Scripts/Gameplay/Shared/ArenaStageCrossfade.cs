using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Corebound.Gameplay.Shared
{
    // Owner decision 2026-09-29 (Open Topics 3.2, option 3): a Stage -> Stage change is diegetic.
    // The old arena stays on screen and fades away over the new one while the Core holds its
    // place, so it reads as "the facility moved around me" instead of a hard cut.
    //
    // The outgoing arena is the previous ArenaVisualsRoot, renamed so nothing finds it, with every
    // collider off so its walls cannot block the player for the second it is still visible. One
    // SortingGroup lifts the whole of it above the new floor, props and gates but below the Core
    // and everything that moves, so the fade reveals the new stage underneath rather than the
    // other way round. Its sprites and textures would normally be destroyed at rebuild; they are
    // handed over here and destroyed when the fade ends.
    public sealed class ArenaStageCrossfade : MonoBehaviour
    {
        // Above the new arena's backdrop, ground, boundary, props, gates and slot markers (-30 to
        // -5); below the Core (-2) and all gameplay objects.
        private const int OutgoingSortingOrder = -3;
        private const int CoreGhostSortingOrder = -1;

        private readonly List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
        private readonly List<float> _baseAlpha = new List<float>();
        private List<Object> _deferredCleanup;
        private float _duration;
        private float _age;
        private Transform _core;
        private Vector3 _coreFrom;
        private Vector3 _coreTo;
        private SpriteRenderer _coreGhost;
        private float _coreGhostAlpha;

        public static void Begin(GameObject outgoingRoot, float seconds, List<Object> deferredCleanup, Transform core, Vector3 coreFrom, SpriteRenderer oldCore)
        {
            var fade = outgoingRoot.AddComponent<ArenaStageCrossfade>();
            fade.Initialize(seconds, deferredCleanup, core, coreFrom, oldCore);
        }

        // Called before the new arena is built, so nothing in the old one can be found or collided
        // with while the new one is created.
        public static void Detach(GameObject outgoingRoot)
        {
            outgoingRoot.name = "ArenaVisualsRoot_Outgoing";

            foreach (var collider in outgoingRoot.GetComponentsInChildren<Collider2D>(true))
            {
                collider.enabled = false;
            }

            // Not `??`: in the Editor a missing component is a fake-null object that `??` treats as
            // present, which threw here and aborted the whole stage change.
            var group = outgoingRoot.GetComponent<SortingGroup>();

            if (group == null)
            {
                group = outgoingRoot.AddComponent<SortingGroup>();
            }

            group.sortingOrder = OutgoingSortingOrder;
        }

        private void Initialize(float seconds, List<Object> deferredCleanup, Transform core, Vector3 coreFrom, SpriteRenderer oldCore)
        {
            _duration = Mathf.Max(0.1f, seconds);
            _deferredCleanup = deferredCleanup;
            GetComponentsInChildren(true, _renderers);

            for (var index = 0; index < _renderers.Count; index++)
            {
                _baseAlpha.Add(_renderers[index].color.a);
            }

            _core = core;
            _coreFrom = coreFrom;
            _coreTo = core != null ? core.position : coreFrom;

            if (core != null)
            {
                core.position = coreFrom;
            }

            // The Core's own sprite has already switched to the new stage's. A copy of the old
            // one sits just above it and fades with the floor, so the Core changes with the room.
            if (oldCore != null && oldCore.sprite != null)
            {
                var ghostObject = new GameObject("CoreCrossfadeGhost");
                ghostObject.transform.SetPositionAndRotation(coreFrom, oldCore.transform.rotation);
                ghostObject.transform.localScale = oldCore.transform.lossyScale;
                _coreGhost = ghostObject.AddComponent<SpriteRenderer>();
                _coreGhost.sprite = oldCore.sprite;
                _coreGhost.color = oldCore.color;
                _coreGhost.sortingOrder = CoreGhostSortingOrder;
                _coreGhostAlpha = oldCore.color.a;
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / _duration);
            var eased = t * t * (3f - 2f * t);
            var remaining = 1f - eased;

            for (var index = 0; index < _renderers.Count; index++)
            {
                var renderer = _renderers[index];

                if (renderer == null)
                {
                    continue;
                }

                var color = renderer.color;
                color.a = _baseAlpha[index] * remaining;
                renderer.color = color;
            }

            if (_core != null)
            {
                _core.position = Vector3.Lerp(_coreFrom, _coreTo, eased);
            }

            if (_coreGhost != null)
            {
                _coreGhost.transform.position = _core != null ? _core.position : _coreFrom;
                var color = _coreGhost.color;
                color.a = _coreGhostAlpha * remaining;
                _coreGhost.color = color;
            }

            if (t >= 1f)
            {
                Finish();
            }
        }

        private void OnDestroy()
        {
            // A scene unload mid-fade must still release the old stage's textures.
            ReleaseDeferred();

            if (_coreGhost != null)
            {
                Destroy(_coreGhost.gameObject);
            }
        }

        private void Finish()
        {
            if (_core != null)
            {
                _core.position = _coreTo;
            }

            enabled = false;
            Destroy(gameObject);
        }

        private void ReleaseDeferred()
        {
            if (_deferredCleanup == null)
            {
                return;
            }

            for (var index = 0; index < _deferredCleanup.Count; index++)
            {
                if (_deferredCleanup[index] != null)
                {
                    Destroy(_deferredCleanup[index]);
                }
            }

            _deferredCleanup = null;
        }
    }
}
