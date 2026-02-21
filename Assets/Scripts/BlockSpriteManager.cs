using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BlockSpriteManager : MonoBehaviour
{
    [Serializable]
    public struct Entry
    {
        public BlockType type;
        public Sprite sprite;
    }

    [Header("Block Sprites")]
    [SerializeField] private Entry[] _sprites;

    private Dictionary<BlockType, Sprite> _lookup;

    private void Awake()
    {
        BuildLookup();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildLookup();
    }
#endif

    private void BuildLookup()
    {
        _lookup ??= new Dictionary<BlockType, Sprite>();
        _lookup.Clear();

        for (int i = 0; i < _sprites.Length; i++)
        {
            var e = _sprites[i];
            if (e.type == BlockType.None || e.sprite == null)
                continue;

            // Last one wins (lets you override without errors)
            _lookup[e.type] = e.sprite;
        }
    }

    public Sprite GetSprite(BlockType type)
    {
        if (type == BlockType.None)
            return null;

        return _lookup != null && _lookup.TryGetValue(type, out var sprite)
            ? sprite
            : null;
    }
}