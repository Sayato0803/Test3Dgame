using DG.Tweening;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// VFX của lá phép, dựng bằng code từ asset của CartoonAssets (như CartoonFx):
    /// hiệu ứng tức thì khi dùng / kích hoạt lá, và vòng trạng thái gắn trên quân (StatusFx).
    /// </summary>
    public static class CardFx
    {
        public static readonly Color ShieldColor = new(0.45f, 0.8f, 1f);
        public static readonly Color ReflectColor = new(1f, 0.85f, 0.3f);
        public static readonly Color BombColor = new(1f, 0.35f, 0.15f);
        public static readonly Color BoostColor = new(1f, 0.3f, 0.25f);
        public static readonly Color HealColor = new(0.45f, 1f, 0.45f);
        public static readonly Color CloneColor = new(1f, 0.55f, 0.9f);
        public static readonly Color RiftInColor = new(1f, 0.55f, 0.1f);
        public static readonly Color RiftOutColor = new(0.2f, 0.6f, 1f);

        // ---------------- Hiệu ứng tức thì ----------------

        /// <summary>Dùng lá lên 1 quân: vòng sóng màu loang ra + sao bắn + hạt sáng bay lên.</summary>
        public static void Cast(Vector3 feet, Color color)
        {
            RingWave(feet, color, 0.9f);
            Rise(feet, color, 14);
            CartoonFx.StarBurst(feet + Vector3.up * 0.8f, color, 10, 3f);
        }

        /// <summary>Healing: hạt xanh lá bay lên dày + vòng sóng.</summary>
        public static void Heal(Vector3 feet)
        {
            RingWave(feet, HealColor, 1f);
            Rise(feet, HealColor, 26);
            Rise(feet, Color.white, 8);
        }

        /// <summary>Shield chặn đòn: vòm khiên vỡ thành mảnh + vòng sóng xanh.</summary>
        public static void ShieldBreak(Vector3 feet)
        {
            RingWave(feet, ShieldColor, 1.4f, 0.35f, 0.18f);
            CartoonFx.StarBurst(feet + Vector3.up * 0.7f, ShieldColor, 16, 5f);
            CartoonFx.StarBurst(feet + Vector3.up * 0.7f, Color.white, 6, 4f);
            Sfx.Play(SfxId.Ding, 0.8f, 0.1f);
        }

        /// <summary>Reflect: tia sét bật từ King ngược về attacker.</summary>
        public static void ReflectBounce(Vector3 kingFeet, Vector3 attackerFeet)
        {
            Vector3 from = kingFeet + Vector3.up * 0.8f, to = attackerFeet + Vector3.up * 0.8f;
            RingWave(kingFeet, ReflectColor, 1.2f, 0.35f, 0.15f);
            CartoonFx.Lightning(from, to);
            CartoonFx.StarBurst(to, ReflectColor, 14, 4f);
            Sfx.Play(SfxId.Zap, 1f, 0.05f);
        }

        /// <summary>Suicide Bomb: cầu lửa + khói + vòng sóng phủ vùng nổ + rung camera.</summary>
        public static void Explosion(Vector3 center, float radius)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a != null && a.PuffMaterial != null)
            {
                ParticleSystem ps = CartoonFx.Create("Explosion", center + Vector3.up * 0.4f, a.PuffMaterial);
                var main = ps.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.1f);
                main.gravityModifier = -0.3f;
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.3f;
                var drag = ps.limitVelocityOverLifetime;
                drag.enabled = true;
                drag.drag = 3f;
                CartoonFx.SizeGrowFade(ps);
                var col = ps.colorOverLifetime; // vàng -> cam -> khói xám
                var g = new Gradient();
                g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.5f), 0f), new GradientColorKey(new Color(1f, 0.45f, 0.1f), 0.3f),
                        new GradientColorKey(new Color(0.35f, 0.3f, 0.3f), 1f) },
                    new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.9f, 0.5f), new GradientAlphaKey(0f, 1f) });
                col.color = g;
                ps.Emit(30);
                Object.Destroy(ps.gameObject, 1.3f);
            }
            CartoonFx.Puff(center + Vector3.up * 0.2f, 2.2f, 24);
            RingWave(center, BombColor, radius, 0.4f, 0.22f);
            CartoonFx.StarBurst(center + Vector3.up * 0.5f, BombColor, 18, 6f);
            CartoonFx.Comic("pow", center + Vector3.up * 1.4f);
            Sfx.Play(SfxId.Punch, 1f, 0f);
            if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.4f, 0.35f);
        }

        /// <summary>Tornado: cột khói xoáy bốc lên giữa vùng 2x2 + lá cây bay.</summary>
        public static void Tornado(Vector3 center, float size)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a != null && a.PuffMaterial != null)
            {
                ParticleSystem ps = CartoonFx.Create("Tornado", center, a.PuffMaterial);
                var main = ps.main;
                main.duration = 1f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.4f);
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.4f * size, 0.75f * size);
                // xám xanh đậm: khói trắng chìm mất trên nền ô sáng
                main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.62f, 0.68f, 0.8f, 0.95f), new Color(0.45f, 0.5f, 0.62f, 0.95f));
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.5f * size;
                shape.rotation = new Vector3(90f, 0f, 0f);
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.Local;
                vel.orbitalY = new ParticleSystem.MinMaxCurve(7f);
                vel.y = new ParticleSystem.MinMaxCurve(2.5f);
                vel.radial = new ParticleSystem.MinMaxCurve(0.4f); // xoè ra khi lên cao
                CartoonFx.SizeGrowFade(ps);
                var emission = ps.emission;
                emission.enabled = true;
                emission.rateOverTime = 90f;
                ps.Play();
                Object.Destroy(ps.gameObject, 2.3f);
            }
            RingWave(center, Color.white, 1.4f * size, 0.5f);
            CartoonFx.StarBurst(center + Vector3.up * 0.5f, new Color(0.5f, 0.85f, 0.35f), 10, 4f);
            CartoonFx.Comic("whoosh", center + Vector3.up * 1.4f);
            Sfx.Play(SfxId.Whoosh);
        }

        /// <summary>Dịch chuyển qua Rift: loé sáng ở chỗ đi (màu cổng vào) và chỗ đến (màu cổng ra).</summary>
        public static void Teleport(Vector3 from, Vector3 to)
        {
            RingWave(from, RiftInColor, 0.9f);
            CartoonFx.StarBurst(from + Vector3.up * 0.5f, RiftInColor, 12, 3f);
            RingWave(to, RiftOutColor, 0.9f);
            Rise(to, RiftOutColor, 16);
            CartoonFx.StarBurst(to + Vector3.up * 0.5f, RiftOutColor, 12, 3f);
        }

        /// <summary>Clone: lấp lánh hồng ở King gốc, King giả hiện ra từ khói hồng.</summary>
        public static void Clone(Vector3 from, Vector3 to)
        {
            CartoonFx.StarBurst(from + Vector3.up * 0.7f, CloneColor, 10, 3f);
            CartoonFx.SmokePoof(to + Vector3.up * 0.3f, 1f);
            RingWave(to, CloneColor, 1f);
            Rise(to, CloneColor, 14);
        }

        /// <summary>Cổng Rift trên ô: 2 vòng xoay + hạt bị hút vào tâm. Huỷ GameObject trả về khi cổng đóng.</summary>
        public static GameObject Portal(Vector3 cellCenter, Color color, float cellSize)
        {
            var root = new GameObject("RiftPortal");
            root.transform.position = cellCenter;
            Ring(root.transform, color, 0.38f * cellSize, 0.1f);
            Ring(root.transform, Color.Lerp(color, Color.white, 0.5f), 0.2f * cellSize, 0.06f);
            CartoonAssets a = CartoonAssets.Instance;
            if (a != null && a.StarMaterial != null)
            {
                ParticleSystem ps = Loop("PortalSwirl", root.transform, a.StarMaterial, 10f);
                var main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startLifetime = 0.8f;
                main.startSpeed = 0f;
                main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
                main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.38f * cellSize;
                shape.radiusThickness = 0f;
                shape.rotation = new Vector3(90f, 0f, 0f);
                var vel = ps.velocityOverLifetime;
                vel.enabled = true;
                vel.space = ParticleSystemSimulationSpace.Local;
                vel.orbitalY = new ParticleSystem.MinMaxCurve(5f);
                vel.radial = new ParticleSystem.MinMaxCurve(-0.45f); // hút vào tâm
                vel.y = new ParticleSystem.MinMaxCurve(0.3f);
                ps.Play();
            }
            root.transform.DORotate(new Vector3(0f, 360f, 0f), 2f, RotateMode.FastBeyond360).SetLoops(-1).SetEase(Ease.Linear).SetLink(root);
            root.transform.localScale = Vector3.zero;
            root.transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack).SetLink(root);
            RingWave(cellCenter, color, 0.8f);
            return root;
        }

        // ---------------- Vòng trạng thái (StatusFx tạo, gắn trên quân) ----------------

        /// <summary>Shield: vòng xanh nhịp thở + vòm sáng mờ + hạt lấp lánh bay lên.</summary>
        public static GameObject ShieldAura(Transform unit)
        {
            Transform root = AuraRoot(unit, "ShieldFx");
            Pulse(Ring(root, ShieldColor, 0.5f, 0.1f), 0.08f, 0.16f, 0.6f);
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null) return root.gameObject;
            if (a.PuffMaterial != null)
            {
                ParticleSystem dome = Loop("ShieldDome", root, a.PuffMaterial, 1.5f);
                dome.transform.localPosition = Vector3.up * 0.6f;
                var main = dome.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                main.startLifetime = 1.4f;
                main.startSpeed = 0f;
                main.startSize = 1.5f;
                main.startColor = new Color(ShieldColor.r, ShieldColor.g, ShieldColor.b, 0.22f);
                var shape = dome.shape;
                shape.enabled = false;
                CartoonFx.SizeGrowFade(dome);
                dome.Play();
            }
            if (a.StarMaterial != null) Sparkles(root, a.StarMaterial, ShieldColor, 5f, 0.6f);
            return root.gameObject;
        }

        /// <summary>Reflect: vòng vàng mảnh + sao vàng quay quanh thân.</summary>
        public static GameObject ReflectAura(Transform unit)
        {
            Transform root = AuraRoot(unit, "ReflectFx");
            Ring(root, ReflectColor, 0.6f, 0.07f);
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.StarMaterial == null) return root.gameObject;
            ParticleSystem ps = Loop("ReflectOrbit", root, a.StarMaterial, 6f);
            ps.transform.localPosition = Vector3.up * 0.7f;
            var main = ps.main;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.startLifetime = 1.2f;
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.22f);
            main.startColor = new ParticleSystem.MinMaxGradient(ReflectColor, Color.white);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.55f;
            shape.radiusThickness = 0f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.space = ParticleSystemSimulationSpace.Local;
            vel.orbitalY = new ParticleSystem.MinMaxCurve(4f);
            ps.Play();
            return root.gameObject;
        }

        /// <summary>Suicide Bomb: vòng đỏ nháy nhanh như đồng hồ đếm + tia lửa ngòi nổ trên đầu.</summary>
        public static GameObject BombAura(Transform unit)
        {
            Transform root = AuraRoot(unit, "BombFx");
            Pulse(Ring(root, BombColor, 0.5f, 0.08f), 0.05f, 0.2f, 0.25f);
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.StarMaterial == null) return root.gameObject;
            ParticleSystem ps = Loop("BombFuse", root, a.StarMaterial, 14f);
            ps.transform.localPosition = Vector3.up * (TopOf(unit) + 0.1f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), BombColor);
            main.gravityModifier = 0.8f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 35f;
            shape.radius = 0.02f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            ps.Play();
            return root.gameObject;
        }

        /// <summary>DMG Boost: lửa đỏ cam bốc lên quanh attacker.</summary>
        public static GameObject BoostAura(Transform unit)
        {
            Transform root = AuraRoot(unit, "BoostFx");
            Pulse(Ring(root, BoostColor, 0.5f, 0.06f), 0.05f, 0.1f, 0.4f);
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return root.gameObject;
            ParticleSystem ps = Loop("BoostFlame", root, a.PuffMaterial, 20f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.8f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.75f, 0.2f, 0.9f), new Color(1f, 0.25f, 0.1f, 0.9f));
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.38f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(1.6f);
            CartoonFx.SizeGrowFade(ps);
            ps.Play();
            if (a.StarMaterial != null) Sparkles(root, a.StarMaterial, BoostColor, 8f, 1.5f); // tàn lửa bay cao hơn đầu
            return root.gameObject;
        }

        /// <summary>King giả: khói hồng mờ bốc lên (chỉ chủ quân thấy, để phân biệt với King thật).</summary>
        public static GameObject DecoyAura(Transform unit)
        {
            Transform root = AuraRoot(unit, "DecoyFx");
            Ring(root, CloneColor, 0.45f, 0.07f);
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return root.gameObject;
            ParticleSystem ps = Loop("DecoyWisp", root, a.PuffMaterial, 5f);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.6f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.3f);
            main.startColor = new Color(CloneColor.r, CloneColor.g, CloneColor.b, 0.5f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(0.5f);
            CartoonFx.SizeGrowFade(ps);
            ps.Play();
            return root.gameObject;
        }

        // ---------------- Khối dựng ----------------

        /// <summary>Gốc hiệu ứng gắn vào quân, khử scale của quân để mọi số đo bên trong tính theo đơn vị world.</summary>
        private static Transform AuraRoot(Transform unit, string name)
        {
            var root = new GameObject(name).transform;
            root.SetParent(unit, false);
            root.localScale = Vector3.one / Mathf.Max(unit.lossyScale.x, 0.01f);
            return root;
        }

        private static float TopOf(Transform unit) => unit.TryGetComponent(out UnitReactions r) ? r.TopHeight : 1f;

        /// <summary>Vòng tròn phẳng sát mặt bàn, con của 'parent'.</summary>
        private static LineRenderer Ring(Transform parent, Color color, float radius, float width)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.PuffMaterial == null) return null;
            var go = new GameObject("Ring");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = Vector3.up * 0.04f;
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = a.PuffMaterial;
            lr.useWorldSpace = false;
            lr.loop = true;
            const int n = 40;
            lr.positionCount = n;
            for (int i = 0; i < n; i++)
            {
                float t = i * Mathf.PI * 2f / n;
                lr.SetPosition(i, new Vector3(Mathf.Cos(t) * radius, 0f, Mathf.Sin(t) * radius));
            }
            lr.widthMultiplier = width;
            lr.startColor = lr.endColor = color;
            return lr;
        }

        /// <summary>Vòng sóng loang từ nhỏ tới bán kính 'radius' rồi mờ đi.</summary>
        public static void RingWave(Vector3 pos, Color color, float radius, float duration = 0.45f, float width = 0.12f)
        {
            var root = new GameObject("RingWave").transform;
            root.position = pos;
            LineRenderer lr = Ring(root, color, 1f, width);
            if (lr == null) { Object.Destroy(root.gameObject); return; }
            root.localScale = Vector3.one * radius * 0.2f;
            DOTween.Sequence().SetLink(root.gameObject)
                .Append(root.DOScale(radius, duration).SetEase(Ease.OutCubic))
                .Join(DOVirtual.Float(1f, 0f, duration, k => { Color c = color; c.a = k; lr.startColor = lr.endColor = c; }).SetEase(Ease.InQuad))
                .OnComplete(() => Object.Destroy(root.gameObject));
        }

        /// <summary>Độ dày vòng phập phồng liên tục (min -> max -> min, mỗi chiều 'half' giây).</summary>
        private static void Pulse(LineRenderer lr, float min, float max, float half)
        {
            if (lr == null) return;
            lr.widthMultiplier = min;
            DOTween.To(() => lr.widthMultiplier, w => lr.widthMultiplier = w, max, half)
                .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(lr.gameObject);
        }

        /// <summary>Đợt hạt sáng bay thẳng lên từ vòng quanh chân quân.</summary>
        private static void Rise(Vector3 feet, Color color, int count)
        {
            CartoonAssets a = CartoonAssets.Instance;
            if (a == null || a.StarMaterial == null) return;
            ParticleSystem ps = CartoonFx.Create("Rise", feet + Vector3.up * 0.1f, a.StarMaterial);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.6f, 0.9f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.4f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            // 3 trục phải cùng kiểu curve (ở đây: khoảng 2 hằng số), lệch kiểu Unity báo lỗi.
            vel.x = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.y = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1f), new Keyframe(0.7f, 1f), new Keyframe(1, 0f)));
            ps.Emit(count);
            Object.Destroy(ps.gameObject, 1.2f);
        }

        /// <summary>Hạt lấp lánh lơ lửng bay lên liên tục quanh quân.</summary>
        private static void Sparkles(Transform root, Material mat, Color color, float rate, float speed)
        {
            ParticleSystem ps = Loop("Sparkles", root, mat, rate);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1f, 1.4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.45f;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.y = new ParticleSystem.MinMaxCurve(speed);
            ps.Play();
        }

        /// <summary>ParticleSystem phát liên tục, con của 'parent' (gọi Play sau khi chỉnh xong).</summary>
        private static ParticleSystem Loop(string name, Transform parent, Material mat, float rate)
        {
            ParticleSystem ps = CartoonFx.Create(name, parent.position, mat);
            ps.transform.SetParent(parent, true);
            var main = ps.main;
            main.loop = true;
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            return ps;
        }
    }

    /// <summary>Vòng trạng thái lá phép trên 1 quân. Phases gọi Show sau mỗi thay đổi (bật thì tạo, tắt thì huỷ).</summary>
    public class StatusFx : MonoBehaviour
    {
        private GameObject _shield, _reflect, _bomb, _boost, _decoy;

        public void Show(bool shield, bool reflect, bool bomb, bool boost, bool decoy)
        {
            Toggle(ref _shield, shield, CardFx.ShieldAura);
            Toggle(ref _reflect, reflect, CardFx.ReflectAura);
            Toggle(ref _bomb, bomb, CardFx.BombAura);
            Toggle(ref _boost, boost, CardFx.BoostAura);
            Toggle(ref _decoy, decoy, CardFx.DecoyAura);
        }

        private void Toggle(ref GameObject fx, bool on, System.Func<Transform, GameObject> create)
        {
            if (on && fx == null) fx = create(transform);
            else if (!on && fx != null)
            {
                Destroy(fx);
                fx = null;
            }
        }
    }
}
