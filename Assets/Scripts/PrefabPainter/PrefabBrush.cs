using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrefabPainter
{
    [CreateAssetMenu(menuName = "Tools/Prefab Painter/Brush", fileName = "PrefabBrush")]
    public class PrefabBrush : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public GameObject prefab;
            [Min(0f)] public float weight = 1f;
        }

        [Header("Prefabs (weighted)")] public List<Entry> prefabs = new List<Entry>();

        [Header("Placement")] public LayerMask paintMask = ~0; // surfaces we can paint onto
        public float surfaceOffset = 0f; // push away from surface along normal
        public bool alignToNormal = true;
        [Range(0f, 360f)] public float randomYaw = 360f;

        [Header("Scale (uniform)")] public bool randomScale = false;
        public Vector2 uniformScaleRange = new Vector2(1f, 1f);

        [Header("Stroke")] [Min(0f)] public float spacing = 0.5f; // min distance between placements during drag

        [Header("Erase")] [Min(0.01f)] public float eraseRadius = 0.75f;

        public bool IsValid => prefabs != null && prefabs.Count > 0 &&
                               prefabs.Exists(e => e.prefab != null && e.weight > 0f);

        public GameObject PickPrefab(System.Random rng)
        {
            if (prefabs == null || prefabs.Count == 0) return null;

            float total = 0f;
            for (int i = 0; i < prefabs.Count; i++)
            {
                var e = prefabs[i];
                if (e.prefab == null || e.weight <= 0f) continue;
                total += e.weight;
            }

            if (total <= 0f) return null;

            // deterministic from rng
            double r = rng.NextDouble() * total;
            float acc = 0f;

            for (int i = 0; i < prefabs.Count; i++)
            {
                var e = prefabs[i];
                if (e.prefab == null || e.weight <= 0f) continue;
                acc += e.weight;
                if (r <= acc) return e.prefab;
            }

            // fallback
            for (int i = prefabs.Count - 1; i >= 0; i--)
                if (prefabs[i].prefab != null && prefabs[i].weight > 0f)
                    return prefabs[i].prefab;

            return null;
        }
    }
}