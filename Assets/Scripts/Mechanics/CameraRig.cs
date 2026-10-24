using DG.Tweening;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// 1 camera chính bay mượt giữa các điểm neo (Cam1..Cam5 trong scene, được tắt đi và chỉ dùng làm vị trí/góc/FOV).
    /// Đường bay là cung Bezier vòng qua phía trên tâm sân, nên đổi sang bàn đối diện sẽ thấy camera lượn quanh sân.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        [SerializeField] private Camera[] _anchors = new Camera[5];
        [Tooltip("Tâm sân: đường bay vòng quanh điểm này.")]
        [SerializeField] private Vector3 _orbitCenter = Vector3.zero;
        [SerializeField] private float _duration = 1.1f;
        [SerializeField] private float _arcHeight = 3f;

        public Camera Camera => _camera;

        /// <summary>Rung camera (va chạm mạnh). Bỏ qua khi đang bay.</summary>
        public void Shake(float strength = 0.25f, float duration = 0.3f)
        {
            if (IsMoving) return;
            _camera.transform.DOKill(true);
            _camera.transform.DOShakePosition(duration, strength, 25, 90f, false, true);
        }
        public bool IsMoving => _tween != null && _tween.IsActive() && _tween.IsPlaying();
        public int CurrentIndex { get; private set; } = -1;

        private Tween _tween;

        /// <summary>Rig của scene hiện tại (để hiệu ứng va chạm rung camera).</summary>
        public static CameraRig Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            if (_camera == null) _camera = Camera.main;
            foreach (Camera a in _anchors)
                if (a != null && a != _camera) a.gameObject.SetActive(false);
        }

        private bool Valid(int index) => index >= 0 && index < _anchors.Length && _anchors[index] != null;

        public void SnapTo(int index)
        {
            if (!Valid(index)) return;
            _tween?.Kill();
            Transform a = _anchors[index].transform;
            _camera.transform.SetPositionAndRotation(a.position, a.rotation);
            _camera.fieldOfView = _anchors[index].fieldOfView;
            CurrentIndex = index;
        }

        public void MoveTo(int index)
        {
            if (!Valid(index)) return;
            if (CurrentIndex < 0) { SnapTo(index); return; }
            if (index == CurrentIndex && !IsMoving)
            {
                // Cùng góc: chỉ trả FOV về mặc định (sau khi người chơi cuộn zoom).
                _camera.DOFieldOfView(_anchors[index].fieldOfView, 0.3f);
                return;
            }

            _tween?.Kill();
            _camera.transform.DOKill(true); // dừng rung camera (nếu có) trước khi bay
            CurrentIndex = index;
            Sfx.Play(SfxId.CameraWhoosh, 1f, 0.05f);

            Transform t = _camera.transform;
            Vector3 fromPos = t.position;
            Quaternion fromRot = t.rotation;
            float fromFov = _camera.fieldOfView;
            Transform a = _anchors[index].transform;
            Vector3 toPos = a.position;
            Quaternion toRot = a.rotation;
            float toFov = _anchors[index].fieldOfView;
            Vector3 control = ArcControlPoint(fromPos, toPos);

            float duration = _duration / Mathf.Max(0.1f, GameSettings.CameraSpeed);
            _tween = DOVirtual.Float(0f, 1f, duration, k =>
            {
                // Bezier bậc 2: from -> control -> to.
                float u = 1f - k;
                t.position = u * u * fromPos + 2f * u * k * control + k * k * toPos;
                t.rotation = Quaternion.Slerp(fromRot, toRot, k);
                _camera.fieldOfView = Mathf.Lerp(fromFov, toFov, k);
            }).SetEase(Ease.InOutSine);
        }

        /// <summary>
        /// Điểm điều khiển của cung bay: trung điểm nâng lên cao. Nếu 2 điểm nằm 2 phía đối diện của sân
        /// (trung điểm gần tâm), đẩy điểm điều khiển ra cạnh sân để camera lượn vòng thay vì chui qua tâm.
        /// </summary>
        private Vector3 ArcControlPoint(Vector3 from, Vector3 to)
        {
            Vector3 mid = (from + to) * 0.5f;
            Vector3 flatMid = new(mid.x - _orbitCenter.x, 0f, mid.z - _orbitCenter.z);
            Vector3 flatFrom = new(from.x - _orbitCenter.x, 0f, from.z - _orbitCenter.z);
            float radius = flatFrom.magnitude;

            if (radius > 0.01f && flatMid.magnitude < radius * 0.5f)
            {
                Vector3 dir = to - from;
                Vector3 side = Vector3.Cross(Vector3.up, new Vector3(dir.x, 0f, dir.z)).normalized;
                if (side == Vector3.zero) side = Vector3.right;
                // Bezier bậc 2 chỉ đi được nửa đường tới điểm điều khiển -> nhân 2 để đỉnh cung chạm bán kính.
                flatMid = side * radius * 2f;
            }

            float height = Mathf.Max(from.y, to.y) + _arcHeight;
            return new Vector3(_orbitCenter.x + flatMid.x, height, _orbitCenter.z + flatMid.z);
        }
    }
}
