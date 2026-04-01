using System;
using System.Collections.Generic;
using System.Linq;
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
        private VisualElement _allianceSummary;
        private Button _btnGrowBranch;
        private VisualElement _seedPoolList;
        private VisualElement _selectedSeedsList;
        private VisualElement _growDropZone;
        private Label _growDropZoneHint;
        private VisualElement _growProgressRoot;
        private VisualElement _growProgressFill;
        private Label _growProgressLabel;
        private VisualElement _dragGhost;
        private Label _dragGhostLabel;
        private VisualElement _tooltip;
        private Label _tooltipTitle;
        private Label _tooltipBody;
        private VisualElement _removeConfirmDialog;
        private Label _removeConfirmText;
        private Button _removeConfirmYesButton;
        private Button _removeConfirmNoButton;

        private bool _isDraggingBranch;
        private int _dragBranchPointerId = -1;
        private VisualElement _dragCaptureOwner;
        private BranchInstance _draggedBranch;
        private int _dragBranchRotationQuarterTurns;
        private GridCoord _hoverAnchor;
        private bool _isDraggingSeed;
        private int _dragSeedPointerId = -1;
        private SeedType _draggedSeed;
        private bool _isGrowthInProgress;
        private readonly List<SeedType> _selectedSeeds = new();
        private readonly Dictionary<SeedType, string> _seedDescriptions = new()
        {
            { SeedType.Fire, "Bias toward fire-type nodes." },
            { SeedType.Speed, "Bias toward speed-type nodes." },
            { SeedType.Defense, "Bias toward defense-type nodes." },
            { SeedType.Crit, "Bias toward crit-type nodes." },
            { SeedType.Bleed, "Bias toward bleed-type nodes." },
            { SeedType.Utility, "Bias toward utility nodes." },
            { SeedType.Growth, "Bias toward growth and socket nodes." },
            { SeedType.Universal, "Balanced seed for mixed outcomes." }
        };

        private readonly Dictionary<string, Button> _gridCells = new();
        private readonly Dictionary<string, BranchInstance> _branchByCoordKey = new();
        private readonly List<string> _previewKeys = new();
        private BranchInstance _pendingRemoveBranch;
        private TreeTalentsState _lastState;
        private IReadOnlyList<int> _lastHalfWidths;
        private IReadOnlyList<int> _allianceThresholds = new List<int> { 5, 10, 20, 30, 40 };

        public event Action<IReadOnlyList<SeedType>> OnGrowBranchRequested;
        public event Action<string, int, int, int> OnPlaceBranchRequested;
        public event Action<string> OnRemoveBranchRequested;

        protected override void OnBind()
        {
            _treeGrid = Q("tree-grid");
            _branchInventoryScroll = Q<ScrollView>("branch-inventory-scroll");
            _branchInventoryList = Q("branch-inventory-list");
            _levelLabel = Q<Label>("tree-level-label");
            _xpLabel = Q<Label>("tree-xp-label");
            _xpFill = Q("tree-xp-fill");
            _allianceSummary = Q("tree-alliance-summary");
            _btnGrowBranch = Q<Button>("btn-grow-branch");
            _seedPoolList = Q("seed-pool-list");
            _selectedSeedsList = Q("selected-seeds-list");
            _growDropZone = Q("grow-drop-zone");
            _growDropZoneHint = Q<Label>("grow-drop-zone-hint");
            _growProgressRoot = Q("grow-progress-root");
            _growProgressFill = Q("grow-progress-fill");
            _growProgressLabel = Q<Label>("grow-progress-label");

            _btnGrowBranch.clicked += HandleGrowClicked;

            Root.RegisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            Root.RegisterCallback<PointerUpEvent>(OnGlobalPointerUp);

            BuildOverlayElements();
            BuildSeedPool();
            RenderSelectedSeeds();
            SetGrowthState(false, 0f, 0f);
            UpdateGrowButtonState();
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

            RenderAllianceSummary(state.AllianceCounts, _allianceThresholds);
            RenderTreeGrid(state, halfWidthsByRow);
            RenderBranchInventory(state.BranchInventory);
        }

        public void SetAllianceThresholds(IReadOnlyList<int> thresholds)
        {
            _allianceThresholds = thresholds != null && thresholds.Count > 0
                ? thresholds
                : new List<int> { 5, 10, 20, 30, 40 };
        }

        public bool IsDraggingBranch => _isDraggingBranch;

        public void RotateDraggedBranch(int deltaSteps)
        {
            if (!_isDraggingBranch || _draggedBranch == null || deltaSteps == 0) return;
            _dragBranchRotationQuarterTurns = NormalizeRotation(_dragBranchRotationQuarterTurns + deltaSteps);
            if (!IsValidAnchor(_hoverAnchor)) return;
            DrawPreview(_draggedBranch, _hoverAnchor);
        }

        public void CancelDragFromInput()
        {
            CancelBranchDrag();
        }

        public void SetGrowthState(bool isRunning, float remainingSeconds, float totalSeconds)
        {
            _isGrowthInProgress = isRunning;
            _growProgressRoot.style.display = isRunning ? DisplayStyle.Flex : DisplayStyle.None;
            _growDropZone.SetEnabled(!isRunning);
            _seedPoolList.SetEnabled(!isRunning);

            if (isRunning && totalSeconds > 0.001f)
            {
                var pct = Mathf.Clamp01(1f - (remainingSeconds / totalSeconds)) * 100f;
                _growProgressFill.style.width = new Length(pct, LengthUnit.Percent);
                _growProgressLabel.text = $"{Mathf.Max(0f, remainingSeconds):0.0}s";
                _growDropZoneHint.text = "Growing...";
            }
            else
            {
                _growProgressFill.style.width = new Length(0f, LengthUnit.Percent);
                _growProgressLabel.text = string.Empty;
                _growDropZoneHint.text = _selectedSeeds.Count >= 3 ? "Ready to grow" : "Drag 3 seeds here";
            }

            UpdateGrowButtonState();
        }

        public void ClearSelectedSeeds()
        {
            _selectedSeeds.Clear();
            RenderSelectedSeeds();
            UpdateGrowButtonState();
        }

        #region Tree Grid

        private void RenderTreeGrid(TreeTalentsState state, IReadOnlyList<int> halfWidthsByRow)
        {
            _treeGrid.Clear();
            _gridCells.Clear();
            _branchByCoordKey.Clear();
            if (halfWidthsByRow == null) return;

            var occupied = new HashSet<GridCoord>();
            foreach (var branch in state.PlacedBranches)
            foreach (var coord in branch.GetWorldCoords())
                occupied.Add(coord);

            var sockets = new HashSet<GridCoord>(state.GetAvailableSockets());
            var trunk = BuildTrunkCoords();
            foreach (var branch in state.PlacedBranches)
            foreach (var coord in branch.GetWorldCoords())
                _branchByCoordKey[CoordKey(coord.X, coord.Y)] = branch;

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
                    var key = CoordKey(x, y);
                    if (trunk.Contains(gc))
                        cell.AddToClassList("tree-cell--trunk");
                    else if (occupied.Contains(gc))
                        cell.AddToClassList("tree-cell--occupied");
                    else if (sockets.Contains(gc))
                        cell.AddToClassList("tree-cell--socket");
                    else
                        cell.AddToClassList("tree-cell--empty");

                    if (_branchByCoordKey.TryGetValue(key, out var placedBranch))
                    {
                        var capturedBranch = placedBranch;
                        cell.RegisterCallback<PointerEnterEvent>(_ => ShowBranchTooltip(capturedBranch, cell));
                        cell.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
                        cell.RegisterCallback<PointerDownEvent>(evt => HandlePlacedBranchPointerDown(evt, capturedBranch));
                    }

                    _gridCells[key] = cell;
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

        #region Alliances

        private void RenderAllianceSummary(
            IReadOnlyDictionary<NodeAllianceType, int> counts,
            IReadOnlyList<int> thresholds)
        {
            _allianceSummary.Clear();
            var active = counts?.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).ToList();
            if (active == null || active.Count == 0)
            {
                var empty = new Label("No active alliances");
                empty.AddToClassList("tree-alliance-summary__empty");
                _allianceSummary.Add(empty);
                return;
            }

            foreach (var kv in active)
            {
                var next = GetNextThreshold(kv.Value, thresholds);
                var label = new Label($"{kv.Key}: {kv.Value}/{next}");
                label.AddToClassList("tree-alliance-summary__line");
                var capturedType = kv.Key;
                var capturedCount = kv.Value;
                label.RegisterCallback<PointerEnterEvent>(_ =>
                    ShowAllianceTooltip(capturedType, capturedCount, thresholds, label));
                label.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
                _allianceSummary.Add(label);
            }
        }

        private void ShowAllianceTooltip(
            NodeAllianceType type,
            int count,
            IReadOnlyList<int> thresholds,
            VisualElement anchor)
        {
            _tooltipTitle.text = $"{type} Alliance";
            var lines = new List<string>();
            foreach (var t in thresholds)
            {
                var active = count >= t;
                var color = active ? "#74D674" : "#555A62";
                lines.Add($"<color={color}>{(active ? "Active" : "Locked")} - {t} nodes</color>");
            }
            _tooltipBody.text = string.Join("\n", lines);

            var bounds = anchor.worldBound;
            _tooltip.style.left = bounds.xMin - 220f;
            _tooltip.style.top = bounds.yMin;
            _tooltip.style.display = DisplayStyle.Flex;
        }

        private static int GetNextThreshold(int count, IReadOnlyList<int> thresholds)
        {
            if (thresholds == null || thresholds.Count == 0) return count;
            foreach (var t in thresholds)
                if (count < t) return t;
            return thresholds[thresholds.Count - 1];
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

                var body = new VisualElement();
                body.AddToClassList("branch-inventory-cell__body");
                cell.Add(body);

                var nodeIcons = new VisualElement();
                nodeIcons.AddToClassList("branch-node-icons");
                foreach (var tile in branch.Tiles)
                {
                    var icon = new VisualElement();
                    icon.AddToClassList("branch-node-icon");
                    icon.style.backgroundColor = GetNodeColor(tile.Node);
                    nodeIcons.Add(icon);
                }

                var miniShapeRoot = new VisualElement();
                miniShapeRoot.AddToClassList("branch-shape-preview");
                BuildMiniShape(branch, miniShapeRoot);

                body.Add(nodeIcons);
                body.Add(miniShapeRoot);

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
            if (_isDraggingBranch) CancelBranchDrag();

            _isDraggingBranch = true;
            _dragBranchPointerId = evt.pointerId;
            _draggedBranch = branch;
            _dragBranchRotationQuarterTurns = 0;
            _hoverAnchor = InvalidAnchor();
            _dragCaptureOwner = evt.currentTarget as VisualElement;
            _dragCaptureOwner?.CapturePointer(_dragBranchPointerId);

            _dragGhostLabel.text = $"Branch {branch.Id[..Math.Min(6, branch.Id.Length)]}";
            MoveGhostTo(evt.position);
            _dragGhost.style.display = DisplayStyle.Flex;

            HideTooltip();
            evt.StopPropagation();
        }

        private void OnGlobalPointerMove(PointerMoveEvent evt)
        {
            if (_isDraggingSeed && evt.pointerId == _dragSeedPointerId)
            {
                MoveGhostTo(evt.position);
                UpdateSeedDropZoneHover(evt.position);
                return;
            }
            if (!_isDraggingBranch || evt.pointerId != _dragBranchPointerId) return;

            MoveGhostTo(evt.position);
            UpdatePreview(evt.position);
        }

        private void OnGlobalPointerUp(PointerUpEvent evt)
        {
            if (_isDraggingSeed && evt.pointerId == _dragSeedPointerId)
            {
                TryDropSeed(evt.position);
                FinishSeedDrag();
                return;
            }
            if (!_isDraggingBranch || evt.pointerId != _dragBranchPointerId) return;

            if (IsValidAnchor(_hoverAnchor) && CanPlacePreview(_draggedBranch, _hoverAnchor))
                OnPlaceBranchRequested?.Invoke(
                    _draggedBranch.Id,
                    _hoverAnchor.X,
                    _hoverAnchor.Y,
                    _dragBranchRotationQuarterTurns);

            FinishBranchDrag();
        }

        private void OnCapturePointerMove(PointerMoveEvent evt)
        {
            if (!_isDraggingBranch || evt.pointerId != _dragBranchPointerId) return;
            MoveGhostTo(evt.position);
            UpdatePreview(evt.position);
        }

        private void OnCapturePointerUp(PointerUpEvent evt)
        {
            if (!_isDraggingBranch || evt.pointerId != _dragBranchPointerId) return;

            if (IsValidAnchor(_hoverAnchor) && CanPlacePreview(_draggedBranch, _hoverAnchor))
                OnPlaceBranchRequested?.Invoke(
                    _draggedBranch.Id,
                    _hoverAnchor.X,
                    _hoverAnchor.Y,
                    _dragBranchRotationQuarterTurns);

            FinishBranchDrag();
        }

        private void OnCapturePointerLost(PointerCaptureOutEvent evt)
        {
            if (!_isDraggingBranch) return;
            CancelBranchDrag();
        }

        private void HandlePlacedBranchPointerDown(PointerDownEvent evt, BranchInstance branch)
        {
            if (evt.button != 1 || branch == null) return;
            if (_isDraggingBranch) CancelBranchDrag();
            ShowRemoveConfirm(branch, evt.currentTarget as VisualElement);
            evt.StopPropagation();
        }

        private void FinishBranchDrag()
        {
            if (_dragCaptureOwner != null && _dragCaptureOwner.HasPointerCapture(_dragBranchPointerId))
                _dragCaptureOwner.ReleasePointer(_dragBranchPointerId);

            _isDraggingBranch = false;
            _dragBranchPointerId = -1;
            _dragCaptureOwner = null;
            _draggedBranch = null;
            _dragBranchRotationQuarterTurns = 0;
            _dragGhost.style.display = DisplayStyle.None;
            ClearPreview();
            _hoverAnchor = InvalidAnchor();
        }

        private void CancelBranchDrag()
        {
            if (!_isDraggingBranch) return;
            FinishBranchDrag();
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
                var offset = BranchInstance.GetRotatedOffset(tile.Offset, _dragBranchRotationQuarterTurns);
                var coord = anchor.Add(offset);
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
                var offset = BranchInstance.GetRotatedOffset(tile.Offset, _dragBranchRotationQuarterTurns);
                var c = anchor.Add(offset);
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
            if (branch == null || _isDraggingBranch || _isDraggingSeed) return;

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

        private void ShowRemoveConfirm(BranchInstance branch, VisualElement anchor)
        {
            _pendingRemoveBranch = branch;
            var hasDependent = HasDependentBranchesInViewState(branch);
            if (hasDependent)
            {
                _removeConfirmText.text = "Cannot delete:\nremove dependent branches first.";
                _removeConfirmYesButton.SetEnabled(false);
            }
            else
            {
                _removeConfirmText.text = $"Delete branch {branch.Id[..Math.Min(6, branch.Id.Length)]}?";
                _removeConfirmYesButton.SetEnabled(true);
            }

            if (anchor != null)
            {
                var bounds = anchor.worldBound;
                _removeConfirmDialog.style.left = bounds.xMin - 230f;
                var tooltipHeight = _tooltip.style.display == DisplayStyle.Flex
                    ? _tooltip.worldBound.height
                    : 0f;
                _removeConfirmDialog.style.top = bounds.yMin + (tooltipHeight > 0f ? tooltipHeight : 96f);
            }
            _removeConfirmDialog.style.display = DisplayStyle.Flex;
        }

        private void HideRemoveConfirm()
        {
            _pendingRemoveBranch = null;
            _removeConfirmDialog.style.display = DisplayStyle.None;
        }

        private bool HasDependentBranchesInViewState(BranchInstance parent)
        {
            if (_lastState == null || parent == null) return false;
            var provided = GetProvidedSocketsForBranch(parent);
            foreach (var branch in _lastState.PlacedBranches)
            {
                if (branch.Id == parent.Id) continue;
                if (provided.Contains(branch.Anchor))
                    return true;
            }
            return false;
        }

        private static HashSet<GridCoord> GetProvidedSocketsForBranch(BranchInstance branch)
        {
            var result = new HashSet<GridCoord>();
            foreach (var tile in branch.Tiles)
            {
                if (tile.Node == null || !tile.Node.IsSocket) continue;
                var rotated = BranchInstance.GetRotatedOffset(tile.Offset, branch.PlacedRotationQuarterTurns);
                var center = branch.Anchor.Add(rotated);
                result.Add(center.Add(new GridCoord(1, 0)));
                result.Add(center.Add(new GridCoord(-1, 0)));
                result.Add(center.Add(new GridCoord(0, 1)));
                result.Add(center.Add(new GridCoord(0, -1)));
            }
            return result;
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
            _tooltipBody.enableRichText = true;
            _tooltip.Add(_tooltipTitle);
            _tooltip.Add(_tooltipBody);
            Root.Add(_tooltip);

            _removeConfirmDialog = new VisualElement();
            _removeConfirmDialog.AddToClassList("tree-confirm-dialog");
            _removeConfirmDialog.style.display = DisplayStyle.None;

            _removeConfirmText = new Label("Delete branch?");
            _removeConfirmText.AddToClassList("tree-confirm-dialog__text");
            _removeConfirmDialog.Add(_removeConfirmText);

            var actions = new VisualElement();
            actions.AddToClassList("tree-confirm-dialog__actions");
            _removeConfirmYesButton = new Button(() =>
            {
                var id = _pendingRemoveBranch?.Id;
                HideRemoveConfirm();
                if (!string.IsNullOrEmpty(id))
                    OnRemoveBranchRequested?.Invoke(id);
            }) { text = "Delete" };
            _removeConfirmYesButton.AddToClassList("btn");
            _removeConfirmYesButton.AddToClassList("tree-confirm-dialog__btn");
            _removeConfirmNoButton = new Button(HideRemoveConfirm) { text = "Cancel" };
            _removeConfirmNoButton.AddToClassList("btn");
            _removeConfirmNoButton.AddToClassList("tree-confirm-dialog__btn");
            actions.Add(_removeConfirmYesButton);
            actions.Add(_removeConfirmNoButton);
            _removeConfirmDialog.Add(actions);

            Root.Add(_removeConfirmDialog);
        }

        #endregion

        #region Seeds & Growth

        private void BuildSeedPool()
        {
            _seedPoolList.Clear();
            var values = (SeedType[])Enum.GetValues(typeof(SeedType));
            foreach (var seed in values)
            {
                var item = new VisualElement();
                item.AddToClassList("seed-pool-item");
                item.userData = seed;

                var icon = new VisualElement();
                icon.AddToClassList("seed-pool-item__icon");
                icon.style.backgroundColor = GetSeedColor(seed);
                item.Add(icon);

                var label = new Label(seed.ToString());
                label.AddToClassList("seed-pool-item__label");
                item.Add(label);

                item.RegisterCallback<PointerDownEvent>(evt => OnSeedPointerDown(evt, seed));
                item.RegisterCallback<PointerEnterEvent>(_ => ShowSeedTooltip(seed, item));
                item.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
                _seedPoolList.Add(item);
            }
        }

        private void OnSeedPointerDown(PointerDownEvent evt, SeedType seed)
        {
            if (_isGrowthInProgress || evt.button != 0) return;
            if (_isDraggingBranch) CancelBranchDrag();
            if (_isDraggingSeed) FinishSeedDrag();

            _isDraggingSeed = true;
            _dragSeedPointerId = evt.pointerId;
            _draggedSeed = seed;
            _dragGhostLabel.text = seed.ToString();
            MoveGhostTo(evt.position);
            _dragGhost.style.display = DisplayStyle.Flex;
            UpdateSeedDropZoneHover(evt.position);
            evt.StopPropagation();
        }

        private void UpdateSeedDropZoneHover(Vector2 pointerPosition)
        {
            var isHover = _growDropZone.worldBound.Contains(pointerPosition);
            _growDropZone.EnableInClassList("grow-drop-zone--hover", isHover);
        }

        private void TryDropSeed(Vector2 pointerPosition)
        {
            if (!_growDropZone.worldBound.Contains(pointerPosition)) return;
            if (_selectedSeeds.Count >= 3) return;

            _selectedSeeds.Add(_draggedSeed);
            RenderSelectedSeeds();
            UpdateGrowButtonState();
        }

        private void FinishSeedDrag()
        {
            _isDraggingSeed = false;
            _dragSeedPointerId = -1;
            _growDropZone.EnableInClassList("grow-drop-zone--hover", false);
            _dragGhost.style.display = DisplayStyle.None;
        }

        private void HandleGrowClicked()
        {
            if (_isGrowthInProgress || _selectedSeeds.Count != 3) return;
            OnGrowBranchRequested?.Invoke(new List<SeedType>(_selectedSeeds));
        }

        private void RenderSelectedSeeds()
        {
            _selectedSeedsList.Clear();
            foreach (var seed in _selectedSeeds)
            {
                var icon = new VisualElement();
                icon.AddToClassList("selected-seed-icon");
                icon.style.backgroundColor = GetSeedColor(seed);
                _selectedSeedsList.Add(icon);
            }
        }

        private void UpdateGrowButtonState()
        {
            _btnGrowBranch.SetEnabled(!_isGrowthInProgress && _selectedSeeds.Count == 3);
        }

        private void ShowSeedTooltip(SeedType seed, VisualElement anchor)
        {
            if (_isDraggingBranch || _isDraggingSeed) return;
            _tooltipTitle.text = seed.ToString();
            _tooltipBody.text = _seedDescriptions.TryGetValue(seed, out var text) ? text : "Seed";
            var bounds = anchor.worldBound;
            _tooltip.style.left = bounds.xMin - 220f;
            _tooltip.style.top = bounds.yMin;
            _tooltip.style.display = DisplayStyle.Flex;
        }

        private static Color GetSeedColor(SeedType seed)
        {
            return seed switch
            {
                SeedType.Fire => new Color(0.95f, 0.42f, 0.26f),
                SeedType.Speed => new Color(0.35f, 0.83f, 0.96f),
                SeedType.Defense => new Color(0.52f, 0.82f, 0.43f),
                SeedType.Crit => new Color(0.95f, 0.78f, 0.24f),
                SeedType.Bleed => new Color(0.78f, 0.24f, 0.35f),
                SeedType.Utility => new Color(0.66f, 0.56f, 0.96f),
                SeedType.Growth => new Color(0.42f, 0.93f, 0.58f),
                SeedType.Universal => new Color(0.86f, 0.86f, 0.9f),
                _ => new Color(0.7f, 0.7f, 0.7f)
            };
        }

        #endregion

        #region Inventory Preview Helpers

        private static Color GetNodeColor(BranchNode node)
        {
            if (node == null) return new Color(0.55f, 0.55f, 0.55f);
            if (node.IsSocket) return new Color(0.78f, 0.66f, 0.3f);
            return node.AllianceType switch
            {
                NodeAllianceType.Fire => new Color(0.95f, 0.42f, 0.26f),
                NodeAllianceType.Speed => new Color(0.35f, 0.83f, 0.96f),
                NodeAllianceType.Defense => new Color(0.52f, 0.82f, 0.43f),
                NodeAllianceType.Crit => new Color(0.95f, 0.78f, 0.24f),
                NodeAllianceType.Bleed => new Color(0.78f, 0.24f, 0.35f),
                NodeAllianceType.Utility => new Color(0.66f, 0.56f, 0.96f),
                NodeAllianceType.Growth => new Color(0.42f, 0.93f, 0.58f),
                NodeAllianceType.Universal => new Color(0.86f, 0.86f, 0.9f),
                _ => new Color(0.65f, 0.65f, 0.65f)
            };
        }

        private static void BuildMiniShape(BranchInstance branch, VisualElement root)
        {
            if (branch == null || branch.Tiles.Count == 0) return;
            var minX = branch.Tiles.Min(t => t.Offset.X);
            var minY = branch.Tiles.Min(t => t.Offset.Y);
            var maxX = branch.Tiles.Max(t => t.Offset.X);
            var maxY = branch.Tiles.Max(t => t.Offset.Y);
            var width = Math.Max(1, maxX - minX + 1);
            var height = Math.Max(1, maxY - minY + 1);

            root.style.width = width * 8 + (width - 1);
            root.style.height = height * 8 + (height - 1);

            foreach (var tile in branch.Tiles)
            {
                var c = tile.Offset;
                var cell = new VisualElement();
                cell.AddToClassList("branch-shape-preview__cell");
                cell.style.left = (c.X - minX) * 9;
                cell.style.top = (maxY - c.Y) * 9;
                root.Add(cell);
            }
        }

        #endregion

        #region Helpers

        private static string CoordKey(int x, int y) => $"{x}:{y}";

        private static GridCoord InvalidAnchor() => new(int.MinValue, int.MinValue);

        private static bool IsValidAnchor(GridCoord a) => a.X != int.MinValue;

        private static int NormalizeRotation(int rotationQuarterTurns)
        {
            var r = rotationQuarterTurns % 4;
            return r < 0 ? r + 4 : r;
        }

        #endregion

        public override void Dispose()
        {
            CancelBranchDrag();
            FinishSeedDrag();
            HideRemoveConfirm();
            Root?.UnregisterCallback<PointerMoveEvent>(OnGlobalPointerMove);
            Root?.UnregisterCallback<PointerUpEvent>(OnGlobalPointerUp);
        }
    }
}
