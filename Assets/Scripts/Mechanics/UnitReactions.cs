using DG.Tweening;
using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Phản ứng/hoạt ảnh procedural cartoon của 1 quân (model chưa có Animator Controller nên dùng tween):
    /// xuất hiện, bị cầm lên (giật mình rồi giãy, đung đưa theo hướng kéo), rơi xuống nảy + bụi,
    /// thở nhẹ khi đứng yên, lao lên tấn công, trúng đòn (rung + nháy đỏ), bị đè bẹp (thành bánh kếp rồi bật lại),
    /// đè bẹp người khác rồi nảy đi, bị đè chết thì bắn lên trời (bay mất hút thành 1 ngôi sao).
    /// Đòn kết liễu từ xa (Knight chém mạnh / Mage tụ điện zap) và King bị đánh văng khỏi sân chạy bằng Animator clip
    /// (CartoonAssets.UnitAnimator); clip gọi Fx("...") qua Animation Event để bật hiệu ứng đúng khung hình.
    ///
    /// Mọi chuyển động chạy trên object con "Visual" (bọc các con của model), nên transform gốc
    /// vẫn giữ đúng vị trí ô và hướng mặt - AttackRange đọc transform.forward của gốc.
    /// </summary>
    public class UnitReactions : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Transform _visual;
        private Animator _anim;
        private UnitReactions _victim;   // King sẽ bị văng khi đòn kết liễu trúng (event "impact")
        private Vector3 _victimDir;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Vector3 _baseScale = Vector3.one;

        private bool _held;
        private bool _dead;
        private bool _waitSpawn;
        private bool _scripted;          // đang chạy 1 chuỗi hoạt ảnh dài (đè bẹp / nảy / văng): bỏ qua Land, idle
        private bool _wantBurrow;        // King ẩn: muốn biến mất
        private bool _burrowed;          // đã biến mất (xoay 1 vòng rồi tan thành khói)
        private ParticleSystem _wisp;    // làn khói mỏng bốc lên ở ô King ẩn - chỉ chủ quân thấy
        private float _heldTime;
        private Vector3 _lastPos;
        private Vector3 _swing;          // độ nghiêng theo vận tốc kéo (làm mượt)
        private float _shake;            // độ lắc khi đang kéo, 0 = đứng yên (làm mượt)
        private float _idleSeed;
        private float _flashUntil;

        public Transform Visual => _visual;
        /// <summary>Chiều cao đỉnh đầu so với chân (world), đo lúc tạo.</summary>
        public float TopHeight { get; private set; } = 1f;

        private void Awake()
        {
            _visual = WrapChildren();
            _renderers = GetComponentsInChildren<Renderer>(true);
            _block = new MaterialPropertyBlock();
            _idleSeed = Random.value * 10f;
            _lastPos = transform.position;
            MeasureHeight();
        }

        /// <summary>Gom các con của model vào 1 pivot "Visual" để tween mà không đụng transform gốc.</summary>
        private Transform WrapChildren()
        {
            var visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform c = transform.GetChild(i);
                if (c != visual) c.SetParent(visual, true);
            }
            return visual;
        }

        private void MeasureHeight()
        {
            float top = float.MinValue;
            foreach (Renderer r in _renderers)
                if (r != null) top = Mathf.Max(top, r.bounds.max.y);
            if (top > float.MinValue) TopHeight = Mathf.Max(0.3f, top - transform.position.y);
        }

        private Vector3 Feet => transform.position;

        // ---------------- Sự kiện cơ bản ----------------

        /// <summary>Ẩn model tới khi PlaySpawn được gọi (dùng khi muốn xuất hiện trễ).</summary>
        public void HideUntilSpawn()
        {
            _waitSpawn = true;
            _visual.localScale = Vector3.zero;
        }

        public void PlaySpawn()
        {
            if (this == null) return;
            _waitSpawn = false;
            _visual.DOKill();
            _visual.localScale = Vector3.zero;
            _visual.DOScale(_baseScale, 0.35f).SetEase(Ease.OutBack);
            _visual.DOPunchRotation(new Vector3(0f, 25f, 0f), 0.45f, 8, 0.8f);
            Sfx.Play(SfxId.Pop);
        }

        /// <summary>Bị nhấc lên: giật mình (co người + rung 1 nhịp); sau đó chỉ nghiêng/lắc theo hướng kéo trong Update.</summary>
        public void BeginHeld()
        {
            if (_dead || _scripted) return;
            if (_burrowed)
            {
                // Bị nhấc lên: hiện ra ngay từ làn khói.
                _burrowed = false;
                SetWisp(false);
                _visual.DOKill();
                _visual.localPosition = Vector3.zero;
                _visual.localRotation = Quaternion.identity;
                if (IsShown) { Sfx.Play(SfxId.Poof, 0.8f); CartoonFx.Puff(Feet + Vector3.up * 0.3f, 0.8f, 10); }
            }
            _held = true;
            if (TryGetComponent(out CharacterRig rig)) rig.Panic = true;
            _heldTime = 0f;
            _shake = 0f;
            _lastPos = transform.position;
            _visual.DOKill(true);
            _visual.localScale = _baseScale;
            _visual.DOPunchScale(new Vector3(-0.15f, 0.25f, -0.15f), 0.3f, 8, 0.7f);
            _visual.DOShakeRotation(0.35f, new Vector3(15f, 10f, 25f), 25, 90f, true);
            Sfx.Play(SfxId.Yelp, 0.8f, 0.12f);
        }

        /// <summary>Được thả xuống. fallHeight = khoảng cách từ độ cao lúc kéo tới mặt bàn: model rơi nốt rồi nảy.</summary>
        public void Land(float fallHeight, bool quiet = false)
        {
            _held = false;
            if (TryGetComponent(out CharacterRig rig)) rig.Panic = false;
            if (_scripted || _dead) return; // đang chạy chuỗi đè bẹp / nảy: chuỗi đó tự kết thúc bằng cú tiếp đất
            ResetVisual();
            _swing = Vector3.zero;

            Sequence s = DOTween.Sequence().SetTarget(_visual);
            if (fallHeight > 0.01f)
            {
                _visual.localPosition = Vector3.up * fallHeight / Mathf.Max(transform.lossyScale.y, 0.001f);
                s.Append(_visual.DOLocalMove(Vector3.zero, 0.18f).SetEase(Ease.InQuad));
            }
            s.AppendCallback(() => Touchdown(quiet ? 0f : 1f));
            AppendSquashStretch(s, 1f);
        }

        /// <summary>Squash & stretch khi chạm đất.</summary>
        private void AppendSquashStretch(Sequence s, float strength)
        {
            Vector3 b = _baseScale;
            s.Append(_visual.DOScale(new Vector3(b.x * (1f + 0.3f * strength), b.y * (1f - 0.3f * strength), b.z * (1f + 0.3f * strength)), 0.07f));
            s.Append(_visual.DOScale(new Vector3(b.x * (1f - 0.1f * strength), b.y * (1f + 0.15f * strength), b.z * (1f - 0.1f * strength)), 0.1f));
            s.Append(_visual.DOScale(b, 0.15f).SetEase(Ease.OutBack));
        }

        private void Touchdown(float volume)
        {
            if (volume <= 0f) return;
            Sfx.Play(SfxId.Thud, volume);
            CartoonFx.Puff(Feet + Vector3.up * 0.05f, 0.8f, 10);
        }

        /// <summary>Xoay hướng (cuộn chuột): nảy nhẹ cho có cảm giác.</summary>
        public void PlayTurn()
        {
            Sfx.Play(SfxId.Tick);
            if (!_held) _visual.DOPunchScale(Vector3.one * 0.08f, 0.2f, 6, 1f);
        }

        /// <summary>Lao về phía trước (theo hướng mặt) rồi lùi về.</summary>
        public void PlayAttack(float delay)
        {
            if (_dead || _scripted) return;
            Sequence s = DOTween.Sequence().SetTarget(_visual).SetDelay(delay);
            s.Append(_visual.DOLocalMove(new Vector3(0f, 0.15f, -0.25f), 0.12f).SetEase(Ease.OutQuad)); // lấy đà
            s.AppendCallback(() => Sfx.Play(SfxId.Whoosh, 0.6f, 0.15f));
            s.Append(_visual.DOLocalMove(new Vector3(0f, 0.05f, 0.6f), 0.1f).SetEase(Ease.InQuad));    // lao tới
            s.Append(_visual.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutBack));
        }

        /// <summary>Trúng đòn (không chết): rung, giật lùi, nháy đỏ, sao bắn ra.</summary>
        public void PlayHit(float delay)
        {
            // Delayed call không gắn target _visual, để DOKill bên trong không tự huỷ chính nó.
            DOVirtual.DelayedCall(delay, () =>
            {
                if (this == null || _dead) return;
                ResetVisual();
                _visual.DOShakePosition(0.4f, new Vector3(0.25f, 0.05f, 0.25f), 30, 90f);
                _visual.DOPunchRotation(new Vector3(-25f, 0f, 15f), 0.4f, 10, 0.6f);
                _visual.DOPunchScale(new Vector3(0.2f, -0.2f, 0.2f), 0.3f, 8, 1f);
                _flashUntil = Time.time + 0.35f;
                Sfx.Play(SfxId.Punch);
                CartoonFx.StarBurst(Feet + Vector3.up * TopHeight * 0.7f, new Color(1f, 0.85f, 0.3f));
            });
        }

        // ---------------- Đòn kết liễu (Animator) ----------------

        /// <summary>Animator gắn lúc cần: chỉ bật khi chạy clip, để không đè các hoạt ảnh procedural.</summary>
        private Animator Anim
        {
            get
            {
                if (_anim != null) return _anim;
                _anim = gameObject.AddComponent<Animator>();
                _anim.runtimeAnimatorController = CartoonAssets.Instance.UnitAnimator;
                _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                _anim.enabled = false;
                return _anim;
            }
        }

        private AnimationClip Clip(string name)
        {
            foreach (AnimationClip c in Anim.runtimeAnimatorController.animationClips)
                if (c.name == name) return c;
            return null;
        }

        /// <summary>Thời điểm (giây) của Animation Event Fx(fx) trong clip, 0 nếu không có.</summary>
        private float EventTime(string clip, string fx)
        {
            AnimationClip c = Clip(clip);
            if (c != null)
                foreach (AnimationEvent e in c.events)
                    if (e.stringParameter == fx) return e.time;
            return 0f;
        }

        private void PlayClip(string trigger)
        {
            _visual.DOKill();
            _visual.localPosition = Vector3.zero;
            _visual.localRotation = Quaternion.identity;
            _visual.localScale = _baseScale;
            Anim.enabled = true;
            Anim.SetTrigger(trigger);
        }

        /// <summary>
        /// Đòn kết liễu sau 'delay': Knight chém thật mạnh (PowerSlash), Mage tụ điện rồi zap (ZapCharge).
        /// Lúc trúng (event "impact" trong clip) King 'victim' bị đánh văng theo hướng từ quân này tới King.
        /// Trả thời điểm trúng (giây kể từ bây giờ) để HUD hiện damage / slow-mo đúng lúc.
        /// </summary>
        public float PlayFinisher(UnitReactions victim, float delay)
        {
            string trigger = TryGetComponent(out GridUnit u) && u.Kind == UnitKind.Mage ? "ZapCharge" : "PowerSlash";
            _victim = victim;
            Vector3 d = victim.transform.position - transform.position;
            _victimDir = new Vector3(d.x, 0f, d.z).normalized;
            DOVirtual.DelayedCall(delay, () =>
            {
                if (this == null) return;
                _held = false;
                _scripted = true;
                PlayClip(trigger); // clip tự kết thúc bằng event "end"
            });
            return delay + EventTime(trigger, "impact");
        }

        /// <summary>Bị đòn kết liễu: quay lưng về hướng bị đánh rồi chạy clip KnockedOff (nảy trên sân, văng xuống biển).</summary>
        public void PlayKnockedOff(Vector3 dir)
        {
            if (this == null || _dead) return;
            _dead = true;
            _held = false;
            _scripted = true;
            SetWisp(false);
            // shortcut: clip bay cố định ~33.4m theo +Z local (đủ ra biển từ mọi ô của đảo hiện tại), đổi cỡ đảo thì sửa clip + FinisherTests.
            if (dir != Vector3.zero) transform.rotation = Quaternion.LookRotation(dir);
            Sfx.Play(SfxId.Punch, 1f, 0f);
            Sfx.Play(SfxId.SlideWhistle, 0.7f, 0.05f, 0.05f);
            if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.3f, 0.3f);
            CartoonFx.StarBurst(Feet + Vector3.up * TopHeight * 0.5f, Color.white, 16, 7f);
            CartoonFx.Comic("ko", Feet + Vector3.up * TopHeight);
            PlayClip("KnockedOff");
        }

        /// <summary>Animation Event của các clip: Fx("tên") bật hiệu ứng / âm thanh đúng khung hình.</summary>
        public void Fx(string id)
        {
            switch (id)
            {
                case "charge":
                    Sfx.Play(SfxId.Magic, 1f, 0f);
                    CartoonFx.ZapCharge(Feet + Vector3.up * 0.9f);
                    break;
                case "impact":
                    FinisherImpact();
                    break;
                case "thud":
                    Sfx.Play(SfxId.Thud, 0.8f);
                    CartoonFx.Puff(_visual.position + Vector3.up * 0.05f, 0.9f, 10);
                    break;
                case "end":
                    // Hết clip đòn kết liễu: tắt Animator, trả quyền lại cho hoạt ảnh procedural.
                    Anim.enabled = false;
                    ResetVisual();
                    _scripted = false;
                    break;
                case "splash":
                    CartoonFx.Splash(_visual.position);
                    Sfx.Play(SfxId.Splash, 1f, 0.05f);
                    _visual.gameObject.SetActive(false);
                    break;
            }
        }

        private void FinisherImpact()
        {
            if (_victim == null) return;
            Vector3 body = _victim.transform.position + Vector3.up * _victim.TopHeight * 0.5f;
            if (TryGetComponent(out GridUnit u) && u.Kind == UnitKind.Mage)
            {
                Sfx.Play(SfxId.Zap, 1f, 0f);
                CartoonFx.Lightning(Feet + Vector3.up * 0.9f, body);
                CartoonFx.StarBurst(body, new Color(0.45f, 0.95f, 1f), 20, 6f);
                CartoonFx.StarBurst(body, new Color(1f, 0.95f, 0.4f), 12, 4f);
            }
            else
            {
                CartoonFx.Slash(body, _victimDir, 0f, comic: false, size: 1.8f);
            }
            _victim.PlayKnockedOff(_victimDir);
            _victim = null;
        }

        // ---------------- Đè bẹp ----------------

        /// <summary>
        /// Attacker (đang đứng trên ô King tại kingFeet) chờ 'delay', nhún lấy đà, nhảy vọt lên cao rồi giáng xuống
        /// đè bẹp King, nhún trên "bánh kếp" rồi nảy lộn nhào sang ô mới (gốc transform đã ở ô mới).
        /// Trả về thời điểm va chạm (giây kể từ bây giờ) để King phản ứng đúng lúc.
        /// </summary>
        public float PlaySquashAndBounce(Vector3 kingFeet, float kingHeight, float delay)
        {
            _held = false;
            _scripted = true;
            _visual.DOKill();
            _visual.localRotation = Quaternion.identity;
            _visual.localScale = _baseScale;
            _visual.position = kingFeet; // trong lúc chờ vẫn đứng ở ô cũ (đè lên King)

            const float crouch = 0.1f, rise = 0.28f, fall = 0.2f;
            Vector3 headTop = kingFeet + Vector3.up * kingHeight;
            Vector3 pancakeTop = kingFeet + Vector3.up * kingHeight * 0.15f;
            Vector3 b = _baseScale;

            Sequence s = DOTween.Sequence().SetTarget(_visual);
            s.AppendInterval(delay);
            // 1. Nhún lấy đà rồi nhảy vọt lên cao (xoay 1 vòng cho "ngầu").
            s.Append(_visual.DOScale(new Vector3(b.x * 1.25f, b.y * 0.7f, b.z * 1.25f), crouch));
            s.AppendCallback(() => Sfx.Play(SfxId.Yelp, 0.7f, 0.1f));
            s.Append(_visual.DOMove(headTop + Vector3.up * 2.4f, rise).SetEase(Ease.OutQuad));
            s.Join(_visual.DOScale(new Vector3(b.x * 0.85f, b.y * 1.2f, b.z * 0.85f), rise * 0.5f));
            s.Join(_visual.DOLocalRotate(new Vector3(0f, 360f, 0f), rise, RotateMode.LocalAxisAdd));
            // 2. Giáng thẳng xuống đầu King.
            s.Append(_visual.DOMove(headTop, fall).SetEase(Ease.InQuad));
            s.Join(_visual.DOScale(b, fall));
            // 3. Ép King thành bánh kếp (King lo phần của nó), attacker lún theo + bẹp người.
            s.Append(_visual.DOMove(pancakeTop, 0.06f).SetEase(Ease.OutQuad));
            s.Join(_visual.DOScale(new Vector3(b.x * 1.35f, b.y * 0.6f, b.z * 1.35f), 0.06f));
            s.Append(_visual.DOScale(new Vector3(b.x * 0.85f, b.y * 1.25f, b.z * 0.85f), 0.12f));
            // 4. Nhún nhún trên bánh kếp như đứng trên đệm.
            s.Append(_visual.DOMove(pancakeTop + Vector3.up * 0.25f, 0.12f).SetEase(Ease.OutQuad));
            s.Join(_visual.DOScale(b, 0.12f));
            s.Append(_visual.DOMove(pancakeTop, 0.1f).SetEase(Ease.InQuad));
            s.AppendCallback(() => Sfx.Play(SfxId.Boing));
            // 5. Bật nảy lộn nhào sang ô mới.
            s.Append(_visual.DOJump(Feet, 1.8f, 1, 0.6f).SetEase(Ease.Linear));
            s.Join(_visual.DOLocalRotate(new Vector3(360f, 0f, 0f), 0.6f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutQuad));
            s.AppendCallback(() =>
            {
                _visual.localPosition = Vector3.zero;
                _visual.localRotation = Quaternion.identity;
                Touchdown(1f);
            });
            AppendSquashStretch(s, 1.2f);
            s.OnComplete(() => _scripted = false);
            return delay + crouch + rise + fall;
        }

        /// <summary>
        /// Bị đè bẹp lúc 'impactDelay': dẹp lép thành bánh kếp, nằm rung rinh, rồi "bụp" bật lại + choáng váng.
        /// launch: thay vì bật lại, bánh kếp bung như lò xo bắn King thẳng lên trời (bị đè chết).
        /// </summary>
        public void PlaySquashed(float impactDelay, bool launch)
        {
            DOVirtual.DelayedCall(impactDelay, () =>
            {
                if (this == null || _dead) return;
                _scripted = true;
                ResetVisual();
                Vector3 b = _baseScale;
                var pancake = new Vector3(b.x * 1.9f, b.y * 0.12f, b.z * 1.9f);

                Sfx.Play(SfxId.Squish, 1f, 0.05f);
                if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.3f, 0.35f);
                Sfx.Play(SfxId.Punch, 0.5f);
                CartoonFx.Puff(Feet + Vector3.up * 0.05f, 1.3f, 18);
                CartoonFx.StarBurst(Feet + Vector3.up * 0.3f, new Color(1f, 0.8f, 0.3f), 14, 5f);
                CartoonFx.Comic("bonk", Feet + Vector3.up * 0.8f);
                _flashUntil = Time.time + 0.25f;

                Sequence s = DOTween.Sequence().SetTarget(_visual);
                // Dẹp lép tức thì (hơi quá đà rồi hồi lại 1 chút cho "dẻo").
                s.Append(_visual.DOScale(new Vector3(b.x * 2.1f, b.y * 0.08f, b.z * 2.1f), 0.05f).SetEase(Ease.OutQuad));
                s.Append(_visual.DOScale(pancake, 0.08f));
                // Bánh kếp rung rinh như thạch.
                s.Append(_visual.DOPunchScale(new Vector3(0.25f, 0f, -0.25f), 0.45f, 6, 0.6f));
                s.Join(_visual.DOShakeRotation(0.45f, new Vector3(0f, 25f, 0f), 12, 90f, false));
                s.AppendInterval(0.35f);
                if (launch)
                {
                    s.OnComplete(LaunchToSky);
                    return;
                }
                // "Bụp" - phồng lại như bóng bay, vươn cao quá đà rồi lò xo về dáng cũ.
                s.AppendCallback(() => Sfx.Play(SfxId.Pop, 1f, 0f));
                s.Append(_visual.DOScale(new Vector3(b.x * 0.7f, b.y * 1.5f, b.z * 0.7f), 0.12f).SetEase(Ease.OutQuad));
                s.Append(_visual.DOScale(b, 0.7f).SetEase(Ease.OutElastic));
                s.Join(_visual.DOShakeRotation(1.2f, new Vector3(10f, 0f, 18f), 6, 90f, true));
                s.InsertCallback(0.95f, () => CartoonFx.DizzyStars(transform, Vector3.up * (TopHeight + 0.15f), 2.2f));
                s.OnComplete(() => _scripted = false);
            });
        }

        // ---------------- Chết: bắn lên trời (bị đè bẹp) ----------------

        /// <summary>Bánh kếp bung như lò xo: vươn dài rồi vút thẳng lên trời, xoay tít, kéo vệt khói, loé sao trên cao.</summary>
        public void LaunchToSky()
        {
            if (this == null || _dead) return;
            _dead = true;
            _held = false;
            _scripted = true;
            SetWisp(false);
            _visual.DOKill();
            Vector3 b = _baseScale;
            Vector3 start = _visual.position;
            Vector3 top = start + Vector3.up * 14f;

            Sequence s = DOTween.Sequence().SetTarget(_visual);
            // Lò xo: nén thêm rồi bung dài ngoằng.
            s.Append(_visual.DOScale(new Vector3(b.x * 2.2f, b.y * 0.05f, b.z * 2.2f), 0.08f));
            s.AppendCallback(() =>
            {
                Sfx.Play(SfxId.Boing, 1f, 0f);
                Sfx.Play(SfxId.SlideWhistle, 0.9f, 0f, 0.05f);
                if (CameraRig.Instance != null) CameraRig.Instance.Shake(0.25f, 0.3f);
                CartoonFx.Puff(start + Vector3.up * 0.05f, 1.4f, 20);
                CartoonFx.Comic("ko", start + Vector3.up * TopHeight);
            });
            s.Append(_visual.DOScale(new Vector3(b.x * 0.55f, b.y * 1.9f, b.z * 0.55f), 0.1f).SetEase(Ease.OutQuad));
            // Vút lên: nhanh dần rồi chậm lại ở đỉnh, xoay tít quanh trục đứng, nhỏ dần vì xa.
            s.Append(_visual.DOMove(top, 1.0f).SetEase(Ease.OutCubic));
            s.Join(_visual.DOLocalRotate(new Vector3(0f, 1440f, 0f), 1.0f, RotateMode.LocalAxisAdd).SetEase(Ease.OutCubic));
            s.Join(_visual.DOScale(b * 0.35f, 1.0f).SetEase(Ease.InQuad));
            // Vệt khói phía sau như tên lửa.
            for (int i = 0; i < 10; i++)
                s.InsertCallback(0.2f + i * 0.08f, () => CartoonFx.Puff(_visual.position, 0.55f, 4));
            s.AppendCallback(() =>
            {
                CartoonFx.Twinkle(_visual.position);
                Sfx.Play(SfxId.Ding, 0.9f, 0f);
                _visual.gameObject.SetActive(false);
            });
        }

        /// <summary>Ăn mừng chiến thắng (lặp mãi): nhún nhảy 2 nhịp, lắc lư trái phải, bật cao xoay 1 vòng.</summary>
        public void PlayDance(float delay)
        {
            if (this == null || _dead) return;
            _held = false;
            _scripted = true;
            ResetVisual();
            Vector3 b = _baseScale;
            float up = 0.35f / Mathf.Max(transform.lossyScale.y, 0.001f);
            var squash = new Vector3(b.x * 1.15f, b.y * 0.85f, b.z * 1.15f);
            var stretch = new Vector3(b.x * 0.9f, b.y * 1.15f, b.z * 0.9f);

            Sequence s = DOTween.Sequence().SetTarget(_visual).SetDelay(delay);
            for (int i = 0; i < 2; i++)
            {
                s.Append(_visual.DOScale(squash, 0.1f));
                s.Append(_visual.DOLocalMoveY(up * 0.5f, 0.15f).SetEase(Ease.OutQuad));
                s.Join(_visual.DOScale(stretch, 0.15f));
                s.Append(_visual.DOLocalMoveY(0f, 0.15f).SetEase(Ease.InQuad));
                s.Join(_visual.DOScale(b, 0.15f));
            }
            s.Append(_visual.DOLocalRotate(new Vector3(0f, 0f, 18f), 0.18f).SetEase(Ease.OutQuad));
            s.Append(_visual.DOLocalRotate(new Vector3(0f, 0f, -18f), 0.3f).SetEase(Ease.InOutQuad));
            s.Append(_visual.DOLocalRotate(Vector3.zero, 0.18f).SetEase(Ease.InQuad));
            s.Append(_visual.DOScale(squash, 0.1f));
            s.Append(_visual.DOLocalMoveY(up, 0.25f).SetEase(Ease.OutQuad));
            s.Join(_visual.DOScale(stretch, 0.25f));
            s.Join(_visual.DOLocalRotate(new Vector3(0f, 360f, 0f), 0.5f, RotateMode.LocalAxisAdd));
            s.Append(_visual.DOLocalMoveY(0f, 0.25f).SetEase(Ease.InQuad));
            s.Join(_visual.DOScale(b, 0.25f));
            s.SetLoops(-1);
        }

        /// <summary>Hiệu ứng huỷ (quân mới bị huỷ khi kéo): thu nhỏ rồi xoá.</summary>
        public void Vanish()
        {
            _held = false;
            _visual.DOKill();
            Sfx.Play(SfxId.Pop, 0.6f);
            CartoonFx.Puff(_visual.position, 0.6f, 8);
            _visual.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => Destroy(gameObject));
        }

        private void ResetVisual()
        {
            _visual.DOKill();
            _burrowed = false; // về tư thế đứng; nếu vẫn cần ẩn, Update sẽ cho biến mất lại
            SetWisp(false);
            _visual.localPosition = Vector3.zero;
            _visual.localRotation = Quaternion.identity;
            _visual.localScale = _baseScale;
        }

        // ---------------- Vòng lặp ----------------

        private void Update()
        {
            if (_flashUntil > 0f)
            {
                if (Time.time < _flashUntil)
                {
                    float k = Mathf.PingPong(Time.time * 12f, 1f);
                    SetTint(Color.Lerp(Color.white, new Color(1f, 0.2f, 0.2f), k));
                }
                else
                {
                    _flashUntil = 0f;
                    ClearTint();
                }
            }

            if (_dead || _waitSpawn || _scripted) return;
            if (_held) { UpdateHeld(); return; }
            if (_wantBurrow != _burrowed && !DOTween.IsTweening(_visual))
            {
                if (_wantBurrow) PlayBurrow();
                else PlayEmerge();
                return;
            }
            UpdateIdle();
        }

        // ---------------- Ẩn: chui xuống bàn ----------------

        /// <summary>Đặt trạng thái ẩn mong muốn; chuyển cảnh chạy khi quân rảnh (không bị cầm / không đang tween).</summary>
        public void SetBurrowed(bool burrowed) => _wantBurrow = burrowed;

        /// <summary>Có renderer nào đang bật (người đang xem nhìn thấy quân này)?</summary>
        private bool IsShown
        {
            get
            {
                foreach (Renderer r in _renderers)
                    if (r != null && r.enabled) return true;
                return false;
            }
        }

        /// <summary>Xoay tít 1 vòng, thu nhỏ rồi biến mất, để lại vệt khói bay lên; ô đó còn làn khói mỏng cho chủ quân.</summary>
        private void PlayBurrow()
        {
            _burrowed = true;
            bool fx = IsShown;
            Vector3 b = _baseScale;
            Sequence s = DOTween.Sequence().SetTarget(_visual);
            s.Append(_visual.DOScale(new Vector3(b.x * 1.15f, b.y * 0.8f, b.z * 1.15f), 0.1f));                    // nhún lấy đà
            s.AppendCallback(() => { if (fx) Sfx.Play(SfxId.Whoosh, 0.8f, 0.1f); });
            s.Append(_visual.DOLocalRotate(new Vector3(0f, 360f, 0f), 0.45f, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad));
            s.Join(_visual.DOScale(new Vector3(b.x * 0.8f, b.y * 1.15f, b.z * 0.8f), 0.3f));
            s.Join(_visual.DOLocalMoveY(0.12f / Mathf.Max(transform.lossyScale.y, 0.001f), 0.3f).SetEase(Ease.OutQuad));
            s.AppendCallback(() =>
            {
                if (!fx) return;
                Sfx.Play(SfxId.Poof, 1f, 0.08f);
                CartoonFx.SmokeTrail(Feet, TopHeight);
                CartoonFx.Comic("whoosh", Feet + Vector3.up * TopHeight);
            });
            s.Append(_visual.DOScale(Vector3.zero, 0.12f).SetEase(Ease.InBack));
            s.AppendCallback(() =>
            {
                _visual.localPosition = Vector3.zero;
                _visual.localRotation = Quaternion.identity;
                SetWisp(true);
            });
        }

        /// <summary>Hiện ra: khói "bụp", xoay ngược 1 vòng phóng to, tiếp đất nảy nhẹ.</summary>
        private void PlayEmerge()
        {
            _burrowed = false;
            SetWisp(false);
            bool fx = IsShown;
            if (fx)
            {
                Sfx.Play(SfxId.Poof, 0.8f, 0.08f);
                Sfx.Play(SfxId.Boing, 0.5f, 0.1f, 0.05f);
                CartoonFx.SmokePoof(Feet + Vector3.up * TopHeight * 0.45f, TopHeight);
            }
            Vector3 b = _baseScale;
            _visual.localScale = Vector3.zero;
            _visual.localPosition = new Vector3(0f, 0.2f / Mathf.Max(transform.lossyScale.y, 0.001f), 0f);
            Sequence s = DOTween.Sequence().SetTarget(_visual);
            s.Append(_visual.DOScale(new Vector3(b.x * 0.85f, b.y * 1.2f, b.z * 0.85f), 0.28f).SetEase(Ease.OutBack));
            s.Join(_visual.DOLocalRotate(new Vector3(0f, -360f, 0f), 0.35f, RotateMode.LocalAxisAdd).SetEase(Ease.OutQuad));
            s.Append(_visual.DOLocalMoveY(0f, 0.12f).SetEase(Ease.InQuad));
            s.Join(_visual.DOScale(b, 0.12f));
            AppendSquashStretch(s, 0.8f);
        }

        /// <summary>Làn khói mỏng bốc lên từ ô King ẩn. Chỉ hiện khi người xem thấy King (= chủ quân).</summary>
        private void SetWisp(bool on)
        {
            if (on && _wisp == null) _wisp = CartoonFx.CreateWisp(transform);
            if (_wisp == null) return;
            if (on) _wisp.Play();
            else _wisp.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        /// <summary>Đang bị cầm: giãy (xoay lắc nhanh), đung đưa ngược hướng kéo như bị xách lên, co duỗi người.</summary>
        private void UpdateHeld()
        {
            if (DOTween.IsTweening(_visual)) { _lastPos = transform.position; return; } // để cú giật mình chạy xong

            _heldTime += Time.deltaTime;
            float t = _heldTime;

            // Vận tốc trong không gian local của quân -> nghiêng thân ngược chiều di chuyển.
            Vector3 vel = (transform.position - _lastPos) / Mathf.Max(Time.deltaTime, 0.0001f);
            _lastPos = transform.position;
            Vector3 localVel = transform.InverseTransformDirection(vel);
            Vector3 targetSwing = new(Mathf.Clamp(-localVel.z * 4f, -35f, 35f), 0f, Mathf.Clamp(localVel.x * 4f, -35f, 35f));
            _swing = Vector3.Lerp(_swing, targetSwing, 1f - Mathf.Exp(-8f * Time.deltaTime));

            // Chỉ lắc khi đang di chuyển: dao động dọc theo trục nghiêng (= hướng di chuyển), mạnh theo tốc độ; đứng yên thì im.
            float speed = Mathf.Clamp01(localVel.magnitude / 6f);
            _shake = Mathf.Lerp(_shake, speed, 1f - Mathf.Exp(-10f * Time.deltaTime));
            Vector3 wobble = _swing.normalized * (Mathf.Sin(t * 22f) * 12f * _shake);

            _visual.localRotation = Quaternion.Euler(_swing + wobble);
            _visual.localPosition = Vector3.zero;
            _visual.localScale = _baseScale;
        }

        /// <summary>Đứng yên: thở nhẹ. Đang chui dưới bàn: ló lên thụt xuống, ngó nghiêng.</summary>
        private void UpdateIdle()
        {
            if (DOTween.IsTweening(_visual)) return;
            if (_burrowed)
            {
                _visual.localScale = Vector3.zero;
                if (_wisp != null)
                {
                    var wr = _wisp.GetComponent<ParticleSystemRenderer>();
                    bool show = IsShown;
                    if (wr.enabled != show) wr.enabled = show;
                }
                return;
            }
            float b = 1f + Mathf.Sin((Time.time + _idleSeed) * 2.2f) * 0.025f;
            _visual.localScale = new Vector3(_baseScale.x * (2f - b), _baseScale.y * b, _baseScale.z * (2f - b));
        }

        private void SetTint(Color c)
        {
            foreach (Renderer r in _renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, c);
                _block.SetColor(ColorId, c);
                r.SetPropertyBlock(_block);
            }
        }

        private void ClearTint()
        {
            foreach (Renderer r in _renderers)
                if (r != null) r.SetPropertyBlock(null);
        }

        private void OnDestroy()
        {
            if (_visual != null) _visual.DOKill();
        }
    }
}
