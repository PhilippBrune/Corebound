using System;
using UnityEngine;

namespace Corebound.Input
{
    public interface IInputReader
    {
        Vector2 Move { get; }

        Vector2 Aim { get; }

        bool IsPrimaryFireHeld { get; }

        bool IsInteractHeld { get; }

        bool IsUsingGamepad { get; }

        event Action<Vector2> NavigatePerformed;

        event Action ConfirmPressed;

        event Action CancelPressed;

        event Action InteractPressed;

        event Action PausePressed;

        event Action PreviousPressed;

        event Action NextPressed;

        event Action PreviousTowerTypePressed;

        event Action NextTowerTypePressed;

        void ApplyContext(InputContextData context);

        void ResetInputState();
    }
}