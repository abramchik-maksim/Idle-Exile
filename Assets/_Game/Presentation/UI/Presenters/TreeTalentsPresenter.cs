using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Ports;
using Game.Application.Progression.TreeTalents;
using Game.Domain.DTOs.Progression;
using Game.Presentation.UI.MainScreen;

namespace Game.Presentation.UI.Presenters
{
    public sealed class TreeTalentsPresenter : IStartable, IDisposable
    {
        private readonly TreeTalentsTabView _view;
        private readonly IGameStateProvider _state;
        private readonly RunBranchGrowthCycleUseCase _runGrowth;
        private readonly ApplyTreeBranchOperationUseCase _applyOperation;
        private readonly TreeUnlockProfileService _unlockProfile;
        private readonly IPublisher<BranchGrowthCompletedDTO> _growthCompletedPub;
        private readonly IPublisher<BranchPlacedDTO> _branchPlacedPub;
        private readonly IPublisher<TreeAlliancesChangedDTO> _alliancesChangedPub;
        private readonly IPublisher<TreeTalentsChangedDTO> _treeChangedPub;
        private readonly ISubscriber<TreeTalentsChangedDTO> _treeChangedSub;
        private readonly List<IDisposable> _subscriptions = new();

        public TreeTalentsPresenter(
            TreeTalentsTabView view,
            IGameStateProvider state,
            RunBranchGrowthCycleUseCase runGrowth,
            ApplyTreeBranchOperationUseCase applyOperation,
            TreeUnlockProfileService unlockProfile,
            IPublisher<BranchGrowthCompletedDTO> growthCompletedPub,
            IPublisher<BranchPlacedDTO> branchPlacedPub,
            IPublisher<TreeAlliancesChangedDTO> alliancesChangedPub,
            IPublisher<TreeTalentsChangedDTO> treeChangedPub,
            ISubscriber<TreeTalentsChangedDTO> treeChangedSub)
        {
            _view = view;
            _state = state;
            _runGrowth = runGrowth;
            _applyOperation = applyOperation;
            _unlockProfile = unlockProfile;
            _growthCompletedPub = growthCompletedPub;
            _branchPlacedPub = branchPlacedPub;
            _alliancesChangedPub = alliancesChangedPub;
            _treeChangedPub = treeChangedPub;
            _treeChangedSub = treeChangedSub;
        }

        public void Start()
        {
            _view.OnGrowBranchRequested += HandleGrowBranchRequested;
            _view.OnPlaceBranchRequested += HandlePlaceBranchRequested;
            _subscriptions.Add(_treeChangedSub.Subscribe(_ => Refresh()));
            Refresh();
        }

        private void HandleGrowBranchRequested(
            Domain.Progression.TreeTalents.SeedType a,
            Domain.Progression.TreeTalents.SeedType b,
            Domain.Progression.TreeTalents.SeedType c)
        {
            var result = _runGrowth.Execute(_state.TreeTalents, new[] { a, b, c }, _state.TreeTalents.Level);
            if (!result.Success || result.Branch == null) return;

            _growthCompletedPub.Publish(new BranchGrowthCompletedDTO(result.Branch.Id));
            _treeChangedPub.Publish(new TreeTalentsChangedDTO());
        }

        private void HandlePlaceBranchRequested(string branchId, int x, int y)
        {
            var result = _applyOperation.Execute(_state.TreeTalents,
                new TreeBranchOperation(TreeBranchOperationType.Place, branchId, x, y));
            if (!result.Success) return;

            _branchPlacedPub.Publish(new BranchPlacedDTO(branchId));
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
            foreach (var sub in _subscriptions) sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
