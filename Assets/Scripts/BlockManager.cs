using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BlockManager : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
        _isShuttingDown = false;
    }

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

    public IReadOnlyList<BlockCell> Cells => _cells;
    public float CellSize => cellSize;

    private static BlockManager _instance;
    private static bool _isShuttingDown;

    [SerializeField] private float cellSize = 1f;

    private readonly Dictionary<Vector2Int, BlockCell> _grid = new();

    List<BlockCell> _cells = new List<BlockCell>();

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
    }

    public List<BlockCell> GetAllSpecialCells() => _cells.Where(c => c.Type != BlockType.None).ToList();

    private Vector2Int WorldToCellKey(Vector3 p)
    {
        // XZ grid
        int gx = Mathf.RoundToInt(p.x / cellSize);
        int gz = Mathf.RoundToInt(p.z / cellSize);
        return new Vector2Int(gx, gz);
    }

    public void RegisterCell(BlockCell cell)
    {
        _cells.Add(cell);
        _grid[WorldToCellKey(cell.transform.position)] = cell;
    }

    public void UnregisterCell(BlockCell cell)
    {
        _cells.Remove(cell);
        var key = WorldToCellKey(cell.transform.position);
        if (_grid.TryGetValue(key, out var existing) && existing == cell)
            _grid.Remove(key);
    }

    public bool TryGetCellAt(Vector3 worldPos, out BlockCell cell)
        => _grid.TryGetValue(WorldToCellKey(worldPos), out cell);

    public IEnumerable<BlockCell> GetCellsInRadius(Vector3 worldPos, int radiusCells)
    {
        var center = WorldToCellKey(worldPos);
        for (int dx = -radiusCells; dx <= radiusCells; dx++)
        for (int dz = -radiusCells; dz <= radiusCells; dz++)
        {
            var k = new Vector2Int(center.x + dx, center.y + dz);
            if (_grid.TryGetValue(k, out var c))
                yield return c;
        }
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