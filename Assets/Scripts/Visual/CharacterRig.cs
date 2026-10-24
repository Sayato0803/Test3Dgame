using UnityEngine;

namespace GridBattle
{
    /// <summary>
    /// Điều khiển xương + mắt của King/Knight/Mage (chung 1 bộ rig: Bone.009 = cổ, Bone.003/004 = tay cầm vũ khí,
    /// *_EyeL/R + *_PupilL/R gắn vào đầu). Model không có Animator nên ghi đè trong LateUpdate, mỗi frame tính lại từ tư thế gốc.
    /// - Chớp mắt ngẫu nhiên; con ngươi nhìn theo LookTarget (để trống = con trỏ, Pointer).
    ///   Đang bị cầm (Panic): nhìn người chơi (camera) nếu đang quay mặt về phía camera, quay hướng khác thì nhìn thẳng.
    /// - Con ngươi chạm mép mắt mà mục tiêu còn lệch nữa thì xoay đầu bù phần còn thiếu.
    /// - Panic (bị nhấc lên): mắt trợn to, con ngươi co lại.
    /// - Angry: lườm (con ngươi nhỏ), lông mày xếch, dấu gân nổi 💢 ở thái dương, giơ vũ khí lên, đầu run.
    /// </summary>
    public class CharacterRig : MonoBehaviour
    {
        /// <summary>Điểm con trỏ chuột trong thế giới (GridPlacer cập nhật mỗi frame) - con ngươi các quân nhìn theo.</summary>
        public static Vector3? Pointer;

        public Vector3? LookTarget;
        public bool Panic;
        public bool Angry;

        /// <summary>Góc nhìn (độ) con ngươi tự với tới được; lệch hơn thì đầu xoay theo.</summary>
        private const float EyeReach = 25f;
        private const float MaxHeadTurn = 55f;

        private class Part
        {
            public Transform T;
            public Vector3 Pos, Scale, Center; // Center: tâm mesh trong không gian object (pivot mesh mắt nằm ở xương đầu, không ở giữa mắt)
            public Quaternion Rot;
            public int UpAxis;                 // trục local gần với "lên" nhất -> co trục này để chớp / nheo mắt
        }

        /// <summary>
        /// Mặt trước của mắt (đĩa dẹt viền bo) gần đúng là 1 chỏm cầu; con ngươi đã được uốn cong ôm theo chỏm đó trong Blender.
        /// Xoay con ngươi quanh TÂM CHỎM CẦU (không phải tâm mắt) thì nó luôn nằm trên bề mặt, không chìm vào mắt.
        /// Mọi toạ độ trong không gian mesh của mắt, đo ở tư thế nghỉ.
        /// </summary>
        private class EyeShape
        {
            public Vector3 Dome;              // tâm chỏm cầu
            public float DomeRadius;
            public Vector3 FrontAxis;         // hướng ra trước của mắt
            public Vector3 PupilRest;         // tâm con ngươi ở tư thế nghỉ
            public Vector3 RestDir;           // từ tâm chỏm tới tâm con ngươi ở tư thế nghỉ
            public float PupilRadius;
            public float MaxAngle;            // độ lệch tối đa (độ) để con ngươi không trượt ra khỏi mép mắt
        }

        private Transform _neck, _arm, _armTip;
        private Quaternion _neckRest, _armRest;
        private Quaternion _headTurn = Quaternion.identity;
        private Vector3 _faceLocal;            // hướng mặt trong không gian xương cổ
        private Vector3 _outLocal;             // hướng từ thân ra phía tay cầm vũ khí (không gian gốc), đo ở tư thế nghỉ
        private Renderer _head;
        private Part[] _eyes, _pupils;
        private EyeShape[] _shapes;
        private Transform[] _brows;
        private Transform _vein;
        private float _eyeDiscRadius;
        private float _nextBlink, _blinkStart = -1f;
        private float _panic, _angry;

