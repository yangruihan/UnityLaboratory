using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AStarImplementation : MonoBehaviour
{
    public Transform Unit;
    public float UnitSize = 0.5f;
    public float UnitSpace = 0.1f;

    private List<Transform> _units = new List<Transform>();

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
        // var pathFinder = new BfsPathFinder();
        var pathFinder = new DijkstraPathFinder();
        var path = pathFinder.Find(gridGraph, Pos.Create(0, 7), Pos.Create(15, 15));

        SpawnUnit(gridGraph);
        DrawGrid(gridGraph);
        DrawPath(gridGraph, path);
    }

    private void SpawnUnit(Graph graph)
    {
        _units.Clear();
        for (var j = 0; j < graph.GetHeight(); j++)
        {
            for (var i = 0; i < graph.GetWidth(); i++)
            {
                var unit = Instantiate(Unit,
                    new Vector3(
                        i * UnitSize + (i - 1) * UnitSpace,
                        j * UnitSize + (j - 1) * UnitSpace,
                        0),
                    Quaternion.identity);
                _units.Add(unit);
            }
        }
    }

    private void DrawGrid(Graph graph)
    {
        for (var i = 0; i < graph.GetWidth(); i++)
        {
            for (var j = 0; j < graph.GetHeight(); j++)
            {
                var unit = _units[i + j * graph.GetWidth()];

                unit.localScale = new Vector3(UnitSize, UnitSize, 1);
                unit.gameObject.SetActive(true);

                var t = unit.GetComponentInChildren<TextMeshProUGUI>();
                var c = graph.GetCost(Pos.Create(i, j));
                t.text = c.ToString();
                if (c >= 10)
                {
                    var spriteRenderer = unit.GetComponent<SpriteRenderer>();
                    spriteRenderer.color = Color.red;
                }
            }
        }
    }

    private void DrawPath(Graph graph, List<Pos> path)
    {
        foreach (var p in path)
        {
            var unit = _units[p.X + p.Y * graph.GetWidth()];
            var spriteRenderer = unit.GetComponent<SpriteRenderer>();
            spriteRenderer.color = Color.green;
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

public class PriorityQueue<T>
{
    private readonly List<Tuple<T, double>> _elements = new List<Tuple<T, double>>();

    public int Count => _elements.Count;

    public void Enqueue(T item, double priority)
    {
        _elements.Add(Tuple.Create(item, priority));
    }

    public T Dequeue()
    {
        var bestIndex = 0;

        for (var i = 0; i < _elements.Count; i++)
        {
            if (_elements[i].Item2 < _elements[bestIndex].Item2)
            {
                bestIndex = i;
            }
        }

        var bestItem = _elements[bestIndex].Item1;
        _elements.RemoveAt(bestIndex);
        return bestItem;
    }

    public void Clear()
    {
        _elements.Clear();
    }
}

public interface IPathFinder
{
    List<Pos> Find(Graph g, Pos start, Pos goal);
}

public abstract class Graph
{
    public abstract int GetHeight();
    public abstract int GetWidth();
    public abstract List<Pos> GetNeighbors(Pos p, ref List<Pos> neighbors);
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

    public override List<Pos> GetNeighbors(Pos p, ref List<Pos> neighbors)
    {
        if (neighbors == null)
            neighbors = new List<Pos>();
        else
            neighbors.Clear();

        if (p.X - 1 >= 0)
            neighbors.Add(new Pos(p.X - 1, p.Y));

        if (p.X + 1 < _width)
            neighbors.Add(new Pos(p.X + 1, p.Y));

        if (p.Y - 1 >= 0)
            neighbors.Add(new Pos(p.X, p.Y - 1));

        if (p.Y + 1 < _height)
            neighbors.Add(new Pos(p.X, p.Y + 1));

        return neighbors;
    }

    public override int GetCost(Pos p)
    {
        return _cost[p.X][p.Y];
    }
}

/// <summary>
/// 广度优先搜索算法
/// </summary>
public class BfsPathFinder : IPathFinder
{
    private readonly Queue<Pos> _frontier = new Queue<Pos>();
    private readonly Dictionary<Pos, Pos> _cameFrom = new Dictionary<Pos, Pos>();

    public List<Pos> Find(Graph g, Pos start, Pos goal)
    {
        var ret = new List<Pos>();
        var neighbors = new List<Pos>();

        _frontier.Enqueue(start);

        while (_frontier.Count > 0)
        {
            var current = _frontier.Dequeue();

            if (current.Eq(goal))
                break;

            foreach (var neighbor in g.GetNeighbors(current, ref neighbors))
            {
                if (!_cameFrom.ContainsKey(neighbor))
                {
                    _cameFrom.Add(neighbor, current);
                    _frontier.Enqueue(neighbor);
                }
            }
        }

        var c = goal;
        while (!c.Eq(start))
        {
            ret.Add(c);
            c = _cameFrom[c];
        }

        ret.Add(start);

        _frontier.Clear();
        _cameFrom.Clear();

        return ret;
    }
}

public class DijkstraPathFinder : IPathFinder
{
    private readonly PriorityQueue<Pos> _frontier = new PriorityQueue<Pos>();
    private readonly Dictionary<Pos, Pos> _cameFrom = new Dictionary<Pos, Pos>();
    private readonly Dictionary<Pos, int> _costSoFar = new Dictionary<Pos, int>();

    public List<Pos> Find(Graph g, Pos start, Pos goal)
    {
        var ret = new List<Pos>();
        var neignbors = new List<Pos>();

        _frontier.Enqueue(start, 0);
        _costSoFar.Add(start, 0);

        while (_frontier.Count > 0)
        {
            var current = _frontier.Dequeue();

            if (current.Eq(goal))
                break;

            foreach (var neighbor in g.GetNeighbors(current, ref neignbors))
            {
                var newCost = _costSoFar[current] + g.GetCost(neighbor);
                if (!_costSoFar.ContainsKey(neighbor) || _costSoFar[neighbor] > newCost)
                {
                    if (!_costSoFar.ContainsKey(neighbor))
                        _costSoFar.Add(neighbor, newCost);
                    else
                        _costSoFar[neighbor] = newCost;

                    if (!_cameFrom.ContainsKey(neighbor))
                        _cameFrom.Add(neighbor, current);
                    else
                        _cameFrom[neighbor] = current;

                    _frontier.Enqueue(neighbor, newCost);
                }
            }
        }
        
        var c = goal;
        while (!c.Eq(start))
        {
            ret.Add(c);
            c = _cameFrom[c];
        }

        ret.Add(start);

        _frontier.Clear();

        _cameFrom.Clear();
        _costSoFar.Clear();

        return ret;
    }
}