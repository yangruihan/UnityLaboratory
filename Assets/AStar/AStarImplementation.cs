using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AStarImplementation : MonoBehaviour
{
    public Transform Unit;
    public float UnitSize = 0.5f;
    public float UnitSpace = 0.1f;

    private void Start()
    {
        var width = 16;
        var height = 16;
        var cost = new int[height][];
        for (var i = 0; i < width; i++)
        {
            cost[i] = new int[height];
            for (var j = 0; j < height; j++)
                cost[i][j] = 0;
        }

        for (var i = 3; i < 6; i++)
            for (var j = 3; j < 12; j++)
                cost[i][j] = 10;
            
        var gridGraph = new GridGraph(16, 16, cost);
        
        for (var i = 0; i < gridGraph.GetWidth(); i++)
        {
            for (var j = 0; j < gridGraph.GetHeight(); j++)
            {
                var unit = Instantiate(Unit,
                    new Vector3(
                        i * UnitSize + (i - 1) * UnitSpace,
                        j * UnitSize + (j - 1) * UnitSpace,
                        0),
                    Quaternion.identity);
                unit.localScale = new Vector3(UnitSize, UnitSize, 1);
                unit.gameObject.SetActive(true);

                var t = unit.GetComponentInChildren<TextMeshProUGUI>();
                var c = gridGraph.GetCost(Pos.Create(i, j));
                t.text = c.ToString();
                if (c >= 10)
                {
                    var spriteRenderer = unit.GetComponent<SpriteRenderer>();
                    spriteRenderer.color = Color.red;
                }
            }
        }
    }
}

public struct Pos
{
    public int X;
    public int Y;

    public static Pos Create(int x, int y)
    {
        return new Pos(x, y);
    }
    
    public Pos(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Eq(Pos other)
    {
        return other.X == X && other.Y == Y;
    }
}

public interface PathFinder
{
    List<Pos> Find(Graph g, Pos start, Pos goal);
}

public abstract class Graph
{
    public abstract int GetHeight();
    public abstract int GetWidth();
    public abstract List<Pos> GetNeighbors(Pos p);
    public abstract int GetCost(Pos p);
}

public class GridGraph : Graph
{
    private readonly int _width;
    private readonly int _height;
    private readonly int[][] _cost;

    public GridGraph(int width, int height, int[][] cost)
    {
        _width = width;
        _height = height;
        _cost = cost;
    }

    public override int GetHeight()
    {
        return _height;
    }

    public override int GetWidth()
    {
        return _width;
    }

    public override List<Pos> GetNeighbors(Pos p)
    {
        var ret = new List<Pos>();

        if (p.X - 1 >= 0)
            ret.Add(new Pos(p.X - 1, p.Y));

        if (p.X + 1 < _width)
            ret.Add(new Pos(p.X + 1, p.Y));

        if (p.Y - 1 >= 0)
            ret.Add(new Pos(p.X, p.Y - 1));

        if (p.Y + 1 < _height)
            ret.Add(new Pos(p.X, p.Y + 1));

        return ret;
    }

    public override int GetCost(Pos p)
    {
        return _cost[p.X][p.Y];
    }
}

/// <summary>
/// 广度优先搜索算法
/// </summary>
public class BFSPathFinder : PathFinder
{
    private readonly Queue<Pos> _frontier = new Queue<Pos>();
    private readonly Dictionary<Pos, Pos> _came_from = new Dictionary<Pos, Pos>();

    public List<Pos> Find(Graph g, Pos start, Pos goal)
    {
        var ret = new List<Pos>();

        _frontier.Enqueue(start);

        while (_frontier.Count > 0)
        {
            var current = _frontier.Dequeue();

            if (current.Eq(goal))
                break;

            foreach (var neighbor in g.GetNeighbors(current))
            {
                if (!_came_from.ContainsKey(neighbor))
                {
                    _came_from.Add(neighbor, current);
                    _frontier.Enqueue(neighbor);
                }
            }
        }

        var c = goal;
        while (!c.Eq(start))
        {
            ret.Add(c);
            c = _came_from[c];
        }

        ret.Add(start);

        _frontier.Clear();
        _came_from.Clear();

        return ret;
    }
}