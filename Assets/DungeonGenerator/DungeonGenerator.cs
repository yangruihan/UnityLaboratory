using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    enum EColor
    {
        White,
        Red,
        Blue,
        Green
    }

    class UnitData
    {
        public int X;
        public int Y;
        public EColor Color;
        public SpriteRenderer SpriteRenderer;

        public void SetColor(EColor color)
        {
            Color = color;
            Color c = UnityEngine.Color.white;
            switch (color)
            {
                case EColor.Red:
                    c = UnityEngine.Color.red;
                    break;

                case EColor.Blue:
                    c = UnityEngine.Color.blue;
                    break;

                case EColor.Green:
                    c = UnityEngine.Color.green;
                    break;
            }

            SpriteRenderer.color = c;
        }
    }

    public Transform Unit;
    public float UnitSize = 0.5f;
    public float UnitSpace = 0.1f;

    public int MapWidth = 16;
    public int MapHeight = 16;

    public float RedRate = 0.2f;
    public float BlueRate = 0.3f;
    public float GreenRate = 0.5f;

    private int _sumCount;
    private int _redCount;
    private int _blueCount;
    private int _greenCount;

    private UnitData[][] _unitDatas;
    private List<UnitData> _redUnits = new List<UnitData>();
    private List<UnitData> _blueUnits = new List<UnitData>();
    private List<UnitData> _greenUnits = new List<UnitData>();

    private void Start()
    {
        InitData();

        // 1. 生成网格
        GenerateUnit();

        // 2. 染色
        ColorUnit();
        
        // 3. 选择两个节点，开辟一条通路
        
    }

    private void ColorUnit()
    {
        var neighbors = new List<UnitData>();

        // 1. 将x个网格涂成红色，保证没有红色网格相邻
        _redUnits.Clear();

        var redCount = _redCount;
        var tryMaxTimes = 0; // 避免死循环
        while (redCount > 0 && tryMaxTimes <= 1000)
        {
            var x = Random.Range(0, MapWidth);
            var y = Random.Range(0, MapHeight);

            // 已经涂过色了
            if (_unitDatas[x][y].Color != EColor.White)
                continue;

            GetNeighbor(x, y, ref neighbors);
            var canColor = true;
            foreach (var neighbor in neighbors)
            {
                if (neighbor.Color == EColor.Red)
                {
                    canColor = false;
                    break;
                }
            }

            if (canColor)
            {
                _unitDatas[x][y].SetColor(EColor.Red);
                _redUnits.Add(_unitDatas[x][y]);
                redCount--;
            }

            tryMaxTimes++;
        }

        // 2. 对于每一个红色网格，将其相邻的一个网格涂成蓝色，另一个相邻的网格涂成绿色
        foreach (var redUnit in _redUnits)
        {
            GetNeighbor(redUnit.X, redUnit.Y, ref neighbors);

            for (var i = neighbors.Count - 1; i >= 0; i--)
            {
                if (neighbors[i].Color != EColor.White)
                    neighbors.RemoveAt(i);
            }

            if (neighbors.Count > 0)
            {
                var i = Random.Range(0, neighbors.Count);
                neighbors[i].SetColor(EColor.Blue);
                neighbors.RemoveAt(i);
            }

            if (neighbors.Count > 0)
            {
                var i = Random.Range(0, neighbors.Count);
                neighbors[i].SetColor(EColor.Green);
                neighbors.RemoveAt(i);
            }
        }

        // 3. 用每种颜色剩余的数量将剩余网格随机上色
        var blueRemain = _blueCount - _blueUnits.Count;
        var greenRemain = _greenCount - _greenUnits.Count;
        
        for (var i = 0; i < MapWidth; i++)
        {
            for (var j = 0; j < MapHeight; j++)
            {
                var unit = _unitDatas[i][j];
                if (unit.Color == EColor.White)
                {
                    var r = Random.Range(0, 2);
                    if (r == 0)
                    {
                        if (blueRemain > 0)
                        {
                            unit.SetColor(EColor.Blue);
                            blueRemain--;
                        }
                        else
                        {
                            unit.SetColor(EColor.Green);
                            greenRemain--;
                        }
                    }
                    else if (r == 1)
                    {
                        if (greenRemain > 0)
                        {
                            unit.SetColor(EColor.Green);
                            greenRemain--;
                        }
                        else
                        {
                            unit.SetColor(EColor.Blue);
                            blueRemain--;
                        }
                    }
                }
            }
        }
    }

    private void GenerateUnit()
    {
        for (var i = 0; i < MapWidth; i++)
        {
            for (var j = 0; j < MapHeight; j++)
            {
                var unit = Instantiate(Unit,
                    new Vector3(
                        i * UnitSize + (i - 1) * UnitSpace,
                        j * UnitSize + (j - 1) * UnitSpace,
                        0),
                    Quaternion.identity);
                unit.localScale = new Vector3(UnitSize, UnitSize, 1);
                unit.gameObject.SetActive(true);

                _unitDatas[i][j] = new UnitData
                {
                    X = i,
                    Y = j,
                    Color = EColor.White,
                    SpriteRenderer = unit.GetComponent<SpriteRenderer>()
                };
            }
        }
    }

    private void InitData()
    {
        _unitDatas = new UnitData[MapWidth][];
        for (var i = 0; i < _unitDatas.Length; i++)
            _unitDatas[i] = new UnitData[MapHeight];

        _sumCount = MapWidth * MapHeight;
        _redCount = (int) (_sumCount * RedRate);
        _blueCount = (int) (_sumCount * BlueRate);
        _greenCount = _sumCount - _redCount - _blueCount;
    }

    private void GetNeighbor(int x, int y, ref List<UnitData> neighbors)
    {
        // 初始化列表
        if (neighbors == null)
            neighbors = new List<UnitData>();
        else
            neighbors.Clear();

        // left
        if (x - 1 >= 0)
            neighbors.Add(_unitDatas[x - 1][y]);

        // right
        if (x + 1 < MapWidth)
            neighbors.Add(_unitDatas[x + 1][y]);

        // up
        if (y + 1 < MapHeight)
            neighbors.Add(_unitDatas[x][y + 1]);

        // down
        if (y - 1 >= 0)
            neighbors.Add(_unitDatas[x][y - 1]);
    }
}