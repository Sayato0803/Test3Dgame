using System.Collections;
using UnityEngine;

namespace GridBattle
{
    /// <summary>Tia sét răng cưa từ A tới B: nháy đổi hình vài lần rồi tự huỷ. Màu / độ rộng / material chỉnh trên LineRenderer của prefab.</summary>
    [RequireComponent(typeof(LineRenderer))]
    public class LightningBolt : MonoBehaviour
    {
        [SerializeField] private int _segments = 12;
        [Tooltip("Độ lệch răng cưa tối đa (mét).")]
        [SerializeField] private float _jitter = 0.35f;
        [SerializeField] private int _flickers = 5;
        [SerializeField] private float _flickerInterval = 0.05f;

        public void Fire(Vector3 from, Vector3 to) => StartCoroutine(Run(from, to));

        private IEnumerator Run(Vector3 from, Vector3 to)
        {
            var line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = _segments + 1;
            for (int f = 0; f < _flickers; f++)
            {
                for (int i = 0; i <= _segments; i++)
                {
                    float t = i / (float)_segments;
                    // Hai đầu cố định, giữa lệch nhiều nhất.
                    Vector3 offset = i == 0 || i == _segments ? Vector3.zero : Random.insideUnitSphere * _jitter * Mathf.Sin(t * Mathf.PI);
                    line.SetPosition(i, Vector3.Lerp(from, to, t) + offset);
                }
                yield return new WaitForSeconds(_flickerInterval);
            }
            Destroy(gameObject);
        }
    }
}
