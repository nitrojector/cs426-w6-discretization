using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    public static BlockManager Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            // Prevent re-creating the singleton during shutdown/after destroy.
            if (_isShuttingDown)
                return null;
            
            _instance = FindFirstObjectByType<BlockManager>();
            if (_instance != null)
                return _instance;
            
            _instance = new GameObject("BlockManager").AddComponent<BlockManager>();
            DontDestroyOnLoad(_instance.gameObject);
            return _instance;
        }
    }

    private static BlockManager _instance;
    private static bool _isShuttingDown;
    public IReadOnlyList<BlockCell> Cells => _cells;

    List<BlockCell> _cells = new List<BlockCell>();

    public List<BlockCell> GetAllSpecialCells() => _cells.Where(c => c.Type != BlockType.None).ToList();

    public void RegisterCell(BlockCell cell)
    {
        _cells.Add(cell);
    }

    public void UnregisterCell(BlockCell cell)
    {
        _cells.Remove(cell);
    }

    public void ClearCells()
    {
        _cells.Clear();
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