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
