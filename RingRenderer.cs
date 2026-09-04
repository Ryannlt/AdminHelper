using System.Collections.Generic;
using UnityEngine;

namespace AdminEye
{
    // Pool of flat ground rings, one per flagged player, drawn with LineRenderers.
    internal sealed class RingRenderer
    {
        private const int Segments = 48;
        private const float Radius = 1.6f;
        private const float GroundOffset = 0.05f;

        private readonly List<LineRenderer> _pool = new List<LineRenderer>();
        private readonly Vector3[] _points = new Vector3[Segments + 1];

        private GameObject _root;
        private Shader _shader;

        public void Draw(List<ScoredPlayer> flagged)
        {
            EnsureRoot();

            for (int i = 0; i < flagged.Count; i++)
            {
                LineRenderer ring = Resolve(i);
                ring.gameObject.SetActive(true);

                Color colour = ColourFor(flagged[i].Isolation);
                ring.startColor = ring.endColor = colour;

                // URP Unlit ignores vertex colour, so the material carries the colour and each ring owns one.
                if (ring.sharedMaterial != null) ring.sharedMaterial.color = colour;

                BuildCircle(flagged[i].Position);
                ring.SetPositions(_points);
            }

            for (int i = flagged.Count; i < _pool.Count; i++)
            {
                if (_pool[i] != null) _pool[i].gameObject.SetActive(false);
            }
        }

        public void HideAll()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_pool[i] != null) _pool[i].gameObject.SetActive(false);
            }
        }

        public void Destroy()
        {
            _pool.Clear();
            if (_root != null) Object.Destroy(_root);
            _root = null;
        }

        private void BuildCircle(Vector3 centre)
        {
            float y = centre.y + GroundOffset;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                _points[i] = new Vector3(centre.x + Mathf.Cos(angle) * Radius, y, centre.z + Mathf.Sin(angle) * Radius);
            }
        }

        // Yellow where the marker first appears, red once the player is flagged.
        private static Color ColourFor(int isolation)
        {
            float low = Settings.RingThreshold.Value;
            float high = Mathf.Max(low + 1f, Settings.RamboThreshold.Value);
            float t = Mathf.Clamp01((isolation - low) / (high - low));
            return Color.Lerp(new Color(1f, 0.82f, 0.15f), new Color(1f, 0.25f, 0.1f), t);
        }

        private void EnsureRoot()
        {
            if (_root != null) return;

            _root = new GameObject("AdminEyeRings");
            Object.DontDestroyOnLoad(_root);
            _pool.Clear();
        }

        private LineRenderer Resolve(int index)
        {
            while (_pool.Count <= index) _pool.Add(Create());

            LineRenderer ring = _pool[index];
            if (ring == null)
            {
                ring = Create();
                _pool[index] = ring;
            }

            return ring;
        }

        private LineRenderer Create()
        {
            GameObject host = new GameObject("Ring");
            host.transform.SetParent(_root.transform, false);

            LineRenderer ring = host.AddComponent<LineRenderer>();
            ring.useWorldSpace = true;
            ring.loop = false;
            ring.positionCount = Segments + 1;
            ring.startWidth = ring.endWidth = 0.14f;
            ring.numCapVertices = 0;
            ring.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ring.receiveShadows = false;
            ring.sharedMaterial = new Material(ResolveShader());
            return ring;
        }

        // The game runs URP, so its unlit shader is the one guaranteed to be in the build.
        private Shader ResolveShader()
        {
            if (_shader != null) return _shader;

            _shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (_shader == null) _shader = Shader.Find("Sprites/Default");
            if (_shader == null) _shader = Shader.Find("Hidden/Internal-Colored");

            return _shader;
        }
    }
}
