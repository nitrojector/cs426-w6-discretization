using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
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
}