        private void Awake()
        {
            Transform eyeL = null, eyeR = null, pupilL = null, pupilR = null;
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name;
                if (n == "Bone.009") _neck = t;
                else if (n == "Bone.003") _arm = t;
                else if (n == "Bone.004") _armTip = t;
                else if (n.EndsWith("_EyeL")) eyeL = t;
                else if (n.EndsWith("_EyeR")) eyeR = t;
                else if (n.EndsWith("_PupilL")) pupilL = t;
                else if (n.EndsWith("_PupilR")) pupilR = t;
                else if (n.EndsWith("_Head")) _head = t.GetComponent<Renderer>();
            }
            if (_neck != null) _neckRest = _neck.localRotation;
            if (_arm != null) _armRest = _arm.localRotation;
            if (eyeL != null && eyeR != null && pupilL != null && pupilR != null)
            {
                _eyes = new[] { MakePart(eyeL), MakePart(eyeR) };
                _pupils = new[] { MakePart(pupilL), MakePart(pupilR) };
                _shapes = new[] { MeasureEye(eyeL, pupilL), MeasureEye(eyeR, pupilR) };
                Bounds eb = eyeL.GetComponent<Renderer>().bounds;
                _eyeDiscRadius = Mathf.Max(eb.extents.x, eb.extents.y, eb.extents.z);
                if (_neck != null)
                {
                    Vector3 eyesMid = (CenterOf(_eyes[0]) + CenterOf(_eyes[1])) * 0.5f;
                    Vector3 headCenter = _head != null ? _head.bounds.center : _neck.position;
                    Vector3 face = Vector3.ProjectOnPlane(eyesMid - headCenter, Vector3.up);
                    if (face.sqrMagnitude < 1e-6f) face = transform.forward;
                    _faceLocal = _neck.InverseTransformDirection(face.normalized);
                }
            }
            if (_armTip != null)
            {
                // Xương vai nằm gần giữa ngực -> đo hướng "ra ngoài" bằng khuỷu tay.
                Vector3 bodyCenter = _neck != null ? _neck.position : transform.position;
                _outLocal = transform.InverseTransformDirection(Vector3.ProjectOnPlane(_armTip.position - bodyCenter, Vector3.up).normalized);
            }
            _nextBlink = Time.time + Random.Range(1.5f, 4f);
        }

