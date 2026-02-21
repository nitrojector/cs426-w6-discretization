using System;
using UnityEngine;

public class Mgr : MonoBehaviour
{
    public static Mgr Instance { get; private set; }

    public BlockSpriteManager blockSpriteManager;
    public static BlockSpriteManager BSM => Instance != null ? Instance.blockSpriteManager : null;

    public BlockManager blockManager;
    public static BlockManager BM => Instance != null ? Instance.blockManager : null;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (blockSpriteManager == null)
            blockSpriteManager = GetComponent<BlockSpriteManager>();

        if (blockManager == null)
            blockManager = GetComponent<BlockManager>();
    }
}