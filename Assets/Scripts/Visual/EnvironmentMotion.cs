using DG.Tweening;
using UnityEngine;

namespace GridBattle
{
    /// <summary>Mây trôi chậm theo trục x, ra khỏi vùng thì quay lại phía bên kia.</summary>
    public class CloudDrift : MonoBehaviour
    {
        public float Speed = 0.6f;
        public float Range = 45f;
        private float _bobSeed;
        private float _baseY;

        private void Start()
        {
            _bobSeed = Random.value * 10f;
            _baseY = transform.position.y;
        }

        private void Update()
        {
            Vector3 p = transform.position;
            p.x += Speed * Time.deltaTime;
            if (p.x > Range) p.x = -Range;
            p.y = _baseY + Mathf.Sin(Time.time * 0.4f + _bobSeed) * 0.3f;
            transform.position = p;
        }
    }

    /// <summary>
    /// Thời tiết trong trận (gắn vào Directional Light): mỗi lượt mới nắng chuyển dần sáng -> trưa -> chiều -> hoàng hôn rồi lặp,
    /// và vài khối "mây bóng" vô hình (chỉ đổ bóng) trôi ngang qua sân.
    /// </summary>
    public class SkyCycle : MonoBehaviour
    {
        private static readonly (Color color, float intensity, float pitch)[] Presets =
        {
            (new Color(1f, 0.86f, 0.7f), 1.0f, 30f),   // sáng sớm
            (new Color(1f, 0.97f, 0.9f), 1.2f, 55f),   // trưa
            (new Color(1f, 0.82f, 0.55f), 1.1f, 38f),  // chiều vàng
            (new Color(1f, 0.68f, 0.6f), 0.9f, 22f),   // hoàng hôn
        };

        private Light _sun;
        private Phases _phases;
        private Player _lastPlayer;
        private int _preset = 1;

        public void Bind(Phases phases)
        {
            _phases = phases;
            _sun = GetComponent<Light>();
            phases.StateChanged += OnState;
            Apply(0f);
            for (int i = 0; i < 4; i++) SpawnShadowCloud(i);
        }

        private void OnDestroy()
        {
            if (_phases != null) _phases.StateChanged -= OnState;
        }

        private void OnState()
        {
            if (_phases.CurrentPlayer == _lastPlayer) return;
            if (_lastPlayer != null) _preset = (_preset + 1) % Presets.Length;
            _lastPlayer = _phases.CurrentPlayer;
            Apply(2.5f);
        }

        private void Apply(float duration)
        {
            var p = Presets[_preset];
            _sun.DOKill();
            _sun.DOColor(p.color, duration);
            _sun.DOIntensity(p.intensity, duration);
            Vector3 e = transform.eulerAngles;
            transform.DORotate(new Vector3(p.pitch, e.y, e.z), duration).SetEase(Ease.InOutSine);
        }

        /// <summary>Khối dẹt chỉ đổ bóng (không hiện), trôi chậm qua sân như bóng mây.</summary>
        private static void SpawnShadowCloud(int i)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(go.GetComponent<Collider>());
            go.name = "CloudShadow";
            var r = go.GetComponent<Renderer>();
            // Material built-in mặc định của primitive không có pass đổ bóng của URP -> mượn material của mây thật.
            GameObject sky = GameObject.Find("Clouds");
            Renderer cloud = sky != null ? sky.GetComponentInChildren<Renderer>() : null;
            if (cloud != null) r.sharedMaterial = cloud.sharedMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            r.receiveShadows = false;
            go.transform.position = new Vector3(-40f + i * 22f + Random.Range(-5f, 5f), 9f, Random.Range(-14f, 14f));
            go.transform.localScale = new Vector3(Random.Range(7f, 11f), 0.6f, Random.Range(5f, 8f));
            var drift = go.AddComponent<CloudDrift>();
            drift.Speed = Random.Range(0.8f, 1.4f);
            drift.Range = 45f;
        }
    }

    /// <summary>Cờ phất phới: lắc quanh cán + co giãn nhẹ.</summary>
    public class FlagWave : MonoBehaviour
    {
        public float Amplitude = 14f;
        public float Frequency = 3f;
        private float _seed;
        private Quaternion _base;

        private void Start()
        {
            _seed = Random.value * 10f;
            _base = transform.localRotation;
        }

        private void Update()
        {
            float t = Time.time * Frequency + _seed;
            transform.localRotation = _base * Quaternion.Euler(0f, Mathf.Sin(t) * Amplitude, Mathf.Sin(t * 1.7f) * Amplitude * 0.3f);
        }
    }

    /// <summary>Mặt nước dập dềnh lên xuống.</summary>
    public class WaterBob : MonoBehaviour
    {
        public float Amplitude = 0.06f;
        public float Speed = 0.8f;
        private float _baseY;

        private void Start() => _baseY = transform.position.y;

        private void Update()
        {
            Vector3 p = transform.position;
            p.y = _baseY + Mathf.Sin(Time.time * Speed) * Amplitude;
            transform.position = p;
        }
    }

    /// <summary>Cây/bụi đung đưa nhẹ trong gió.</summary>
    public class Sway : MonoBehaviour
    {
        public float Amplitude = 2.5f;
        private float _seed;
        private Quaternion _base;

        private void Start()
        {
            _seed = Random.value * 10f;
            _base = transform.localRotation;
        }

        private void Update()
        {
            float t = Time.time * 1.3f + _seed;
            transform.localRotation = _base * Quaternion.Euler(Mathf.Sin(t) * Amplitude, 0f, Mathf.Sin(t * 0.8f) * Amplitude);
        }
    }
}
