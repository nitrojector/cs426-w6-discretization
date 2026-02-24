using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockMapping", menuName = "ScriptableObjects/BlockMapping", order = 1)]
public class BlockMapping :  ScriptableObject
{
    [Serializable] public struct Entry
    {
        public BlockType type;
        public Sprite sprite;
    }

    [Header("Block Sprites")]
    [SerializeField] public List<Entry> sprites = new();
}