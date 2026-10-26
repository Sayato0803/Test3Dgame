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
        /// <summary>Lá phép trên tay (Phases rút / dùng).</summary>
        public List<CardData> Hand { get; } = new();
        /// <summary>Số lá Knight / Mage trên tay (rút từ bộ bài; mỗi lá đặt được 1 quân).</summary>
        public int KnightCards { get; set; }
        public int MageCards { get; set; }

        /// <summary>Tối đa số lá trên tay (đếm bằng Phases.HandCount: lá phép + Knight + Mage + Scout; không tính King lúc Deploy).</summary>
        public const int MaxHand = 8;
    }
}
