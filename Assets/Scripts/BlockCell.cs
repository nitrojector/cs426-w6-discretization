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
        Mgr.BM.RegisterCell(this);
    }

    private void OnDestroy()
    {
        Mgr.BM.UnregisterCell(this);
    }

    private void ApplyVisual()
    {
        if (_spriteRenderer == null) return;

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

        var sprite = Mgr.BSM.GetSprite(_type);
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
                _spriteRenderer.transform.localScale = new Vector3(scaleX, scaleY, 1f);
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
}