using System.Collections.Generic;
using UnityEngine;

namespace AdminHelper
{
    internal sealed class RingRenderer
    {
        private const int Segments = 48;
        private const float Radius = 1.6f;
        private const float FlagRadius = 2.4f;
        private const float BeamHeight = 6f;
        private const float GroundOffset = 0.05f;

        private static readonly Color FlagColour = new Color(0.3f, 0.95f, 1f);

        private readonly List<LineRenderer> _pool = new List<LineRenderer>();
        private readonly Vector3[] _points = new Vector3[Segments + 1];

        private GameObject _root;
        private Shader _shader;

        public void Draw(List<ScoredPlayer> watched, List<FlagMark> flags)
        {
            EnsureRoot();

            int used = 0;

            for (int i = 0; i < watched.Count; i++)
            {
                LineRenderer ring = Resolve(used++);
                Apply(ring, ColourFor(watched[i].Isolation));

                BuildCircle(watched[i].Position, Radius);
                ring.positionCount = Segments + 1;
                ring.SetPositions(_points);
            }

            for (int i = 0; i < flags.Count; i++)
            {
                LineRenderer ring = Resolve(used++);
                Apply(ring, FlagColour);

                BuildCircle(flags[i].Position, FlagRadius);
                ring.positionCount = Segments + 1;
                ring.SetPositions(_points);

                LineRenderer beam = Resolve(used++);
                Apply(beam, FlagColour);

                beam.positionCount = 2;
                beam.SetPosition(0, flags[i].Position + Vector3.up * GroundOffset);
                beam.SetPosition(1, flags[i].Position + Vector3.up * BeamHeight);
            }

            for (int i = used; i < _pool.Count; i++)
            {
                if (_pool[i] != null) _pool[i].gameObject.SetActive(false);
            }
        }

        private static void Apply(LineRenderer line, Color colour)
        {
            line.gameObject.SetActive(true);
            line.startColor = line.endColor = colour;

            if (line.sharedMaterial != null) line.sharedMaterial.color = colour;
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

        private void BuildCircle(Vector3 centre, float radius)
        {
            float y = centre.y + GroundOffset;
            for (int i = 0; i <= Segments; i++)
            {
                float angle = i * Mathf.PI * 2f / Segments;
                _points[i] = new Vector3(centre.x + Mathf.Cos(angle) * radius, y, centre.z + Mathf.Sin(angle) * radius);
            }
        }

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

            _root = new GameObject("AdminHelperRings");
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
