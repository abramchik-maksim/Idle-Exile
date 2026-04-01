using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Progression.TreeTalents;
using Game.Domain.DTOs.Progression;
using Game.Domain.Progression.TreeTalents;
using Game.Presentation.UI.MainScreen;
using Game.Presentation.UI.Services;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class TreeTalentsPresenter : IStartable, ITickable, IDisposable
    {
        private readonly TreeTalentsTabView _view;
        private readonly IGameStateProvider _state;
        private readonly RunBranchGrowthCycleUseCase _runGrowth;
        private readonly ApplyTreeBranchOperationUseCase _applyOperation;
        private readonly TreeUnlockProfileService _unlockProfile;
        private readonly ITreeTalentsConfigProvider _config;
        private readonly ITreeTalentsInputReader _input;
        private readonly IPublisher<BranchGrowthCompletedDTO> _growthCompletedPub;
        private readonly IPublisher<BranchPlacedDTO> _branchPlacedPub;
        private readonly IPublisher<BranchRemovedDTO> _branchRemovedPub;
        private readonly IPublisher<TreeAlliancesChangedDTO> _alliancesChangedPub;
        private readonly IPublisher<TreeTalentsChangedDTO> _treeChangedPub;
        private readonly ISubscriber<TreeTalentsChangedDTO> _treeChangedSub;
        private readonly List<IDisposable> _subscriptions = new();
        private bool _isGrowthInProgress;
        private float _growthRemainingSeconds;
        private float _growthTotalSeconds;
        private IReadOnlyList<SeedType> _pendingGrowthSeeds;

        public TreeTalentsPresenter(
            TreeTalentsTabView view,
            IGameStateProvider state,
            RunBranchGrowthCycleUseCase runGrowth,
            ApplyTreeBranchOperationUseCase applyOperation,
            TreeUnlockProfileService unlockProfile,
            ITreeTalentsConfigProvider config,
            ITreeTalentsInputReader input,
            IPublisher<BranchGrowthCompletedDTO> growthCompletedPub,
            IPublisher<BranchPlacedDTO> branchPlacedPub,
            IPublisher<BranchRemovedDTO> branchRemovedPub,
            IPublisher<TreeAlliancesChangedDTO> alliancesChangedPub,
            IPublisher<TreeTalentsChangedDTO> treeChangedPub,
            ISubscriber<TreeTalentsChangedDTO> treeChangedSub)
        {
            _view = view;
            _state = state;
            _runGrowth = runGrowth;
            _applyOperation = applyOperation;
            _unlockProfile = unlockProfile;
            _config = config;
            _input = input;
            _growthCompletedPub = growthCompletedPub;
            _branchPlacedPub = branchPlacedPub;
            _branchRemovedPub = branchRemovedPub;
            _alliancesChangedPub = alliancesChangedPub;
            _treeChangedPub = treeChangedPub;
            _treeChangedSub = treeChangedSub;
        }

        public void Start()
        {
            _view.OnGrowBranchRequested += HandleGrowBranchRequested;
            _view.OnPlaceBranchRequested += HandlePlaceBranchRequested;
            _view.OnRemoveBranchRequested += HandleRemoveBranchRequested;
            _subscriptions.Add(_treeChangedSub.Subscribe(_ => Refresh()));
            _view.SetGrowthState(false, 0f, 0f);
            _view.SetAllianceThresholds(_config.GetAllianceThresholds());
            Refresh();
        }

        public void Tick()
        {
            _input.Update();

            if (_view.IsVisible && _view.IsDraggingBranch)
            {
                var rotateSteps = _input.ConsumeRotationSteps();
                if (rotateSteps != 0)
                    _view.RotateDraggedBranch(rotateSteps);

                if (_input.ConsumeCancelDrag())
                    _view.CancelDragFromInput();
            }
            else
            {
                _input.ConsumeRotationSteps();
                _input.ConsumeCancelDrag();
            }

            if (!_isGrowthInProgress) return;
            _growthRemainingSeconds = Mathf.Max(0f, _growthRemainingSeconds - Time.deltaTime);
            _view.SetGrowthState(true, _growthRemainingSeconds, _growthTotalSeconds);
            if (_growthRemainingSeconds > 0f) return;

            CompleteGrowth();
        }

        private void HandleGrowBranchRequested(IReadOnlyList<SeedType> seeds)
        {
            if (_isGrowthInProgress || seeds == null || seeds.Count != 3) return;

            _pendingGrowthSeeds = new List<SeedType>(seeds);
            _growthTotalSeconds = Mathf.Max(1f, _config.GrowthDurationSeconds);
            _growthRemainingSeconds = _growthTotalSeconds;
            _isGrowthInProgress = true;
            _view.SetGrowthState(true, _growthRemainingSeconds, _growthTotalSeconds);
        }

        private void CompleteGrowth()
        {
            _isGrowthInProgress = false;
            var seeds = _pendingGrowthSeeds;
            _pendingGrowthSeeds = null;
            _view.SetGrowthState(false, 0f, _growthTotalSeconds);
            _view.ClearSelectedSeeds();

            if (seeds == null || seeds.Count != 3) return;
            var result = _runGrowth.Execute(_state.TreeTalents, seeds, _state.TreeTalents.Level);
            if (!result.Success || result.Branch == null) return;

            _growthCompletedPub.Publish(new BranchGrowthCompletedDTO(result.Branch.Id));
            _treeChangedPub.Publish(new TreeTalentsChangedDTO());
        }

        private void HandlePlaceBranchRequested(string branchId, int x, int y, int rotationQuarterTurns)
        {
            var result = _applyOperation.Execute(_state.TreeTalents,
                new TreeBranchOperation(TreeBranchOperationType.Place, branchId, x, y, rotationQuarterTurns));
            if (!result.Success) return;

            _branchPlacedPub.Publish(new BranchPlacedDTO(branchId));
            _alliancesChangedPub.Publish(new TreeAlliancesChangedDTO());
            _treeChangedPub.Publish(new TreeTalentsChangedDTO());
        }

        private void HandleRemoveBranchRequested(string branchId)
        {
            if (string.IsNullOrEmpty(branchId)) return;
            var result = _applyOperation.Execute(_state.TreeTalents,
                new TreeBranchOperation(TreeBranchOperationType.Remove, branchId));
            if (!result.Success) return;

            _branchRemovedPub.Publish(new BranchRemovedDTO(branchId));
            _alliancesChangedPub.Publish(new TreeAlliancesChangedDTO());
            _treeChangedPub.Publish(new TreeTalentsChangedDTO());
        }

        private void Refresh()
        {
            var halfWidths = _unlockProfile.GetHalfWidthsForLevel(_state.TreeTalents.Level);
            _view.RenderTreeState(_state.TreeTalents, halfWidths);
        }

        public void Dispose()
        {
            _view.OnGrowBranchRequested -= HandleGrowBranchRequested;
            _view.OnPlaceBranchRequested -= HandlePlaceBranchRequested;
            _view.OnRemoveBranchRequested -= HandleRemoveBranchRequested;
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
