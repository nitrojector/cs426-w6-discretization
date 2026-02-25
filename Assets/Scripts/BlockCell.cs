using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BlockCell : MonoBehaviour
{
    [Header("Type")] [SerializeField] private BlockType _type = BlockType.None;
    [SerializeField] private BlockDirection _dir = BlockDirection.Up;


    [Header("Rendering")] [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] [Range(0f, 1f)] private float _spriteScale = 0.75f;

    private bool _isPreview;
    private BlockType _previewType;
    private BlockDirection _previewDir;
    private Coroutine _blinkCoroutine;

    public BlockType Type
    {
        get => _type;
        set
        {
            if (_type == value) return;
            _type = value;
            ApplyVisual();
        }
    }

    public BlockDirection Dir
    {
        get => _dir;
        set
        {
            if (_dir == value) return;
            _dir = value;
            ApplyVisual();
        }
    }

    private void OnValidate()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        ApplyVisual();
    }

    private void Awake()
    {
        ApplyVisual();

        // Only register if BlockManager exists
        var bm = BlockManager.Instance;
        if (bm != null)
            bm.RegisterCell(this);
    }

    private void OnDestroy()
    {
        // Stop any preview coroutine
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        // Only unregister if BlockManager still exists (might be destroyed during shutdown)
        var bm = BlockManager.Instance;
        if (bm != null)
            bm.UnregisterCell(this);
    }

    private void ApplyVisual()
    {
        if (_spriteRenderer == null)
        {
            Debug.LogWarning("BlockCell: No SpriteRenderer found for visual application.");
            return;
        }

        // Reset alpha to full opacity (important for cleaning up preview state)
        var color = _spriteRenderer.color;
        color.a = 1f;
        _spriteRenderer.color = color;

        // If there's no block type, hide the sprite and reset rotation/scale/position
        if (_type == BlockType.None)
        {
            _spriteRenderer.sprite = null;
            _spriteRenderer.enabled = false;
            _spriteRenderer.transform.localRotation = Quaternion.identity;
            _spriteRenderer.transform.localScale = Vector3.one;
            _spriteRenderer.transform.localPosition = Vector3.zero;
            return;
        }

        var bsm = BlockSpriteManager.Instance;
        if (bsm == null) return;

        var sprite = bsm.GetSprite(_type);
        if (sprite != null)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.enabled = true;

            // Determine cube top bounds using a non-sprite Renderer (MeshRenderer, etc.) if available
            Renderer targetRenderer = null;
            var renderers = GetComponentsInChildren<Renderer>();

            // Prefer a MeshRenderer (common for cube visuals). If not found, pick the first renderer that's not the sprite renderer.
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == _spriteRenderer) continue;
                if (r is MeshRenderer)
                {
                    targetRenderer = r;
                    break;
                }

                if (targetRenderer == null)
                    targetRenderer = r;
            }

            // Fallback to using this transform's scale as a cube of size 1 scaled by localScale
            Vector3 topCenterWorld = transform.position;
            float topYWorld = transform.position.y + (transform.localScale.y * 0.5f);
            float topWidth = transform.localScale.x;
            float topDepth = transform.localScale.z;

            if (targetRenderer != null)
            {
                var b = targetRenderer.bounds;
                topCenterWorld = b.center;
                topYWorld = b.max.y;
                topWidth = b.size.x;
                topDepth = b.size.z;
            }

            // Position the sprite just above the top face (small offset to avoid z-fighting)
            var worldPos = new Vector3(topCenterWorld.x, topYWorld + 0.001f, topCenterWorld.z);
            _spriteRenderer.transform.localPosition = transform.InverseTransformPoint(worldPos);

            // Make the sprite lie flat on the top face: rotate 90 degrees around X so its plane matches XZ
            float angleY = _dir switch
            {
                BlockDirection.Up => 0f,
                BlockDirection.Right => 90f,
                BlockDirection.Down => 180f,
                BlockDirection.Left => 270f,
                _ => 0f
            };

            _spriteRenderer.transform.localRotation = Quaternion.Euler(90f, angleY, 0f);

            // Scale the sprite so it fits the top face only (preserve sprite aspect)
            var spriteSize = sprite.bounds.size; // in world units relative to sprite pixelsPerUnit
            if (spriteSize.x > 0f && spriteSize.y > 0f)
            {
                float scaleX = topWidth / spriteSize.x;
                float scaleY = topDepth / spriteSize.y;

                // Apply the computed scale to the sprite transform (sprite uses X and Y axes)
                _spriteRenderer.transform.localScale = new Vector3(scaleX * _spriteScale, scaleY * _spriteScale, 1f);
            }
            else
            {
                _spriteRenderer.transform.localScale = Vector3.one;
            }

            // Ensure the sprite renders on top by bumping its sorting order (2D renderers)
            _spriteRenderer.sortingOrder = 100;
        }
        else
        {
            _spriteRenderer.sprite = null;
            _spriteRenderer.enabled = false;
            _spriteRenderer.transform.localRotation = Quaternion.identity;
            _spriteRenderer.transform.localScale = Vector3.one;
            _spriteRenderer.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Sets a temporary preview of a block type with blinking opacity.
    /// Returns false if the cell is not currently of type None.
    /// </summary>
    public bool SetPreview(BlockType previewType, BlockDirection previewDir)
    {
        // Only allow preview on empty cells
        if (_type != BlockType.None)
            return false;

        _isPreview = true;
        _previewType = previewType;
        _previewDir = previewDir;

        // Stop any existing blink coroutine
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        // Start blinking
        if (Application.isPlaying)
        {
            _blinkCoroutine = StartCoroutine(BlinkPreview());
        }
        else
        {
            // In edit mode, just show the preview without blinking
            ApplyPreviewVisual();
        }

        return true;
    }

    /// <summary>
    /// Stops the preview and returns to the normal state.
    /// </summary>
    public void UnsetPreview()
    {
        if (!_isPreview)
            return;

        _isPreview = false;
        _previewType = BlockType.None;
        _previewDir = BlockDirection.Up;

        // Stop blinking
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        // Restore normal visual
        ApplyVisual();
    }

    private void ApplyPreviewVisual()
    {
        if (_spriteRenderer == null) return;

        var bsm = BlockSpriteManager.Instance;
        if (bsm == null) return;

        var sprite = bsm.GetSprite(_previewType);
        if (sprite != null)
        {
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.enabled = true;

            // Use the same positioning and scaling logic as ApplyVisual
            Renderer targetRenderer = null;
            var renderers = GetComponentsInChildren<Renderer>();

            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == _spriteRenderer) continue;
                if (r is MeshRenderer)
                {
                    targetRenderer = r;
                    break;
                }

                if (targetRenderer == null)
                    targetRenderer = r;
            }

            Vector3 topCenterWorld = transform.position;
            float topYWorld = transform.position.y + (transform.localScale.y * 0.5f);
            float topWidth = transform.localScale.x;
            float topDepth = transform.localScale.z;

            if (targetRenderer != null)
            {
                var b = targetRenderer.bounds;
                topCenterWorld = b.center;
                topYWorld = b.max.y;
                topWidth = b.size.x;
                topDepth = b.size.z;
            }

            var worldPos = new Vector3(topCenterWorld.x, topYWorld + 0.001f, topCenterWorld.z);
            _spriteRenderer.transform.localPosition = transform.InverseTransformPoint(worldPos);

            float angleY = _previewDir switch
            {
                BlockDirection.Up => 0f,
                BlockDirection.Right => 90f,
                BlockDirection.Down => 180f,
                BlockDirection.Left => 270f,
                _ => 0f
            };

            _spriteRenderer.transform.localRotation = Quaternion.Euler(90f, angleY, 0f);

            var spriteSize = sprite.bounds.size;
            if (spriteSize.x > 0f && spriteSize.y > 0f)
            {
                float scaleX = topWidth / spriteSize.x;
                float scaleY = topDepth / spriteSize.y;
                _spriteRenderer.transform.localScale = new Vector3(scaleX * _spriteScale, scaleY * _spriteScale, 1f);
            }
            else
            {
                _spriteRenderer.transform.localScale = Vector3.one;
            }

            _spriteRenderer.sortingOrder = 100;
        }
    }

    private System.Collections.IEnumerator BlinkPreview()
    {
        float blinkSpeed = 2f; // Blinks per second
        float elapsedTime = 0f;

        while (_isPreview)
        {
            ApplyPreviewVisual();

            // Oscillate opacity between 0.3 and 1.0
            float alpha = Mathf.Lerp(0.3f, 1f, (Mathf.Sin(elapsedTime * blinkSpeed * Mathf.PI * 2f) + 1f) * 0.5f);

            if (_spriteRenderer != null)
            {
                var color = _spriteRenderer.color;
                color.a = alpha;
                _spriteRenderer.color = color;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    public bool IsInCellXZ(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        Vector3 half = transform.localScale * 0.5f;

        return Mathf.Abs(localPos.x) <= half.x &&
               Mathf.Abs(localPos.z) <= half.z;
    }

    public float GetDistanceXZ(Vector3 worldPos)
    {
        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        return new Vector2(localPos.x, localPos.z).magnitude;
    }

    public Vector3 GetForceDirectionXZ()
    {
        return _dir switch
        {
            BlockDirection.Up => Vector3.forward, // +Z
            BlockDirection.Down => Vector3.back, // -Z
            BlockDirection.Left => Vector3.left, // -X
            BlockDirection.Right => Vector3.right, // +X
            _ => Vector3.zero
        };
    }

    public float GetBaseStrength()
    {
        return _type switch
        {
            BlockType.Arrow => 8f,
            BlockType.DoubleArrow => 14f,
            BlockType.Boost => 22f,
            BlockType.Sink => -10f, // or handle as attraction separately?
            BlockType.Source => 10f, // or handle as repulsion separately?
            _ => 0f
        };
    }
}