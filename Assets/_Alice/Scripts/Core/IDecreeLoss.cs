using System;
using UnityEngine;

namespace Alice.Core
{
    /// <summary>
    /// The one lost purse of GDD section 4: the decrees Alice carried when she fell, left at her last safe
    /// footing. The world shows it; Interact next to it gives it back.
    /// </summary>
    public interface IDecreeLoss
    {
        bool HasLoss { get; }
        int LossAmount { get; }
        string LossRoomId { get; }
        Vector2 LossPosition { get; }

        event Action LossChanged;

        void RecoverLoss();
    }
}
