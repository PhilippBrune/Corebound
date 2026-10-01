using System;
using System.Collections.Generic;
using UnityEngine;

namespace Corebound.Gameplay.Shared
{
    // Open Topics 3.3, step 3 of 4 (2026-10-01): the spawn gates, cut out of StageArenaArtPresenter.
    // The presenter still decides *where* gates go and builds their sprites with its own helpers
    // (it owns the arena root, sprite tracking and cleanup); this owns what a gate *does* - idle
    // dimming, the glow behind it, and the pulse that lights the gates the next wave will use.
    //
    // UI proposal 3 (2026-09-21): one gate per spawn point, so the art says where enemies really
    // come from, and the pulse says it before they do.
    public sealed class SpawnGateVisuals
    {
        private const float PulseBeatsPerSecond = 1.5f;
        private const float PulseScale = 0.12f;
        private const float IdleBrightness = 0.72f;
        private static readonly Color PulseColor = new Color(1f, 0.55f, 0.25f, 1f);

        private readonly List<Gate> _gates = new List<Gate>();
        private Sprite _glowSprite;

        public int Count => _gates.Count;

        public void Clear()
        {
            _gates.Clear();
        }

        // Called by the presenter once per gate it has built. trackSprite registers a runtime
        // sprite with the presenter's cleanup, which outlives this object's rebuilds.
        public void Add(Transform gate, Transform parent, string glowName, Vector2 position, Vector2 gateSize, Color tint, Func<Sprite, Sprite> trackSprite)
        {
            if (gate == null)
            {
                return;
            }

            var glow = CreateGlow(parent, glowName, position, gateSize, trackSprite);
            var entry = new Gate(gate, gate.GetComponent<SpriteRenderer>(), tint, gate.localScale, glow);

            // Idle gates are slightly dimmed so a lit gate reads as lit, not merely as the same gate.
            var idle = tint;
            idle.r *= IdleBrightness;
            idle.g *= IdleBrightness;
            idle.b *= IdleBrightness;
            entry.IdleColor = idle;
            entry.Reset();
            _gates.Add(entry);
        }

        // Lights the gates the coming wave will use, for the length of the wave-start spawn delay,
        // so the tell always comes before the enemies, never with them.
        public void Pulse(IReadOnlyList<int> gateIndices, float durationSeconds)
        {
            for (var index = 0; index < _gates.Count; index++)
            {
                _gates[index].PulseRemaining = 0f;
                _gates[index].Reset();
            }

            if (gateIndices == null)
            {
                return;
            }

            var duration = Mathf.Max(0.1f, durationSeconds);

            for (var index = 0; index < gateIndices.Count; index++)
            {
                var gateIndex = gateIndices[index];

                if (gateIndex >= 0 && gateIndex < _gates.Count)
                {
                    _gates[gateIndex].PulseRemaining = duration;
                    _gates[gateIndex].PulseDuration = duration;
                }
            }
        }

        public void Tick(float deltaTime)
        {
            for (var index = 0; index < _gates.Count; index++)
            {
                var gate = _gates[index];

                if (gate.Transform == null || gate.Renderer == null || gate.PulseRemaining <= 0f)
                {
                    continue;
                }

                gate.PulseRemaining = Mathf.Max(0f, gate.PulseRemaining - deltaTime);

                if (gate.PulseRemaining <= 0f)
                {
                    gate.Reset();
                    continue;
                }

                // A few beats over the delay, strongest at the start and fading into the idle state
                // by the time the first enemy appears.
                var elapsed = gate.PulseDuration - gate.PulseRemaining;
                var envelope = gate.PulseRemaining / gate.PulseDuration;
                var beat = 0.5f + 0.5f * Mathf.Sin(elapsed * Mathf.PI * 2f * PulseBeatsPerSecond);
                var strength = Mathf.Lerp(0.35f, 1f, beat) * envelope;
                gate.Renderer.color = Color.Lerp(gate.IdleColor, Color.white, strength * 0.6f);
                gate.Transform.localScale = gate.BaseScale * (1f + PulseScale * strength);

                if (gate.Glow != null)
                {
                    gate.Glow.color = new Color(PulseColor.r, PulseColor.g, PulseColor.b, 0.9f * strength);
                }
            }
        }

        // Playtest check 2026-09-22: tinting the gate itself toward orange barely shows, because the
        // gate art is dark and a multiply tint on dark pixels stays dark. A soft light pool behind
        // the gate is what actually reads as "this door is about to open".
        private SpriteRenderer CreateGlow(Transform parent, string childName, Vector2 position, Vector2 gateSize, Func<Sprite, Sprite> trackSprite)
        {
            if (_glowSprite == null)
            {
                _glowSprite = trackSprite(CreateRadialGlowSprite(64));
            }

            var child = parent.Find(childName);

            if (child == null)
            {
                child = new GameObject(childName).transform;
                child.SetParent(parent, false);
            }

            child.localPosition = position;
            child.localRotation = Quaternion.identity;
            var renderer = child.gameObject.GetOrAdd<SpriteRenderer>();
            renderer.sprite = _glowSprite;
            renderer.sortingOrder = -8;
            renderer.color = new Color(PulseColor.r, PulseColor.g, PulseColor.b, 0f);
            var diameter = Mathf.Max(gateSize.x, gateSize.y) * 1.5f;
            var bounds = _glowSprite.bounds.size;
            child.localScale = new Vector3(diameter / Mathf.Max(0.01f, bounds.x), diameter / Mathf.Max(0.01f, bounds.y), 1f);
            return renderer;
        }

        private static Sprite CreateRadialGlowSprite(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            var center = (size - 1) * 0.5f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center)) / center;
                    var alpha = Mathf.Clamp01(1f - distance);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * alpha));
                }
            }

            texture.Apply();
            texture.hideFlags = HideFlags.DontSave;
            return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private sealed class Gate
        {
            public Gate(Transform transform, SpriteRenderer renderer, Color baseColor, Vector3 baseScale, SpriteRenderer glow)
            {
                Transform = transform;
                Renderer = renderer;
                Glow = glow;
                IdleColor = baseColor;
                BaseScale = baseScale;
            }

            public Transform Transform { get; }

            public SpriteRenderer Renderer { get; }

            public SpriteRenderer Glow { get; }

            public Color IdleColor { get; set; }

            public Vector3 BaseScale { get; }

            public float PulseRemaining { get; set; }

            public float PulseDuration { get; set; } = 1f;

            public void Reset()
            {
                if (Renderer != null)
                {
                    Renderer.color = IdleColor;
                }

                if (Transform != null)
                {
                    Transform.localScale = BaseScale;
                }

                if (Glow != null)
                {
                    var color = Glow.color;
                    color.a = 0f;
                    Glow.color = color;
                }
            }
        }
    }
}
