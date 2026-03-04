using MessagePipe;
using UnityEngine;
using Game.Application.Ports;
using Game.Domain.Combat;
using Game.Domain.DTOs.Combat;
using Game.Presentation.Combat.Rendering;
using Game.Presentation.Combat.Systems;

namespace Game.Presentation.Combat
{
    public sealed class DamageEventProcessor
    {
        private readonly DamageNumberPool _damagePool;
        private readonly ICombatDisplaySettings _displaySettings;
        private readonly IPublisher<DamageDealtDTO> _damageDealtPub;

        private DamageEventBufferSystem _damageBufferSystem;

        public DamageEventProcessor(
            DamageNumberPool damagePool,
            ICombatDisplaySettings displaySettings,
            IPublisher<DamageDealtDTO> damageDealtPub)
        {
            _damagePool = damagePool;
            _displaySettings = displaySettings;
            _damageDealtPub = damageDealtPub;
        }

        public void Initialize(DamageEventBufferSystem bufferSystem)
        {
            _damageBufferSystem = bufferSystem;
        }

        public void ProcessFrame()
        {
            if (_damageBufferSystem == null) return;

            foreach (var evt in _damageBufferSystem.FrameEvents)
            {
                if (_displaySettings.ShowDamageNumbers)
                {
                    _damagePool.Show(
                        new Vector3(evt.WorldX, evt.WorldY, 0f),
                        evt.Amount,
                        evt.IsCritical
                    );
                }

                _damageDealtPub.Publish(new DamageDealtDTO(
                    new DamageResult(evt.Amount, evt.Amount, evt.IsCritical, DamageType.Physical),
                    true,
                    evt.WorldX,
                    evt.WorldY
                ));
            }
        }
    }
}
