using System.Collections.Generic;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// 1 người chơi (gắn lên GameObject "P1"/"P2"). Giữ bàn của mình, đối thủ, camera và các quân mình sở hữu.
    /// Attacker của mình đứng trên bàn đối thủ nhưng vẫn nằm trong list Attackers của mình.
    /// </summary>
    public class Player : MonoBehaviour
    {
        [SerializeField] private string _displayName = "P1";
        [SerializeField] private GridSystem _board;
        [SerializeField] private Player _opponent;
        [Tooltip("Camera nhìn bàn nhà (Deploy, Setup). Số thứ tự trong mảng Camera của GridPlacer.")]
        [SerializeField] private int _cameraIndex;
        [Tooltip("Camera nhìn sang bàn đối thủ từ phía mình (Attack, End).")]
        [SerializeField] private int _attackCameraIndex;

        public string DisplayName => _displayName;
        public GridSystem Board => _board;
        public Player Opponent => _opponent;
        public int CameraIndex => _cameraIndex;
        public int AttackCameraIndex => _attackCameraIndex;

        public List<GridUnit> Kings { get; } = new();
        public List<GridUnit> Attackers { get; } = new();

        public int CountAttackers(UnitKind kind)
        {
            int n = 0;
            foreach (GridUnit a in Attackers)
                if (a.Kind == kind) n++;
            return n;
        }
    }
}
