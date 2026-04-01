using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Game.Presentation.UI.Services
{
    public sealed class TreeTalentsInputReader : ITreeTalentsInputReader
    {
        private int _pendingRotationSteps;
        private bool _pendingCancel;

        public void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.rKey.wasPressedThisFrame)
                    _pendingRotationSteps += 1;

                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                    _pendingCancel = true;
            }

            if (Mouse.current != null)
            {
                var wheelY = Mouse.current.scroll.ReadValue().y;
                if (wheelY > 0.01f)
                    _pendingRotationSteps += 1;
                else if (wheelY < -0.01f)
                    _pendingRotationSteps -= 1;
            }
#endif
        }

        public int ConsumeRotationSteps()
        {
            var steps = _pendingRotationSteps;
            _pendingRotationSteps = 0;
            return steps;
        }

        public bool ConsumeCancelDrag()
        {
            var value = _pendingCancel;
            _pendingCancel = false;
            return value;
        }
    }
}
