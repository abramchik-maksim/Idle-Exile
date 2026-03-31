using System;
using System.Collections.Generic;
using Game.Domain.Progression.TreeTalents;
using Game.Presentation.UI.Base;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Presentation.UI.MainScreen
{
    public sealed class TreeTalentsTabView : LayoutView
    {
        private VisualElement _treeGrid;
        private ScrollView _branchInventoryScroll;
        private VisualElement _branchInventoryList;
        private Label _levelLabel;
        private Label _xpLabel;
        private VisualElement _xpFill;
        private Button _btnGrowBranch;
        private Button _seedAButton;
        private Button _seedBButton;
        private Button _seedCButton;
        private VisualElement _dragGhost;
        private Label _dragGhostLabel;
        private VisualElement _tooltip;
        private Label _tooltipTitle;
        private Label _tooltipBody;

        private SeedType _seedA = SeedType.Fire;
        private SeedType _seedB = SeedType.Speed;
        private SeedType _seedC = SeedType.Defense;

        private bool _isDragging;
        private int _dragPointerId = -1;
        private VisualElement _dragCaptureOwner;
        private BranchInstance _draggedBranch;
        private GridCoord _hoverAnchor;

        private readonly Dictionary<string, Button> _gridCells = new();
        private readonly List<string> _previewKeys = new();
        private TreeTalentsState _lastState;
        private IReadOnlyList<int> _lastHalfWidths;

        public event Action<SeedType, SeedType, SeedType> OnGrowBranchRequested;
        public event Action<string, int, int> OnPlaceBranchRequested;

        protected override void OnBind()
        {
            _treeGrid = Q("tree-grid");
            _branchInventoryScroll = Q<ScrollView>("branch-inventory-scroll");
            _branchInventoryList = Q("branch-inventory-list");
            _levelLabel = Q<Label>("tree-level-label");
            _xpLabel = Q<Label>("tree-xp-label");
            _xpFill = Q("tree-xp-fill");
            _btnGrowBranch = Q<Button>("btn-grow-branch");
            _seedAButton = Q<Button>("seed-slot-a");
            _seedBButton = Q<Button>("seed-slot-b");
            _seedCButton = Q<Button>("seed-slot-c");

            _btnGrowBranch.clicked += () => OnGrowBranchRequested?.Invoke(_seedA, _seedB, _seedC);
            _seedAButton.clicked += () => CycleSeed(ref _seedA, _seedAButton);
            _seedBButton.clicked += () => CycleSeed(ref _seedB, _seedBButton);
            _seedCButton.clicked += () => CycleSeed(ref _seedC, _seedCButton);

            Root.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            Root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);

            BuildOverlayElements();
            RefreshSeedButtons();
        }

        public void RenderTreeState(TreeTalentsState state, IReadOnlyList<int> halfWidthsByRow)
        {
            if (state == null) return;
            _lastState = state;
            _lastHalfWidths = halfWidthsByRow;

            _levelLabel.text = $"Tree Level: {state.Level}";
            _xpLabel.text = $"{state.CurrentXp} / {state.XpToNextLevel}";
            var pct = Mathf.Clamp01(state.XpToNextLevel > 0
                ? state.CurrentXp / (float)state.XpToNextLevel : 0f) * 100f;
            _xpFill.style.width = new Length(pct, LengthUnit.Percent);

            RenderTreeGrid(state, halfWidthsByRow);
            RenderBranchInventory(state.BranchInventory);
        }

        #region Tree Grid

        private void RenderTreeGrid(TreeTalentsState state, IReadOnlyList<int> halfWidthsByRow)
        {
            _treeGrid.Clear();
            _gridCells.Clear();
            if (halfWidthsByRow == null) return;

            var occupied = new HashSet<GridCoord>();
            foreach (var branch in state.PlacedBranches)
            foreach (var coord in branch.GetWorldCoords())
                occupied.Add(coord);

            var sockets = new HashSet<GridCoord>(state.GetAvailableSockets());
            var trunk = BuildTrunkCoords();

            for (var y = halfWidthsByRow.Count - 1; y >= 0; y--)
            {
                var row = new VisualElement();
                row.AddToClassList("tree-grid-row");

                var sideWidth = Math.Max(0, halfWidthsByRow[y]);
                for (var x = -sideWidth; x <= sideWidth + 1; x++)
                {
                    var cell = new Button();
                    cell.AddToClassList("tree-cell");
                    cell.text = "";
                    cell.userData = new Vector2Int(x, y);

                    var gc = new GridCoord(x, y);
                    if (trunk.Contains(gc))
                        cell.AddToClassList("tree-cell--trunk");
                    else if (occupied.Contains(gc))
                        cell.AddToClassList("tree-cell--occupied");
                    else if (sockets.Contains(gc))
                        cell.AddToClassList("tree-cell--socket");
                    else
                        cell.AddToClassList("tree-cell--empty");

                    _gridCells[CoordKey(x, y)] = cell;
                    row.Add(cell);
                }

                _treeGrid.Add(row);
            }
        }

        private static HashSet<GridCoord> BuildTrunkCoords()
        {
            var trunk = new HashSet<GridCoord>();
            for (var y = 0; y < TreeTalentsState.TrunkHeight; y++)
            for (var x = 0; x < TreeTalentsState.TrunkWidth; x++)
                trunk.Add(new GridCoord(x, y));
            return trunk;
        }

        #endregion

        #region Branch Inventory

        private void RenderBranchInventory(IReadOnlyList<BranchInstance> branches)
        {
            _branchInventoryList.Clear();
            if (branches == null) return;

            foreach (var branch in branches)
            {
                var cell = new VisualElement();
                cell.AddToClassList("branch-inventory-cell");
                cell.userData = branch.Id;

                var title = new Label($"Branch {branch.Id[..Math.Min(6, branch.Id.Length)]}");
                title.AddToClassList("branch-inventory-cell__title");
                cell.Add(title);

                var desc = new Label($"Tiles: {branch.Tiles.Count} | Seeds: {string.Join(", ", branch.SeedTypes)}");
                desc.AddToClassList("branch-inventory-cell__desc");
                cell.Add(desc);

                var capturedBranch = branch;
                cell.RegisterCallback<PointerDownEvent>(evt => OnInventoryCellPointerDown(evt, capturedBranch));
                cell.RegisterCallback<PointerMoveEvent>(OnCapturePointerMove);
                cell.RegisterCallback<PointerUpEvent>(OnCapturePointerUp);
                cell.RegisterCallback<PointerCaptureOutEvent>(OnCapturePointerLost);
                cell.RegisterCallback<PointerEnterEvent>(_ => ShowBranchTooltip(capturedBranch, cell));
                cell.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());

                _branchInventoryList.Add(cell);
            }
        }

        #endregion

        #region Drag & Drop

        private void OnInventoryCellPointerDown(PointerDownEvent evt, BranchInstance branch)
        {
            if (evt.button != 0) return;
            if (_isDragging) CancelDrag();

            _isDragging = true;
            _dragPointerId = evt.pointerId;
            _draggedBranch = branch;
            _hoverAnchor = InvalidAnchor();
            _dragCaptureOwner = evt.currentTarget as VisualElement;
            _dragCaptureOwner?.CapturePointer(_dragPointerId);

            _dragGhostLabel.text = $"Branch {branch.Id[..Math.Min(6, branch.Id.Length)]}";
            MoveGhostTo(evt.position);
            _dragGhost.style.display = DisplayStyle.Flex;

            HideTooltip();
            evt.StopPropagation();
        }

        private void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || evt.pointerId != _dragPointerId) return;

            MoveGhostTo(evt.position);
            UpdatePreview(evt.position);
        }

        private void OnGlobalPointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || evt.pointerId != _dragPointerId) return;

            if (IsValidAnchor(_hoverAnchor) && CanPlacePreview(_draggedBranch, _hoverAnchor))
                OnPlaceBranchRequested?.Invoke(_draggedBranch.Id, _hoverAnchor.X, _hoverAnchor.Y);

            FinishDrag();
        }

        private void OnCapturePointerMove(PointerMoveEvent evt)
        {
            if (!_isDragging || evt.pointerId != _dragPointerId) return;
            MoveGhostTo(evt.position);
            UpdatePreview(evt.position);
        }

        private void OnCapturePointerUp(PointerUpEvent evt)
        {
            if (!_isDragging || evt.pointerId != _dragPointerId) return;

            if (IsValidAnchor(_hoverAnchor) && CanPlacePreview(_draggedBranch, _hoverAnchor))
                OnPlaceBranchRequested?.Invoke(_draggedBranch.Id, _hoverAnchor.X, _hoverAnchor.Y);

            FinishDrag();
        }

        private void OnCapturePointerLost(PointerCaptureOutEvent evt)
        {
            if (!_isDragging) return;
            CancelDrag();
        }

        private void FinishDrag()
        {
            if (_dragCaptureOwner != null && _dragCaptureOwner.HasPointerCapture(_dragPointerId))
                _dragCaptureOwner.ReleasePointer(_dragPointerId);

            _isDragging = false;
            _dragPointerId = -1;
            _dragCaptureOwner = null;
            _draggedBranch = null;
            _dragGhost.style.display = DisplayStyle.None;
            ClearPreview();
            _hoverAnchor = InvalidAnchor();
        }

        private void CancelDrag()
        {
            if (!_isDragging) return;
            FinishDrag();
        }

        private void MoveGhostTo(Vector3 position)
        {
            _dragGhost.style.left = position.x + 12f;
            _dragGhost.style.top = position.y - 14f;
        }

        private void UpdatePreview(Vector3 pointerPosition)
        {
            var picked = Root?.panel?.Pick(new Vector2(pointerPosition.x, pointerPosition.y));
            while (picked != null && !picked.ClassListContains("tree-cell"))
                picked = picked.parent;

            if (picked?.userData is not Vector2Int v2)
            {
                ClearPreview();
                _hoverAnchor = InvalidAnchor();
                return;
            }

            var anchor = new GridCoord(v2.x, v2.y);
            if (anchor.X == _hoverAnchor.X && anchor.Y == _hoverAnchor.Y)
                return;

            _hoverAnchor = anchor;
            DrawPreview(_draggedBranch, anchor);
        }

        private void DrawPreview(BranchInstance branch, GridCoord anchor)
        {
            ClearPreview();
            if (branch == null) return;

            var valid = CanPlacePreview(branch, anchor);
            var cls = valid ? "tree-cell--preview-valid" : "tree-cell--preview";

            foreach (var tile in branch.Tiles)
            {
                var coord = anchor.Add(tile.Offset);
                var key = CoordKey(coord.X, coord.Y);
                if (!_gridCells.TryGetValue(key, out var cell)) continue;

                cell.AddToClassList(cls);
                _previewKeys.Add(key);
            }
        }

        private bool CanPlacePreview(BranchInstance branch, GridCoord anchor)
        {
            if (_lastState == null || _lastHalfWidths == null || branch == null) return false;

            var sockets = _lastState.GetAvailableSockets();
            var found = false;
            foreach (var s in sockets)
            {
                if (s == anchor) { found = true; break; }
            }
            if (!found) return false;

            var blocked = BuildBlockedCoords();
            foreach (var placed in _lastState.PlacedBranches)
            foreach (var c in placed.GetWorldCoords())
                blocked.Add(c);

            foreach (var tile in branch.Tiles)
            {
                var c = anchor.Add(tile.Offset);
                if (c.Y < 0 || c.Y >= _lastHalfWidths.Count) return false;
                var sideWidth = Math.Max(0, _lastHalfWidths[c.Y]);
                if (c.X < -sideWidth || c.X > sideWidth + 1) return false;
                if (blocked.Contains(c)) return false;
            }

            return true;
        }

        private static HashSet<GridCoord> BuildBlockedCoords()
        {
            var set = new HashSet<GridCoord>();
            for (var y = 0; y < TreeTalentsState.TrunkHeight; y++)
            for (var x = 0; x < TreeTalentsState.TrunkWidth; x++)
                set.Add(new GridCoord(x, y));
            return set;
        }

        private void ClearPreview()
        {
            foreach (var key in _previewKeys)
            {
                if (!_gridCells.TryGetValue(key, out var cell)) continue;
                cell.RemoveFromClassList("tree-cell--preview");
                cell.RemoveFromClassList("tree-cell--preview-valid");
            }

            _previewKeys.Clear();
        }

        #endregion

        #region Tooltip

        private void ShowBranchTooltip(BranchInstance branch, VisualElement anchor)
        {
            if (branch == null || _isDragging) return;

            _tooltipTitle.text = $"Branch {branch.Id[..Math.Min(6, branch.Id.Length)]}";
            var lines = new List<string>();
            foreach (var tile in branch.Tiles)
            {
                var node = tile.Node;
                if (node.IsSocket)
                    lines.Add("Socket (branch attachment point)");
                else
                    lines.Add($"{node.AllianceType} | +{node.Value:0.##} | {node.NodeType}");
            }

            _tooltipBody.text = string.Join("\n", lines);

            var bounds = anchor.worldBound;
            _tooltip.style.left = bounds.xMin - 230f;
            _tooltip.style.top = bounds.yMin;
            _tooltip.style.display = DisplayStyle.Flex;
        }

        private void HideTooltip()
        {
            _tooltip.style.display = DisplayStyle.None;
        }

        #endregion

        #region Overlay Elements

        private void BuildOverlayElements()
        {
            _dragGhost = new VisualElement();
            _dragGhost.AddToClassList("branch-drag-ghost");
            _dragGhost.style.display = DisplayStyle.None;
            _dragGhost.pickingMode = PickingMode.Ignore;
            _dragGhostLabel = new Label();
            _dragGhostLabel.pickingMode = PickingMode.Ignore;
            _dragGhost.Add(_dragGhostLabel);
            Root.Add(_dragGhost);

            _tooltip = new VisualElement();
            _tooltip.AddToClassList("branch-tooltip");
            _tooltip.style.display = DisplayStyle.None;
            _tooltip.pickingMode = PickingMode.Ignore;
            _tooltipTitle = new Label();
            _tooltipTitle.AddToClassList("branch-tooltip__title");
            _tooltipTitle.pickingMode = PickingMode.Ignore;
            _tooltipBody = new Label();
            _tooltipBody.AddToClassList("branch-tooltip__body");
            _tooltipBody.pickingMode = PickingMode.Ignore;
            _tooltip.Add(_tooltipTitle);
            _tooltip.Add(_tooltipBody);
            Root.Add(_tooltip);
        }

        #endregion

        #region Seeds

        private void CycleSeed(ref SeedType seed, Button button)
        {
            var count = Enum.GetValues(typeof(SeedType)).Length;
            seed = (SeedType)(((int)seed + 1) % count);
            button.text = seed.ToString();
        }

        private void RefreshSeedButtons()
        {
            _seedAButton.text = _seedA.ToString();
            _seedBButton.text = _seedB.ToString();
            _seedCButton.text = _seedC.ToString();
        }

        #endregion

        #region Helpers

        private static string CoordKey(int x, int y) => $"{x}:{y}";

        private static GridCoord InvalidAnchor() => new(int.MinValue, int.MinValue);

        private static bool IsValidAnchor(GridCoord a) => a.X != int.MinValue;

        #endregion

        public override void Dispose()
        {
            CancelDrag();
            Root?.UnregisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            Root?.UnregisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        }
    }
}
