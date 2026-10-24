using DG.Tweening;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Hiệu ứng hạt cartoon dựng bằng code: bụi "puff" khi chạm đất, sao bắn khi trúng đòn,
    /// lấp lánh khi King bay mất hút, vòng sao chóng mặt trên đầu.
    /// </summary>
    public static class CartoonFx
    {
        /// <summary>Vòng khói tròn toả ra sát mặt đất.</summary>
        public static void Puff(Vector3 pos, float size = 1f, int count = 14)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return;
            ParticleSystem ps = Create("Puff", pos, a.PuffMaterial);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * size, 3f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f * size, 0.5f * size);
            main.startColor = new Color(1f, 0.98f, 0.94f, 0.95f);
            main.gravityModifier = -0.05f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f * size;
            shape.rotation = new Vector3(90f, 0f, 0f);
            shape.radiusThickness = 0f;
            var drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 4f;
            SizeGrowFade(ps);
            ps.Emit(count);
            Object.Destroy(ps.gameObject, 1.2f);
        }

        /// <summary>Ngôi sao bắn ra tứ phía (trúng đòn, bị đè bẹp).</summary>
        public static void StarBurst(Vector3 pos, Color color, int count = 10, float speed = 4f)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.StarMaterial == null) return;
            ParticleSystem ps = Create("StarBurst", pos, a.StarMaterial);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = 0.6f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1f), new Keyframe(0.7f, 1f), new Keyframe(1, 0f)));
            ps.Emit(count);
            Object.Destroy(ps.gameObject, 1.2f);
        }

        /// <summary>Đám khói tròn "bụp" kiểu cartoon (King ẩn mình): cục khói to phồng ra rồi tan.</summary>
        public static void SmokePoof(Vector3 pos, float height)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return;
            ParticleSystem ps = Create("SmokePoof", pos, a.PuffMaterial);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.75f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.5f * height, 0.85f * height);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.92f, 0.9f, 0.98f, 0.95f), new Color(0.78f, 0.76f, 0.86f, 0.95f));
            main.gravityModifier = -0.15f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f * height;
            var drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 3f;
            SizeGrowFade(ps);
            ps.Emit(16);
            Object.Destroy(ps.gameObject, 1.2f);
            StarBurst(pos, new Color(1f, 1f, 1f), 6, 2.5f);
        }

        /// <summary>Vệt khói xoắn bay thẳng lên khi King xoay người biến mất.</summary>
        public static void SmokeTrail(Vector3 feet, float height)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return;
            ParticleSystem ps = Create("SmokeTrail", feet, a.PuffMaterial);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f * height, 0.5f * height);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.95f, 0.93f, 1f, 0.95f), new Color(0.75f, 0.73f, 0.85f, 0.9f));
            main.gravityModifier = -0.1f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.15f * height;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.orbitalY = new ParticleSystem.MinMaxCurve(3f);          // xoắn ốc theo chiều xoay của King
            var drag = ps.limitVelocityOverLifetime;
            drag.enabled = true;
            drag.drag = 1.2f;
            SizeGrowFade(ps);
            ps.Emit(22);
            Object.Destroy(ps.gameObject, 1.6f);
            Puff(feet + Vector3.up * 0.05f, 0.9f, 12);
            StarBurst(feet + Vector3.up * height * 0.6f, Color.white, 5, 2f);
        }

        /// <summary>Làn khói mỏng bốc lên liên tục (đánh dấu ô King đang ẩn cho chủ quân).</summary>
        public static ParticleSystem CreateWisp(Transform parent)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return null;
            ParticleSystem ps = Create("HiddenWisp", parent.position, a.PuffMaterial);
            ps.transform.SetParent(parent, true);
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.4f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.88f, 1f, 0.55f), new Color(0.75f, 0.72f, 0.9f, 0.45f));
            main.gravityModifier = -0.04f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.22f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 5f;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(0.35f);
            vel.orbitalY = new ParticleSystem.MinMaxCurve(1.2f);
            SizeGrowFade(ps);
            ps.Play();
            return ps;
        }

        /// <summary>Tia phép: quả cầu sáng xanh-tím bay vòng cung từ 'from' tới 'to', kéo đuôi lấp lánh, nổ sao khi trúng.</summary>
        public static void MagicBolt(Vector3 from, Vector3 to, float delay, float travel)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.StarSprite == null) return;
            DOVirtual.DelayedCall(delay, () =>
            {
                var go = new GameObject("MagicBolt");
                go.transform.position = from;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = a.StarSprite;
                sr.color = new Color(0.45f, 1f, 1f);
                go.AddComponent<Billboard>();
                go.transform.localScale = Vector3.one * 0.6f;
                if (a.StarMaterial != null)
                {
                    ParticleSystem trail = Create("BoltTrail", from, a.StarMaterial);
                    trail.transform.SetParent(go.transform, true);
                    var main = trail.main;
                    main.loop = true;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
                    main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
                    main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 0.4f, 1f), new Color(0.4f, 1f, 1f));
                    var emission = trail.emission;
                    emission.enabled = true;
                    emission.rateOverDistance = 30f;
                    var shape = trail.shape;
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radius = 0.05f;
                    trail.Play();
                }
                GridBattle.Sfx.Play(GridBattle.SfxId.Zap, 0.8f, 0.1f);
                Sequence s = DOTween.Sequence().SetTarget(go.transform);
                s.Append(go.transform.DOJump(to, 0.7f, 1, travel).SetEase(Ease.Linear));
                s.Join(go.transform.DOScale(0.9f, travel));
                s.OnComplete(() =>
                {
                    StarBurst(to, new Color(0.45f, 0.95f, 1f), 12, 4f);
                    StarBurst(to, new Color(0.65f, 0.4f, 1f), 8, 3f);
                    Comic("zap", to + Vector3.up * 0.6f);
                    Object.Destroy(go);
                });
            });
        }

        /// <summary>Vệt kiếm chém: cung trắng-vàng quét chéo qua King rồi tan, kèm tiếng "vút - keng".</summary>
        public static void Slash(Vector3 center, Vector3 dir, float delay)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return;
            DOVirtual.DelayedCall(Mathf.Max(0f, delay), () =>
            {
                Camera cam = Camera.main;
                Vector3 fwd = cam != null ? cam.transform.forward : Vector3.forward;
                Vector3 right = cam != null ? cam.transform.right : Vector3.right;
                Vector3 up = cam != null ? cam.transform.up : Vector3.up;
                // Cung chéo từ trên-trái xuống dưới-phải (theo màn hình), nằm trong mặt phẳng vuông góc camera.
                var go = new GameObject("Slash");
                var lr = go.AddComponent<LineRenderer>();
                lr.sharedMaterial = a.PuffMaterial;
                lr.useWorldSpace = true;
                lr.textureMode = LineTextureMode.Stretch;
                lr.numCapVertices = 4;
                const int n = 14;
                lr.positionCount = n;
                float side = Vector3.Dot(dir, right) >= 0f ? 1f : -1f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)(n - 1);
                    float ang = Mathf.Lerp(130f, -50f, t) * Mathf.Deg2Rad;
                    Vector3 p = center + (right * side * Mathf.Cos(ang) + up * Mathf.Sin(ang)) * 0.7f - fwd * 0.3f;
                    lr.SetPosition(i, p);
                }
                var width = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f));
                lr.widthCurve = width;
                lr.widthMultiplier = 0f;
                var grad = new Gradient();
                grad.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.7f), 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
                lr.colorGradient = grad;
                GridBattle.Sfx.Play(GridBattle.SfxId.Slash, 1f, 0.08f);
                Sequence s = DOTween.Sequence().SetTarget(go);
                s.Append(DOTween.To(() => lr.widthMultiplier, w => lr.widthMultiplier = w, 0.6f, 0.05f));
                s.AppendInterval(0.12f);
                s.Append(DOTween.To(() => lr.widthMultiplier, w => lr.widthMultiplier = w, 0f, 0.25f));
                s.OnComplete(() => Object.Destroy(go));
                StarBurst(center, new Color(1f, 0.95f, 0.75f), 8, 4.5f);
                Comic("sweep", center + Vector3.up * 0.6f);
            });
        }

        /// <summary>Tan rã do phép: hạt tím-xanh xoáy bay lên. power 0..1 (nhỏ = rò rỉ, 1 = nổ tung).</summary>
        public static void MagicDissolve(Vector3 center, float height, float power)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.StarMaterial == null) return;
            ParticleSystem ps = Create("MagicDissolve", center, a.StarMaterial);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f + 1.5f * power, 1.5f + 3f * power);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.28f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.35f, 1f), new Color(0.35f, 1f, 1f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.35f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.35f * height;
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.World;
            vel.orbitalY = new ParticleSystem.MinMaxCurve(4f);
            vel.y = new ParticleSystem.MinMaxCurve(1.2f);
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-5f, 5f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.4f), new Keyframe(0.3f, 1.2f), new Keyframe(1, 0f)));
            ps.Emit(Mathf.RoundToInt(20 + 40 * power));
            Object.Destroy(ps.gameObject, 1.8f);
            if (power >= 1f && a.PuffMaterial != null) Puff(center, 1.0f, 10);
        }

        /// <summary>Nước bắn tung toé khi rơi xuống biển.</summary>
        public static void Splash(Vector3 pos)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return;
            ParticleSystem ps = Create("Splash", pos, a.PuffMaterial);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.75f, 0.9f, 1f), Color.white);
            main.gravityModifier = 1.6f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            SizeGrowFade(ps);
            ps.Emit(30);
            Object.Destroy(ps.gameObject, 1.5f);
            Puff(pos + Vector3.up * 0.1f, 1.4f, 16);
        }

        /// <summary>Lấp lánh "ting" nơi King bay mất hút trên trời.</summary>
        public static void Twinkle(Vector3 pos)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.StarSprite == null) return;
            var go = new GameObject("Twinkle");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = a.StarSprite;
            sr.color = new Color(1f, 0.95f, 0.6f);
            go.AddComponent<Billboard>();
            go.transform.localScale = Vector3.zero;
            Sequence s = DOTween.Sequence().SetTarget(go.transform);
            s.Append(go.transform.DOScale(1.6f, 0.15f).SetEase(Ease.OutBack));
            s.Join(go.transform.DORotate(new Vector3(0, 0, 180f), 0.5f, RotateMode.LocalAxisAdd));
            s.Append(go.transform.DOScale(0f, 0.25f).SetEase(Ease.InBack));
            s.OnComplete(() => Object.Destroy(go));
            StarBurst(pos, new Color(1f, 0.9f, 0.4f), 8, 2.5f);
        }

        /// <summary>Chữ tượng thanh kiểu manga (ảnh Resources/GUI/sfx_text/sfx_&lt;word&gt;) bật ra ở 'pos' rồi bay lên mờ dần.</summary>
        public static void Comic(string word, Vector3 pos, float delay = 0f)
        {
            Sprite sprite = Resources.Load<Sprite>("GUI/sfx_text/sfx_" + word);
            if (sprite == null) return;
            DOVirtual.DelayedCall(Mathf.Max(0f, delay), () =>
            {
                Camera cam = Camera.main;
                var go = new GameObject("Comic_" + word);
                go.transform.position = cam != null ? pos - cam.transform.forward * 0.8f : pos; // nhích về phía camera để King không che chữ
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 100;
                go.AddComponent<Billboard>();
                go.transform.localScale = Vector3.zero;
                float size = 1.4f / sprite.bounds.size.x; // rộng ~1.4 đơn vị bất kể kích thước ảnh
                Sequence s = DOTween.Sequence().SetTarget(go.transform);
                s.Append(go.transform.DOScale(size * 1.25f, 0.14f).SetEase(Ease.OutBack));
                s.Append(go.transform.DOScale(size, 0.08f));
                s.AppendInterval(0.35f);
                s.Append(go.transform.DOMove(go.transform.position + Vector3.up * 0.5f, 0.4f).SetEase(Ease.OutQuad));
                s.Join(sr.DOFade(0f, 0.4f));
                s.OnComplete(() => Object.Destroy(go));
            });
        }

        /// <summary>Vòng sao xoay quanh đầu (choáng) trong 'duration' giây.</summary>
        public static void DizzyStars(Transform head, Vector3 localOffset, float duration)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.StarSprite == null || head == null) return;
            var ring = new GameObject("DizzyStars").AddComponent<DizzyRing>();
            ring.Init(head, localOffset, a.StarSprite, duration);
        }

        private static ParticleSystem Create(string name, Vector3 pos, Material mat)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var emission = ps.emission;
            emission.enabled = false;
            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = mat;
            return ps;
        }

        private static void SizeGrowFade(ParticleSystem ps)
        {
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(0.3f, 1.2f), new Keyframe(1, 0.2f)));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(1f, 0), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1) });
            col.color = g;
        }
    }

    /// <summary>Luôn quay mặt về camera.</summary>
    public class Billboard : MonoBehaviour
    {
        private void LateUpdate()
        {
            Camera cam = Camera.main;
            if (cam != null) transform.forward = cam.transform.forward;
        }
    }

    /// <summary>3 ngôi sao xoay vòng trên đầu nhân vật bị choáng.</summary>
    public class DizzyRing : MonoBehaviour
    {
        private Transform _head;
        private Vector3 _offset;
        private Transform[] _stars;
        private float _end;

        public void Init(Transform head, Vector3 offset, Sprite star, float duration)
        {
            _head = head;
            _offset = offset;
            _end = Time.time + duration;
            _stars = new Transform[3];
            for (int i = 0; i < _stars.Length; i++)
            {
                var s = new GameObject("Star").AddComponent<SpriteRenderer>();
                s.sprite = star;
                s.color = new Color(1f, 0.9f, 0.3f);
                s.transform.SetParent(transform, false);
                s.transform.localScale = Vector3.one * 0.35f;
                s.gameObject.AddComponent<Billboard>();
                _stars[i] = s.transform;
            }
            transform.localScale = Vector3.zero;
            transform.DOScale(1f, 0.2f).SetEase(Ease.OutBack);
        }

        private void LateUpdate()
        {
            if (_head == null) { Destroy(gameObject); return; }
            transform.position = _head.position + _offset;
            float t = Time.time * 5f;
            for (int i = 0; i < _stars.Length; i++)
            {
                float a = t + i * Mathf.PI * 2f / _stars.Length;
                _stars[i].localPosition = new Vector3(Mathf.Cos(a) * 0.35f, Mathf.Sin(a * 2f) * 0.05f, Mathf.Sin(a) * 0.35f);
            }
            if (Time.time > _end)
            {
                _end = float.MaxValue;
                transform.DOScale(0f, 0.2f).OnComplete(() => Destroy(gameObject));
            }
        }

        private void OnDestroy() => transform.DOKill();
    }
}
