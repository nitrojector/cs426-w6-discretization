using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BlockSpriteManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _isShuttingDown = false;
    }

    public static BlockSpriteManager Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            // Prevent re-creating the singleton during shutdown/after destroy.
            if (_isShuttingDown)
                return null;

            _instance = FindFirstObjectByType<BlockSpriteManager>();
            if (_instance != null)
                return _instance;

            _instance = new GameObject("BlockSpriteManager").AddComponent<BlockSpriteManager>();
            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }

    private static BlockSpriteManager _instance;
    private static bool _isShuttingDown;

    private BlockMapping _mapping;
    private Dictionary<BlockType, Sprite> _lookup;

    private void Awake()
    {
        // Reset shutdown flag when entering play mode
        _isShuttingDown = false;

        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
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
        if (_mapping == null)
        {
            _mapping = Resources.Load<BlockMapping>("BlockMapping");
        }

        _lookup ??= new Dictionary<BlockType, Sprite>();
        _lookup.Clear();

        for (int i = 0; i < _mapping.sprites.Count; i++)
        {
            var e = _mapping.sprites[i];
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

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _isShuttingDown = true;
    }
}