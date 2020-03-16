using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class AStarImplementation : MonoBehaviour
{
    public Transform Unit;
    public float UnitSize = 0.5f;
    public float UnitSpace = 0.1f;

    private int[][] _cost;
    private Pos _start;
    private Pos _goal;

    private void Start()
    {
        var width = 16;
        var height = 16;
        _cost = new int[height][];
        for (var i = 0; i < width; i++)
        {
            _cost[i] = new int[height];
            for (var j = 0; j < height; j++)
                _cost[i][j] = Random.Range(1, 100);
        }

        for (var i = 0; i < 80; i++)
        {
            _cost[Random.Range(0, 15)][Random.Range(0, 15)] = int.MaxValue;
        }

        _start = Pos.Create(0, 0);
        _goal = Pos.Create(12, 14);

        _cost[_start.X][_start.Y] = 0;
        _cost[_goal.X][_goal.Y] = 1;

        Show(_start, _goal, _cost);
    }

    private void Show(Pos start, Pos goal, int[][] cost)
    {
        var objs = GameObject.FindGameObjectsWithTag("Unit");
        foreach (var o in objs)
            Destroy(o);

        // ----- BFS -----

        var gridGraph = new GridGraph(16, 16, cost);
        IPathFinder pathFinder = new BfsPathFinder();
        var path = pathFinder.Find(gridGraph, start, goal);

        var units = new List<Transform>();
        SpawnUnit(gridGraph, Vector2.zero, ref units);
        DrawGrid(gridGraph, units, Color.white);
        DrawPath(gridGraph, path, units);

        var sumCost = 0;
        foreach (var p in path)
            sumCost += cost[p.X][p.Y];

        Debug.LogError($"BFS Sum Cost: {sumCost}, Sum Step: {path.Count}");

        // ----- Dijkstra -----

        pathFinder = new DijkstraPathFinder();
        path = pathFinder.Find(gridGraph, start, goal);

        units = new List<Transform>();
        SpawnUnit(gridGraph, new Vector2(10, 0), ref units);
        DrawGrid(gridGraph, units, Color.yellow);
        DrawPath(gridGraph, path, units);

        sumCost = 0;
        foreach (var p in path)
            sumCost += cost[p.X][p.Y];

        Debug.LogError($"Dijkstra Sum Cost: {sumCost}, Sum Step: {path.Count}");

        // ----- GreedyBestFirst -----

        pathFinder = new GreedyBestFirstPathFinder();
        path = pathFinder.Find(gridGraph, start, goal);

        units = new List<Transform>();
        SpawnUnit(gridGraph, new Vector2(0, 10), ref units);
        DrawGrid(gridGraph, units, Color.grey);
        DrawPath(gridGraph, path, units);

        sumCost = 0;
        foreach (var p in path)
            sumCost += cost[p.X][p.Y];

        Debug.LogError($"GreedyBestFirst Sum Cost: {sumCost}, Sum Step: {path.Count}");

        // ----- AStar -----

        pathFinder = new AStarPathFinder();
        path = pathFinder.Find(gridGraph, start, goal);

        units = new List<Transform>();
        SpawnUnit(gridGraph, new Vector2(10, 10), ref units);
        DrawGrid(gridGraph, units, Color.magenta);
        DrawPath(gridGraph, path, units);

        sumCost = 0;
        foreach (var p in path)
            sumCost += cost[p.X][p.Y];

        Debug.LogError($"AStar Sum Cost: {sumCost}, Sum Step: {path.Count}");
    }

    private void SpawnUnit(Graph graph, Vector2 offset, ref List<Transform> units)
    {
        units.Clear();
        for (var j = 0; j < graph.GetHeight(); j++)
        {
            for (var i = 0; i < graph.GetWidth(); i++)
            {
                var unit = Instantiate(Unit,
                    new Vector3(
                        i * UnitSize + (i - 1) * UnitSpace + offset.x,
                        j * UnitSize + (j - 1) * UnitSpace + offset.y,
                        0),
                    Quaternion.identity);
                unit.name = $"{i},{j}";
                units.Add(unit);
                var btn = unit.GetComponentInChildren<Button>();
                btn.onClick.AddListener(() =>
                {
                    var goals = unit.name.Split(',');
                    var pos = Pos.Create(int.Parse(goals[0]), int.Parse(goals[1]));

                    if (Input.GetKey(KeyCode.W))
                    {
                        if (_cost[pos.X][pos.Y] == int.MaxValue)
                        {
                            _cost[pos.X][pos.Y] = Random.Range(1, 100);
                        }
                        else
                        {
                            _cost[pos.X][pos.Y] = int.MaxValue;
                        }
                    }
                    else
                    {
                        _goal = pos;
                    }

                    Show(_start, _goal, _cost);
                });
            }
        }
    }

    private void DrawGrid(Graph graph, List<Transform> units, Color normalColor)
    {
        for (var i = 0; i < graph.GetWidth(); i++)
        {
            for (var j = 0; j < graph.GetHeight(); j++)
            {
                var unit = units[i + j * graph.GetWidth()];

                unit.localScale = new Vector3(UnitSize, UnitSize, 1);
                unit.gameObject.SetActive(true);

                var t = unit.GetComponentInChildren<TextMeshProUGUI>();
                var c = graph.GetCost(Pos.Create(i, j));
                t.text = c == int.MaxValue ? "∞" : c.ToString();
                var spriteRenderer = unit.GetComponent<SpriteRenderer>();
                spriteRenderer.color = c == int.MaxValue ? Color.red : normalColor;
            }
        }
    }

    private void DrawPath(Graph graph, List<Pos> path, List<Transform> units)
    {
        foreach (var p in path)
        {
            var unit = units[p.X + p.Y * graph.GetWidth()];
            var spriteRenderer = unit.GetComponent<SpriteRenderer>();
            spriteRenderer.color = Color.green;
        }

        var startUnit = units[_start.X + _start.Y * graph.GetWidth()];
        var goalUnit = units[_goal.X + _goal.Y * graph.GetWidth()];
        startUnit.GetComponent<SpriteRenderer>().color = Color.blue;
        goalUnit.GetComponent<SpriteRenderer>().color = Color.blue;
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

    public override string ToString()
    {
        return $"({X}, {Y})";
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
    public abstract bool Passable(Pos p);

    public List<Pos> GetPath(Dictionary<Pos, Pos> cameFrom, Pos start, Pos goal)
    {
        var ret = new List<Pos>();
        var c = goal;
        while (!c.Eq(start))
        {
            ret.Add(c);

            if (!cameFrom.ContainsKey(c))
            {
                ret.Clear();
                break;
            }

            c = cameFrom[c];
        }

        return ret;
    }
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

    public override bool Passable(Pos p)
    {
        return _cost[p.X][p.Y] != int.MaxValue;
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
        var neighbors = new List<Pos>();

        _frontier.Enqueue(start);

        while (_frontier.Count > 0)
        {
            var current = _frontier.Dequeue();

            if (current.Eq(goal))
                break;

            foreach (var neighbor in g.GetNeighbors(current, ref neighbors))
            {
                if (!g.Passable(neighbor))
                    continue;

                if (!_cameFrom.ContainsKey(neighbor))
                {
                    _cameFrom.Add(neighbor, current);
                    _frontier.Enqueue(neighbor);
                }
            }
        }

        var ret = g.GetPath(_cameFrom, start, goal);

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
                if (!g.Passable(neighbor))
                    continue;

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

        var ret = g.GetPath(_cameFrom, start, goal);

        _frontier.Clear();

        _cameFrom.Clear();
        _costSoFar.Clear();

        return ret;
    }
}

public class GreedyBestFirstPathFinder : IPathFinder
{
    private readonly PriorityQueue<Pos> _frontier = new PriorityQueue<Pos>();
    private readonly Dictionary<Pos, Pos> _cameFrom = new Dictionary<Pos, Pos>();
    private readonly Dictionary<Pos, double> _costSoFar = new Dictionary<Pos, double>();

    private double Heuristic(Pos a, Pos b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }

    public List<Pos> Find(Graph g, Pos start, Pos goal)
    {
        var neighbors = new List<Pos>();

        _frontier.Enqueue(start, 0);
        _costSoFar.Add(start, Heuristic(start, goal));


        while (_frontier.Count > 0)
        {
            var current = _frontier.Dequeue();

            if (current.Eq(goal))
                break;

            foreach (var neighbor in g.GetNeighbors(current, ref neighbors))
            {
                if (!g.Passable(neighbor))
                    continue;

                var newCost = _costSoFar[current] + Heuristic(neighbor, goal);
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

        var ret = g.GetPath(_cameFrom, start, goal);

        ret.Add(start);

        _frontier.Clear();
        _cameFrom.Clear();
        _costSoFar.Clear();

        return ret;
    }
}

public class AStarPathFinder : IPathFinder
{
    private readonly PriorityQueue<Pos> _frontier = new PriorityQueue<Pos>();
    private readonly Dictionary<Pos, Pos> _cameFrom = new Dictionary<Pos, Pos>();
    private readonly Dictionary<Pos, double> _costSoFar = new Dictionary<Pos, double>();

    private double Heuristic(Pos a, Pos b)
    {
        return Mathf.Abs(a.X - b.X) + Mathf.Abs(a.Y - b.Y);
    }

    public List<Pos> Find(Graph g, Pos start, Pos goal)
    {
        var neighbors = new List<Pos>();

        _frontier.Enqueue(start, 0);
        _costSoFar.Add(start, Heuristic(start, goal));


        while (_frontier.Count > 0)
        {
            var current = _frontier.Dequeue();

            if (current.Eq(goal))
                break;

            foreach (var neighbor in g.GetNeighbors(current, ref neighbors))
            {
                if (!g.Passable(neighbor))
                    continue;

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

                    _frontier.Enqueue(neighbor, newCost + Heuristic(neighbor, goal));
                }
            }
        }

        var ret = g.GetPath(_cameFrom, start, goal);

        ret.Add(start);

        _frontier.Clear();
        _cameFrom.Clear();
        _costSoFar.Clear();

        return ret;
    }
}