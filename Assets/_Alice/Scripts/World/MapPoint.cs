using UnityEngine;

namespace Alice.World
{
    /// <summary>
    /// A point the level designer wants on the map that no component gives by itself: a gate drawn only by
    /// geometry (a fan ledge), a lift, later an NPC. The map atlas bake collects it; it does nothing in the game.
    /// </summary>
    public sealed class MapPoint : MonoBehaviour
    {
        [SerializeField] private MapIconKind _kind = MapIconKind.GateFan;
        [Tooltip("Optional world flag; when raised the icon shows its done state (a door opened, a gate passed).")]
        [SerializeField] private string _flag = string.Empty;

        public MapIconKind Kind
        {
            get => _kind;
            set => _kind = value;
        }

        public string Flag
        {
            get => _flag;
            set => _flag = value;
        }
    }
}
