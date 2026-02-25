using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class ArrowPlacer : MonoBehaviour
    {
        private InputAction _lmbAction;
        private InputAction _scrollAction;

        private BlockDirection _placementDirection;
        private BlockCell _lastSelectedCell;

        private void Awake()
        {
            _lmbAction = InputSystem.actions.FindAction("Attack");
            _scrollAction = InputSystem.actions.FindAction("Scroll");
        }

        private void Update()
        {
            PlacementCheck();

            if (_scrollAction.triggered)
            {
                int dirCount = Enum.GetValues(typeof(BlockDirection)).Length;
                int scrollValue = (int)_scrollAction.ReadValue<float>();
                int dir = (int)_placementDirection - scrollValue;
                if (dir < 0) dir += dirCount;
                _placementDirection = (BlockDirection)(dir % dirCount);
                _lastSelectedCell.SetPreview(BlockType.Arrow, _placementDirection);
            }
        }

        private void PlacementCheck()
        {
            if (!TryGetBlockCellFromMouseRay(out var cell, out var hit) || cell == null || cell.Type != BlockType.None)
                return;


            if (_lastSelectedCell != cell)
            {
                if (_lastSelectedCell != null)
                {
                    _lastSelectedCell.UnsetPreview();
                }

                _lastSelectedCell = cell;
                _lastSelectedCell.SetPreview(BlockType.Arrow, _placementDirection);
            }

            if (_lmbAction.triggered)
            {
                Place(cell);
            }
        }

        private void Place(BlockCell cell)
        {
            cell.Type = BlockType.Arrow;
            cell.Dir = _placementDirection;
            cell.UnsetPreview();
        }

        private bool TryGetBlockCellFromMouseRay(out BlockCell cell, out RaycastHit hit, float maxDistance = 100f,
            LayerMask blockMask = default)
        {
            cell = null;
            hit = default;

            if (Mouse.current == null)
                return false;

            var cam = UnityEngine.Camera.main;
            if (cam == null)
                return false;

            var mousePos = Mouse.current.position.ReadValue();
            var ray = cam.ScreenPointToRay(mousePos);
            int mask = blockMask == default ? Physics.DefaultRaycastLayers : blockMask.value;

            if (!Physics.Raycast(ray, out hit, maxDistance, mask))
                return false;

            cell = hit.collider.GetComponentInParent<BlockCell>();
            return cell != null;
        }
    }
}