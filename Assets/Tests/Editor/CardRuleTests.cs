using System.Collections.Generic;
using GridBattle;
using NUnit.Framework;
using UnityEngine;

/// <summary>Luật lá phép tính thuần: đường đánh qua cổng Rift, vùng nổ Suicide Bomb. Chạy: Test Runner > EditMode.</summary>
public class CardRuleTests
{
    private static bool InBoard(Vector2Int c) => c.x >= 0 && c.y >= 0 && c.x < 10 && c.y < 10;
    private static readonly Vector2Int Up = new(0, 1);

    private static Dictionary<Vector2Int, Vector2Int> Rift(Vector2Int a, Vector2Int b) => new() { [a] = b, [b] = a };

    [Test]
    public void NoPortalWalksStraight()
    {
        CollectionAssert.AreEqual(new[] { new Vector2Int(6, 3), new Vector2Int(6, 4), new Vector2Int(6, 5) },
            AttackRange.Walk(new Vector2Int(6, 3), Up, 3, InBoard));
    }

    [Test]
    public void PortalCellIsFreeAndRangeContinuesFromExit()
    {
        // Mage tầm 4 ở (6,3) quay lên, cổng A (6,5) - B (2,8): A không bị đánh, không tốn tầm.
        var cells = AttackRange.Walk(new Vector2Int(6, 3), Up, 4, InBoard, Rift(new Vector2Int(6, 5), new Vector2Int(2, 8)));
        CollectionAssert.AreEqual(new[] { new Vector2Int(6, 3), new Vector2Int(6, 4), new Vector2Int(2, 8), new Vector2Int(2, 9) }, cells);
    }

    [Test]
    public void PortalIsTwoWay()
    {
        var cells = AttackRange.Walk(new Vector2Int(2, 7), Up, 3, InBoard, Rift(new Vector2Int(6, 5), new Vector2Int(2, 8)));
        CollectionAssert.AreEqual(new[] { new Vector2Int(2, 7), new Vector2Int(6, 5), new Vector2Int(6, 6) }, cells);
    }

    [Test]
    public void LoopingPortalsTerminate()
    {
        // B nằm ngay trước A theo hướng đánh: không giới hạn thì A -> B -> A ... mãi.
        var cells = AttackRange.Walk(new Vector2Int(0, 0), Up, 4, InBoard, Rift(new Vector2Int(0, 2), new Vector2Int(0, 1)));
        Assert.AreEqual(4, cells.Count);
    }

    [Test]
    public void BombSizeIsOddAndGrows()
    {
        CollectionAssert.AreEqual(new[] { 3, 3, 5, 5, 7, 7 },
            new[] { Phases.BombSize(1), Phases.BombSize(2), Phases.BombSize(3), Phases.BombSize(4), Phases.BombSize(5), Phases.BombSize(9) });
    }
}
