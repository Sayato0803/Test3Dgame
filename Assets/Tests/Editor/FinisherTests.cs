using System.Collections.Generic;
using GridBattle;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Cảnh kết liễu: chọn quân kết liễu (Phases.PickFinishers) và dữ liệu clip mà code dựa vào
/// (event Fx, quãng văng đủ ra biển). Chạy: Window > General > Test Runner > EditMode.
/// </summary>
public class FinisherTests
{
    private const float UnitScale = 0.7f; // GridPlacer._unitScale trong GridTest: clip tính theo đơn vị local
    private readonly List<GameObject> _spawned = new();

    private GridUnit Unit(float x, float z)
    {
        var go = new GameObject("TestUnit");
        go.transform.position = new Vector3(x, 0f, z);
        _spawned.Add(go);
        return go.AddComponent<GridUnit>();
    }

    [TearDown]
    public void Cleanup()
    {
        foreach (GameObject go in _spawned) Object.DestroyImmediate(go);
        _spawned.Clear();
    }

    private static List<(GridUnit, List<GridUnit>)> Killed(params (GridUnit king, GridUnit[] hitters)[] kings)
    {
        var list = new List<(GridUnit, List<GridUnit>)>();
        foreach (var (king, hitters) in kings) list.Add((king, new List<GridUnit>(hitters)));
        return list;
    }

    [Test]
    public void NearestHitterFinishes()
    {
        GridUnit king = Unit(0f, 0f), far = Unit(3f, 0f), near = Unit(1f, 0f);
        var f = Phases.PickFinishers(Killed((king, new[] { far, near })));
        Assert.AreSame(near, f[king]);
    }

    [Test]
    public void OneUnitFinishesAtMostOneKing()
    {
        // 'shared' gần cả 2 King nhất, nhưng chỉ kết liễu King đầu; King sau lấy quân gần kế tiếp.
        GridUnit kingA = Unit(0f, 0f), kingB = Unit(0f, 4f), shared = Unit(0f, 1f), other = Unit(5f, 4f);
        var f = Phases.PickFinishers(Killed((kingA, new[] { shared }), (kingB, new[] { shared, other })));
        Assert.AreSame(shared, f[kingA]);
        Assert.AreSame(other, f[kingB]);
    }

    [Test]
    public void KingWithoutFreeHitterHasNoFinisher()
    {
        GridUnit kingA = Unit(0f, 0f), kingB = Unit(0f, 2f), only = Unit(0f, 1f);
        var f = Phases.PickFinishers(Killed((kingA, new[] { only }), (kingB, new[] { only })));
        Assert.AreSame(only, f[kingA]);
        Assert.IsFalse(f.ContainsKey(kingB)); // Phases cho King này văng không có đòn riêng
    }

    private static AnimationClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AnimationClip>($"Assets/Animations/{name}.anim");

    private static float EventTime(AnimationClip clip, string id)
    {
        foreach (AnimationEvent e in AnimationUtility.GetAnimationEvents(clip))
            if (e.stringParameter == id) return e.time;
        return -1f;
    }

    [TestCase("PowerSlash")]
    [TestCase("ZapCharge")]
    public void FinisherClipHitsThenEnds(string name)
    {
        AnimationClip clip = Clip(name);
        Assert.IsNotNull(clip);
        float impact = EventTime(clip, "impact"), end = EventTime(clip, "end");
        Assert.Greater(impact, 0f, "thiếu event Fx(\"impact\"): King không bị văng");
        Assert.Greater(end, impact, "thiếu event Fx(\"end\") sau impact: quân kẹt ở tư thế đánh");
    }

    [Test]
    public void KnockedOffReachesTheSea()
    {
        AnimationClip clip = Clip("KnockedOff");
        AnimationCurve z = AnimationUtility.GetEditorCurve(clip,
            EditorCurveBinding.FloatCurve("Visual", typeof(Transform), "m_LocalPosition.z"));
        // King xa nhất bị đánh về phía bàn kia: mép ngoài bàn z = 11 -> mép đảo z = -22 => phải bay >= 33m.
        Assert.GreaterOrEqual(z.Evaluate(clip.length) * UnitScale, 33f);
        Assert.GreaterOrEqual(EventTime(clip, "splash"), 0f);
    }

    // CharacterRig đọc vertex mắt để đặt con ngươi lên bề mặt nhãn cầu; tắt Read/Write thì con ngươi lún vào mắt.
    [TestCase("Assets/King.fbx")]
    [TestCase("Assets/Knight.fbx")]
    [TestCase("Assets/Mage.fbx")]
    public void UnitModelIsReadable(string path)
    {
        Assert.IsTrue(((ModelImporter)AssetImporter.GetAtPath(path)).isReadable, $"{path}: bật Read/Write (Import Settings > Model)");
    }

    [Test]
    public void CardDrawClipEnds()
    {
        AnimationClip clip = Clip("CardDraw");
        Assert.IsNotNull(clip);
        Assert.Greater(EventTime(clip, "end"), EventTime(clip, "flip"), "thiếu event DrawFx(\"end\"): hover lá bài bị clip ghi đè");
    }
}
