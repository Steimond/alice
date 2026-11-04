using UnityEngine;

namespace Alice.Core
{
    /// <summary>
    /// An enemy that can be executed while it is open (GDD section 4). Each enemy type names its own
    /// execution; Alice keeps the matching frames and timing under that ID, so both sides play in sync.
    /// </summary>
    public interface IExecutable
    {
        bool CanBeExecuted { get; }

        /// <summary>Stable ID of this enemy type's execution, e.g. card_tear.</summary>
        string ExecutionId { get; }

        Vector2 Position { get; }

        /// <summary>Starts the enemy's side: it dies by execution and plays that death from now.</summary>
        void BeginExecution(GameObject by, int fromSide);
    }
}
