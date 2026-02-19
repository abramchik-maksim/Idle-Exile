using System;
using System.Collections.Generic;
using MessagePipe;
using VContainer.Unity;
using Game.Application.Debug;
using Game.Domain.DTOs.Debug;
using Game.Presentation.UI.Cheats;
using UnityEngine;

namespace Game.Presentation.UI.Presenters
{
    public sealed class CheatsPresenter : IStartable, IDisposable
    {
        private readonly CheatsView _cheatsView;
        private readonly SendTestMessageUseCase _sendTestUC;
        private readonly ISubscriber<TestMessageDTO> _testMessageSub;

        private readonly List<IDisposable> _subscriptions = new();
        private int _counter;

        public CheatsPresenter(
            CheatsView cheatsView,
            SendTestMessageUseCase sendTestUC,
            ISubscriber<TestMessageDTO> testMessageSub)
        {
            _cheatsView = cheatsView;
            _sendTestUC = sendTestUC;
            _testMessageSub = testMessageSub;
        }

        public void Start()
        {
            _cheatsView.OnSendTestClicked += HandleSendTest;

            _subscriptions.Add(
                _testMessageSub.Subscribe(dto =>
                {
                    Debug.Log($"[CheatsPresenter] Received TestMessageDTO: {dto.Message}");
                    _cheatsView.SetFeedback($"Received: {dto.Message}");
                }));

            Debug.Log("[CheatsPresenter] Initialized and listening.");
        }

        private void HandleSendTest()
        {
            _counter++;
            string msg = $"Test #{_counter} at {DateTime.Now:HH:mm:ss}";
            Debug.Log($"[CheatsPresenter] Publishing: {msg}");
            _sendTestUC.Execute(msg);
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();
            _subscriptions.Clear();
        }
    }
}