        private static Part MakePart(Transform t)
        {
            var p = new Part { T = t, Pos = t.localPosition, Rot = t.localRotation, Scale = t.localScale };
            var mf = t.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null) p.Center = mf.sharedMesh.bounds.center;
            float best = -1f;
            for (int i = 0; i < 3; i++)
            {
                float d = Mathf.Abs(Vector3.Dot(t.TransformDirection(Axis(i, 1f)), Vector3.up));
                if (d > best) { best = d; p.UpAxis = i; }
            }
            return p;
        }

        private static EyeShape MeasureEye(Transform eye, Transform pupil)
        {
            Mesh em = eye.GetComponent<MeshFilter>().sharedMesh;
            Bounds eb = em.bounds, pb = pupil.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 ext = eb.extents;
            int thin = ext.x <= ext.y && ext.x <= ext.z ? 0 : ext.y <= ext.z ? 1 : 2;
            Vector3 pupilCenter = eye.InverseTransformPoint(pupil.TransformPoint(pb.center));
            float front = (pupilCenter - eb.center)[thin] >= 0f ? 1f : -1f;
            Vector3 frontAxis = Axis(thin, front);
            float discRadius = Mathf.Max(ext[(thin + 1) % 3], ext[(thin + 2) % 3]);

            // Khớp chỏm cầu tâm nằm trên trục mỏng: (z - zc)^2 + r^2 = R^2  <=>  2*z*zc + K = z^2 + r^2  (K = R^2 - zc^2).
            double s11 = 0, s12 = 0, s22 = 0, b1 = 0, b2 = 0;
            Vector3[] verts = em.isReadable ? em.vertices : new Vector3[0];
            foreach (Vector3 v0 in verts)
            {
                Vector3 v = v0 - eb.center;
                float z = v[thin] * front;
                float r2 = v.sqrMagnitude - v[thin] * v[thin];
                if (z <= 0f || r2 > discRadius * discRadius * 0.5f) continue; // chỉ lấy phần giữa mặt trước
                double rhs = z * z + r2;
                s11 += 4.0 * z * z; s12 += 2.0 * z; s22 += 1.0;
                b1 += 2.0 * z * rhs; b2 += rhs;
            }
            double det = s11 * s22 - s12 * s12;
            float zc = 0f, radius = ext[thin];
            if (s22 > 3 && System.Math.Abs(det) > 1e-30)
            {
                zc = (float)((b1 * s22 - b2 * s12) / det);
                float k = (float)((s11 * b2 - s12 * b1) / det);
                radius = Mathf.Sqrt(Mathf.Max(1e-12f, k + zc * zc));
            }

            var g = new EyeShape { FrontAxis = frontAxis, Dome = eb.center + frontAxis * zc, DomeRadius = radius, PupilRest = pupilCenter };
            g.RestDir = (pupilCenter - g.Dome).normalized;
            g.PupilRadius = eye.InverseTransformVector(pupil.TransformVector(new Vector3(Mathf.Max(pb.extents.x, pb.extents.y, pb.extents.z), 0f, 0f))).magnitude;
            // Con ngươi trượt tới khi mép nó còn cách mép mắt ~30% khoảng trống (sát mép mắt cong gắt hơn chỏm cầu).
            float room = Mathf.Max(0f, discRadius - g.PupilRadius) * 0.7f;
            g.MaxAngle = Mathf.Asin(Mathf.Clamp01(room / radius)) * Mathf.Rad2Deg;
            return g;
        }

        /// <summary>Xoay con ngươi quanh tâm chỏm cầu của mắt về phía 'target' (giới hạn trong mép mắt).</summary>
        private static void PlacePupil(Part eye, Part pupil, EyeShape g, Vector3 target)
        {
            Vector3 dir = eye.T.InverseTransformPoint(target) - g.Dome;
            if (dir.sqrMagnitude < 1e-12f) return;
            Vector3 want = Vector3.RotateTowards(g.FrontAxis, dir.normalized, g.MaxAngle * Mathf.Deg2Rad, 0f);
            Quaternion q = Quaternion.FromToRotation(g.RestDir, want);
            q.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle < 0.01f || float.IsInfinity(axis.x)) return;
            pupil.T.RotateAround(eye.T.TransformPoint(g.Dome), eye.T.TransformDirection(axis), angle);
        }

        private static Vector3 CenterOf(Part p) => p.T.TransformPoint(p.Center);

        /// <summary>Vector chỉ có thành phần thứ i = v.</summary>
        private static Vector3 Axis(int i, float v)
        {
            Vector3 a = Vector3.zero;
            a[i] = v;
            return a;
        }

        /// <summary>Đặt scale (nhân với scale gốc) mà giữ nguyên tâm hình học của mesh.</summary>
        private static void ScaleAroundCenter(Part p, Vector3 mul)
        {
            Vector3 s = Vector3.Scale(p.Scale, mul);
            Vector3 center = p.Pos + p.Rot * Vector3.Scale(p.Scale, p.Center);
            p.T.localRotation = p.Rot;
            p.T.localScale = s;
            p.T.localPosition = center - p.Rot * Vector3.Scale(s, p.Center);
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            float k = 1f - Mathf.Exp(-12f * dt);
            _panic = Mathf.Lerp(_panic, Panic ? 1f : 0f, k);
            _angry = Mathf.Lerp(_angry, Angry ? 1f : 0f, k);
            Camera cam = Camera.main;
            Vector3? target = LookTarget ?? Pointer;
            if (Panic)
            {
                // Bị cầm lên: nhìn người chơi nếu đang quay mặt về phía camera; bị xoay quay hướng khác thì nhìn thẳng phía trước.
                bool facingCamera = cam != null && Vector3.Dot(Vector3.ProjectOnPlane(transform.forward, Vector3.up),
                    Vector3.ProjectOnPlane(cam.transform.position - transform.position, Vector3.up)) > 0f;
                target = facingCamera ? cam.transform.position : null;
            }

            if (_neck != null)
            {
                _neck.localRotation = _neckRest;
                // Đầu chỉ xoay phần góc vượt quá tầm con ngươi: nhìn hơi lệch thì chỉ đảo mắt, lệch nhiều mới quay đầu.
                Quaternion want = Quaternion.identity;
                if (target.HasValue)
                {
                    Vector3 face = _neck.TransformDirection(_faceLocal);
                    Vector3 to = target.Value - _neck.position;
                    float excess = Mathf.Min(Vector3.Angle(face, to) - EyeReach, MaxHeadTurn);
                    if (excess > 0f) want = Quaternion.RotateTowards(Quaternion.identity, Quaternion.FromToRotation(face, to), excess);
                }
                _headTurn = Quaternion.Slerp(_headTurn, want, 1f - Mathf.Exp(-8f * dt));
                _neck.rotation = _headTurn * _neck.rotation;
                if (_angry > 0.01f) // run lên vì tức
                    _neck.rotation = Quaternion.Euler(Random.insideUnitSphere * 3f * _angry) * _neck.rotation;
            }

            if (_arm != null)
            {
                _arm.localRotation = _armRest;
                if (_angry > 0.01f && _armTip != null)
                {
                    // Giơ thẳng vũ khí lên trời, hơi chếch ra ngoài vai và ra trước mặt.
                    Vector3 face = _neck != null ? _neck.TransformDirection(_faceLocal) : transform.forward;
                    Vector3 outward = transform.TransformDirection(_outLocal);
                    Vector3 wantArm = (Vector3.up + outward * 0.7f + face * 0.2f).normalized;
                    Quaternion raise = Quaternion.FromToRotation(_armTip.position - _arm.position, wantArm);
                    _arm.rotation = Quaternion.Slerp(Quaternion.identity, raise, _angry) * _arm.rotation;
                }
            }

            if (_eyes == null) return;

            // Chớp mắt: co trục dọc của mắt xuống gần 0 trong 0.12s.
            float blink = 1f;
            if (_blinkStart < 0f && Time.time >= _nextBlink) _blinkStart = Time.time;
            if (_blinkStart >= 0f)
            {
                float t = (Time.time - _blinkStart) / 0.12f;
                if (t >= 1f) { _blinkStart = -1f; _nextBlink = Time.time + Random.Range(2f, 5f); }
                else blink = Mathf.Lerp(1f, 0.08f, 1f - Mathf.Abs(t * 2f - 1f));
            }

            float eyeSize = 1f + 0.3f * _panic;
            // Không nheo mắt khi tức: co mắt không đều làm mặt mắt hết là chỏm cầu -> con ngươi lún. Lườm bằng con ngươi nhỏ.
            float vertical = blink;
            float pupilSize = 1f - Mathf.Max(0.4f * _panic, 0.3f * _angry);
            for (int i = 0; i < 2; i++)
            {
                Part eye = _eyes[i], pupil = _pupils[i];
                EyeShape g = _shapes[i];
                Vector3 m = Vector3.one * eyeSize;
                m[eye.UpAxis] *= vertical;
                ScaleAroundCenter(eye, m);
                m = Vector3.one * eyeSize * pupilSize;
                m[pupil.UpAxis] *= vertical;
                ScaleAroundCenter(pupil, m);
                // Con ngươi bám theo mắt vừa co giãn (mắt to ra / nheo lại thì con ngươi cũng dời theo, không tụt vào trong).
                pupil.T.position += eye.T.TransformPoint(g.PupilRest) - CenterOf(pupil);
                if (pupilSize < 0.999f)
                {
                    // Con ngươi nhỏ lại thì chỏm cong gắt hơn mặt mắt -> mép lún xuống; đẩy ra theo pháp tuyến bù phần lún.
                    float sag = g.PupilRadius * g.PupilRadius / (2f * g.DomeRadius);
                    float push = ((1f - pupilSize) * 0.5f + pupilSize * (1f - pupilSize)) * sag;
                    pupil.T.position += eye.T.TransformVector(g.RestDir * push);
                }
                if (target.HasValue) PlacePupil(eye, pupil, g, target.Value);
            }

            UpdateBrows();
            UpdateVein();
        }

        /// <summary>Lông mày xếch (tạo lần đầu khi tức): 2 thanh bo tròn đè lên mép trên của mắt, đầu phía mũi chúi xuống.</summary>
        private void UpdateBrows()
        {
            if (_brows == null)
            {
                if (_angry < 0.05f) return;
                var mat = new Material(Shader.Find("Sprites/Default")) { color = new Color(0.12f, 0.07f, 0.05f) };
                Vector3 up = Vector3.up;
                Vector3 face = _neck != null ? _neck.TransformDirection(_faceLocal) : transform.forward;
                Vector3 mid = (CenterOf(_eyes[0]) + CenterOf(_eyes[1])) * 0.5f;
                _brows = new Transform[2];
                for (int i = 0; i < 2; i++)
                {
                    GameObject b = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    Destroy(b.GetComponent<Collider>());
                    b.name = "AngryBrow";
                    b.GetComponent<Renderer>().sharedMaterial = mat;
                    Vector3 c = CenterOf(_eyes[i]);
                    float side = Vector3.Dot(c - mid, Vector3.Cross(up, face)) >= 0f ? 1f : -1f;
                    b.transform.SetParent(_eyes[i].T.parent, true);
                    b.transform.position = c + up * _eyeDiscRadius * 0.55f + face * _eyeDiscRadius * 0.6f; // đè mép trên của mắt (lọt trong khe mũ giáp Knight)
                    // Capsule nằm ngang (trục dài = X), đầu phía mũi chúi xuống -> mặt cau có.
                    b.transform.rotation = Quaternion.LookRotation(face, up) * Quaternion.Euler(0f, 0f, 90f + side * 28f);
                    _brows[i] = b.transform;
                }
            }
            float s = _eyeDiscRadius * Mathf.Clamp01(_angry);
            foreach (Transform b in _brows)
            {
                b.gameObject.SetActive(s > _eyeDiscRadius * 0.05f);
                b.localScale = Vector3.one; // đặt kích thước theo thế giới, bỏ ảnh hưởng scale của xương
                Vector3 ls = b.lossyScale;
                // Capsule gốc cao 2, rộng 1: dài ~1.1 bán kính mắt, dày ~0.3.
                b.localScale = new Vector3(s * 0.3f / ls.x, s * 0.55f / ls.y, s * 0.3f / ls.z);
            }
        }

        /// <summary>Dấu gân nổi 💢 kiểu manga cạnh đầu khi tức, đập phập phồng.</summary>
        private void UpdateVein()
        {
            if (_vein == null)
            {
                if (_angry < 0.05f || _head == null) return;
                Sprite sprite = Resources.Load<Sprite>("GUI/fx/anger_vein");
                if (sprite == null) return;
                var go = new GameObject("AngerVein");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 50;
                go.AddComponent<Billboard>();
                _vein = go.transform;
                _vein.SetParent(_neck != null ? _neck : transform, true);
            }
            bool show = _angry > 0.05f && _head != null;
            _vein.gameObject.SetActive(show);
            if (!show) return;
            Bounds hb = _head.bounds;
            Vector3 face = _neck != null ? _neck.TransformDirection(_faceLocal) : transform.forward;
            Vector3 side = Vector3.Cross(Vector3.up, face).normalized;
            // Thái dương, kéo về phía camera để không bị mũ giáp / vành mũ che.
            Vector3 temple = hb.center + Vector3.up * hb.extents.y * 0.45f + side * hb.extents.x * 0.85f + face * hb.extents.z * 0.6f;
            Camera cam = Camera.main;
            if (cam != null) temple += (cam.transform.position - temple).normalized * hb.extents.z * 1.2f;
            _vein.position = temple;
            float size = hb.extents.y * 0.75f * _angry * (1f + 0.15f * Mathf.Sin(Time.unscaledTime * 14f));
            _vein.localScale = Vector3.one;
            Vector3 ls = _vein.lossyScale;
            float world = size / _vein.GetComponent<SpriteRenderer>().sprite.bounds.size.x;
            _vein.localScale = new Vector3(world / ls.x, world / ls.y, world / ls.z);
        }
    }
}
