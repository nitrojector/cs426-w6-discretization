#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PrefabPainter
{
    public class PrefabPainterWindow : EditorWindow
    {
        private enum Mode
        {
            Paint,
            Erase
        }

        [SerializeField] private PrefabPainter.PrefabBrush brush;
        [SerializeField] private Transform parentOverride;
        [SerializeField] private Mode mode = Mode.Paint;

        [SerializeField] private bool placeOnMouseDown = true; // click places one
        [SerializeField] private bool dragToPaint = true; // drag places multiple

        [SerializeField] private int randomSeed = 12345;
        [SerializeField] [Min(0f)] private float placeCooldownSeconds = 0.2f; // 0 = unlimited

        private System.Random rng;


        private Vector3 lastPlacedPos;
        private bool hasLastPlaced;
        private double lastPlaceTime = double.NegativeInfinity;

        private static readonly List<GameObject> eraseBuffer = new();

        [MenuItem("Tools/Prefab Painter")]
        public static void Open()
        {
            var w = GetWindow<PrefabPainterWindow>("Prefab Painter");
            w.Show();
        }

        private void OnEnable()
        {
            rng = new System.Random(randomSeed);
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            brush = (PrefabPainter.PrefabBrush)EditorGUILayout.ObjectField("Brush Asset", brush,
                typeof(PrefabPainter.PrefabBrush), false);

            using (new EditorGUI.DisabledScope(brush == null))
            {
                parentOverride =
                    (Transform)EditorGUILayout.ObjectField("Parent (optional)", parentOverride, typeof(Transform),
                        true);

                mode = (Mode)EditorGUILayout.EnumPopup("Mode", mode);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Stroke", EditorStyles.boldLabel);
                placeOnMouseDown = EditorGUILayout.Toggle("Click to place", placeOnMouseDown);
                dragToPaint = EditorGUILayout.Toggle("Drag to paint", dragToPaint);

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Rate Limit", EditorStyles.boldLabel);
                placeCooldownSeconds = EditorGUILayout.FloatField("Place Cooldown (s)", placeCooldownSeconds);
                if (placeCooldownSeconds < 0f) placeCooldownSeconds = 0f;

                EditorGUILayout.Space(6);
                EditorGUILayout.LabelField("Random", EditorStyles.boldLabel);
                int newSeed = EditorGUILayout.IntField("Seed", randomSeed);
                if (newSeed != randomSeed)
                {
                    randomSeed = newSeed;
                    rng = new System.Random(randomSeed);
                }

                if (GUILayout.Button("Reset Stroke Spacing"))
                {
                    hasLastPlaced = false;
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "Controls in Scene View:\n" +
                "• Hold SHIFT to paint/erase.\n" +
                "• LMB click places one (if enabled).\n" +
                "• SHIFT + drag paints continuously (if enabled).\n" +
                "• Use Erase mode to delete placed instances near the cursor.\n\n" +
                "Tip: Turn off the Move/Rotate/Scale gizmo hotkeys interference by ensuring the Scene view has focus.",
                MessageType.Info
            );
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (brush == null) return;

            Event e = Event.current;
            if (e == null) return;

            // Only act when SHIFT is held (prevents accidental painting while navigating)
            bool shift = (e.modifiers & EventModifiers.Shift) != 0;
            if (!shift)
            {
                // still draw preview when window focused? optional; we only draw when shift to reduce noise
                return;
            }

            // Avoid selecting stuff while painting
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            // Raycast from mouse into scene
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, brush.paintMask))
            {
                SceneView.RepaintAll();
                return;
            }

            // Draw preview gizmos
            DrawPreview(hit);

            bool lmbDown = e.type == EventType.MouseDown && e.button == 0;
            bool lmbDrag = e.type == EventType.MouseDrag && e.button == 0;
            bool lmbUp = e.type == EventType.MouseUp && e.button == 0;

            if (lmbUp)
            {
                // end stroke
                hasLastPlaced = false;
            }

            if (mode == Mode.Paint)
            {
                if (placeOnMouseDown && lmbDown)
                {
                    TryPlace(hit, force: true);
                    e.Use();
                }
                else if (dragToPaint && lmbDrag)
                {
                    TryPlace(hit, force: false);
                    e.Use();
                }
            }
            else // Erase
            {
                if (lmbDown || lmbDrag)
                {
                    TryErase(hit);
                    e.Use();
                }
            }
        }

        private void DrawPreview(RaycastHit hit)
        {
            // Disc at hit point
            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

            float r = (mode == Mode.Erase) ? brush.eraseRadius : Mathf.Max(0.05f, brush.spacing * 0.5f);
            Handles.DrawWireDisc(hit.point, hit.normal, r);

            // Normal line
            Handles.DrawLine(hit.point, hit.point + hit.normal * (r * 1.5f));
        }

        private void TryPlace(RaycastHit hit, bool force)
        {
            double now = EditorApplication.timeSinceStartup;
            if (placeCooldownSeconds > 0f && !force)
            {
                if (now - lastPlaceTime < placeCooldownSeconds)
                    return;
            }

            if (!brush.IsValid) return;

            Vector3 pos = hit.point + hit.normal * brush.surfaceOffset;

            if (!force && hasLastPlaced)
            {
                float dist = Vector3.Distance(lastPlacedPos, pos);
                if (dist < brush.spacing) return;
            }

            GameObject prefab = brush.PickPrefab(rng);
            if (prefab == null) return;

            Quaternion rot = Quaternion.identity;
            if (brush.alignToNormal)
            {
                // up -> normal
                rot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            }

            // random yaw around the normal (or up if not aligning)
            float yaw = (float)(rng.NextDouble() * brush.randomYaw);
            Vector3 yawAxis = brush.alignToNormal ? hit.normal : Vector3.up;
            rot = Quaternion.AngleAxis(yaw, yawAxis) * rot;

            float scale = 1f;
            if (brush.randomScale)
            {
                float min = Mathf.Min(brush.uniformScaleRange.x, brush.uniformScaleRange.y);
                float max = Mathf.Max(brush.uniformScaleRange.x, brush.uniformScaleRange.y);
                scale = Mathf.Lerp(min, max, (float)rng.NextDouble());
            }

            // Instantiate with Undo
            // Instantiate with Undo
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Paint Prefab");

            if (parentOverride != null)
                Undo.SetTransformParent(instance.transform, parentOverride, "Parent Painted Prefab");

            // Apply rotation + scale first (bounds depend on these)
            instance.transform.rotation = rot;

            // scale: keep your existing multiply behavior
            instance.transform.localScale = instance.transform.localScale * scale;

            // Now compute the proper offset so the object sits on top of the surface
            Bounds wb = CalculateWorldBounds(instance);
            float pushOut = ExtentAlongNormal(wb, hit.normal);

            // Base point on surface
            Vector3 basePoint = hit.point + hit.normal * brush.surfaceOffset;

            // Final position = surface point + “half-size along normal”
            instance.transform.position = basePoint + hit.normal * (pushOut + 0.0005f); // tiny epsilon

            if (parentOverride != null)
            {
                Undo.SetTransformParent(instance.transform, parentOverride, "Parent Painted Prefab");
            }

            EditorUtility.SetDirty(instance);

            lastPlacedPos = pos;
            hasLastPlaced = true;
            lastPlaceTime = EditorApplication.timeSinceStartup;
        }

        private void TryErase(RaycastHit hit)
        {
            float radius = Mathf.Max(0.01f, brush.eraseRadius);

            // Find nearby colliders
            Collider[] cols = Physics.OverlapSphere(hit.point, radius, ~0, QueryTriggerInteraction.Ignore);
            if (cols == null || cols.Length == 0) return;

            eraseBuffer.Clear();
            for (int i = 0; i < cols.Length; i++)
            {
                var go = cols[i].gameObject;

                // If user set a parentOverride, prefer erasing only children of it
                if (parentOverride != null)
                {
                    if (!go.transform.IsChildOf(parentOverride)) continue;
                }

                // Avoid erasing terrain/scene geometry: only erase prefab instances
                // (This heuristic checks if it came from a prefab asset)
                var source = PrefabUtility.GetCorrespondingObjectFromSource(go);
                if (source == null) continue;

                eraseBuffer.Add(go);
            }

            // Delete closest first (feels better)
            eraseBuffer.Sort((a, b) =>
                Vector3.SqrMagnitude(a.transform.position - hit.point)
                    .CompareTo(Vector3.SqrMagnitude(b.transform.position - hit.point)));

            // Delete one per call (less destructive). Change to loop if you want “spray erase”.
            if (eraseBuffer.Count > 0)
            {
                Undo.DestroyObjectImmediate(eraseBuffer[0]);
            }
        }

        private static Bounds CalculateWorldBounds(GameObject go)
        {
            // Prefer renderers; fallback to colliders if no renderers
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers != null && renderers.Length > 0)
            {
                Bounds b = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    b.Encapsulate(renderers[i].bounds);
                return b;
            }

            var colliders = go.GetComponentsInChildren<Collider>();
            if (colliders != null && colliders.Length > 0)
            {
                Bounds b = colliders[0].bounds;
                for (int i = 1; i < colliders.Length; i++)
                    b.Encapsulate(colliders[i].bounds);
                return b;
            }

            // Degenerate fallback
            return new Bounds(go.transform.position, Vector3.zero);
        }

        // For an axis-aligned world Bounds, the support distance in direction n is:
        // |nx|*ex + |ny|*ey + |nz|*ez
        private static float ExtentAlongNormal(Bounds worldBounds, Vector3 normal)
        {
            Vector3 n = normal.normalized;
            Vector3 e = worldBounds.extents;
            return Mathf.Abs(n.x) * e.x + Mathf.Abs(n.y) * e.y + Mathf.Abs(n.z) * e.z;
        }
    }
}
#endif