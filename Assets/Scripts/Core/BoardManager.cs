using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 보드 전체를 관장. 그리드 생성, 그룹 탐색, 제거, 중력, 셔플 담당.
/// </summary>
public class BoardManager : MonoBehaviour
{
    // ── 직렬화 필드 ───────────────────────────────────────────
    [Header("References")]
    [SerializeField] private RectTransform boardContainer;
    [SerializeField] private GameObject blockPrefab;

    [Header("Layout")]
    [SerializeField] private float cellSize = 80f;
    [SerializeField] private float cellGap = 6f;

    // ── 내부 상태 ─────────────────────────────────────────────
    private Block[,] _grid;        // [row, col]  row=0이 바닥
    private int _rows, _cols, _colorCount;
    private bool _isAnimating;

    // ── 이벤트 ───────────────────────────────────────────────
    public event Action<int> OnGroupPopped;    // 그룹 크기 전달
    public event Action OnBoardCleared;
    public event Action OnNoMovesLeft;
    public event Action<List<Block>> OnGroupHighlighted;

    // ─────────────────────────────────────────────────────────
    void OnEnable()  => Block.OnBlockClicked += HandleBlockClick;
    void OnDisable() => Block.OnBlockClicked -= HandleBlockClick;

    // ── 보드 생성 ─────────────────────────────────────────────
    public void BuildBoard(LevelData data)
    {
        _rows = data.rows;
        _cols = data.cols;
        _colorCount = Mathf.Clamp(data.colorCount, 2, Block.Palette.Length);

        ClearBoard();
        _grid = new Block[_rows, _cols];

        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
                SpawnBlock(r, c, UnityEngine.Random.Range(0, _colorCount), spawn: false);

        // 보드 크기 맞추기
        float totalW = _cols * cellSize + (_cols - 1) * cellGap;
        float totalH = _rows * cellSize + (_rows - 1) * cellGap;
        boardContainer.sizeDelta = new Vector2(totalW, totalH);

        // 위치 설정 후 생성 애니메이션
        StartCoroutine(SpawnAllAnimation());
    }

    private IEnumerator SpawnAllAnimation()
    {
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                if (_grid[r, c] != null)
                    _grid[r, c].PlaySpawnAnimation();
            }
            yield return new WaitForSeconds(0.03f);
        }
    }

    // ── 블록 스폰 ─────────────────────────────────────────────
    private Block SpawnBlock(int row, int col, int colorIndex, bool spawn = true)
    {
        GameObject go = Instantiate(blockPrefab, boardContainer);
        Block block = go.GetComponent<Block>();
        block.Init(colorIndex, row, col);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(cellSize, cellSize);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0, 0);
        rt.anchoredPosition = GetCellPosition(row, col);

        _grid[row, col] = block;

        if (spawn) block.PlaySpawnAnimation();
        return block;
    }

    // ── 셀 좌표 계산 ──────────────────────────────────────────
    private Vector2 GetCellPosition(int row, int col)
    {
        float x = col * (cellSize + cellGap);
        float y = row * (cellSize + cellGap);
        return new Vector2(x, y);
    }

    // ── 클릭 처리 ─────────────────────────────────────────────
    private void HandleBlockClick(Block clicked)
    {
        if (_isAnimating) return;

        List<Block> group = FindConnectedGroup(clicked);
        if (group.Count < 2)
        {
            // 그룹 없음: 흔들기
            StartCoroutine(ShakeBlock(clicked));
            return;
        }

        StartCoroutine(PopGroup(group));
    }

    // ── 그룹 하이라이트 (호버용, 외부 호출 가능) ──────────────
    public void HighlightGroupOf(Block block)
    {
        if (_isAnimating) return;
        List<Block> group = FindConnectedGroup(block);
        OnGroupHighlighted?.Invoke(group);
        foreach (Block b in group) b.SetHighlight(true);
    }

    public void ClearAllHighlights()
    {
        if (_grid == null) return;
        foreach (Block b in _grid)
            if (b != null) b.SetHighlight(false);
    }

    // ── BFS 그룹 탐색 ─────────────────────────────────────────
    private List<Block> FindConnectedGroup(Block start)
    {
        List<Block> result = new List<Block>();
        if (start == null) return result;

        bool[,] visited = new bool[_rows, _cols];
        Queue<Block> queue = new Queue<Block>();
        queue.Enqueue(start);
        visited[start.Row, start.Col] = true;

        while (queue.Count > 0)
        {
            Block cur = queue.Dequeue();
            result.Add(cur);

            int[][] dirs = { new[]{1,0}, new[]{-1,0}, new[]{0,1}, new[]{0,-1} };
            foreach (int[] d in dirs)
            {
                int nr = cur.Row + d[0];
                int nc = cur.Col + d[1];
                if (nr < 0 || nr >= _rows || nc < 0 || nc >= _cols) continue;
                if (visited[nr, nc]) continue;
                Block neighbor = _grid[nr, nc];
                if (neighbor == null || neighbor.ColorIndex != start.ColorIndex) continue;
                visited[nr, nc] = true;
                queue.Enqueue(neighbor);
            }
        }
        return result;
    }

    // ── 그룹 제거 ─────────────────────────────────────────────
    private IEnumerator PopGroup(List<Block> group)
    {
        _isAnimating = true;

        // 하이라이트 제거
        ClearAllHighlights();

        // 팝 애니메이션
        int done = 0;
        foreach (Block b in group)
        {
            _grid[b.Row, b.Col] = null;
            b.PlayPopAnimation(() => done++);
        }

        yield return new WaitUntil(() => done >= group.Count);

        OnGroupPopped?.Invoke(group.Count);

        yield return ApplyGravity();
        yield return new WaitForSeconds(0.1f);

        // 이동 가능 여부 확인
        if (IsCleared())
            OnBoardCleared?.Invoke();
        else if (!HasAnyMove())
            OnNoMovesLeft?.Invoke();

        _isAnimating = false;
    }

    // ── 중력 ─────────────────────────────────────────────────
    private IEnumerator ApplyGravity()
    {
        bool anyMoved = false;

        for (int c = 0; c < _cols; c++)
        {
            int empty = 0;
            for (int r = 0; r < _rows; r++)
            {
                if (_grid[r, c] == null)
                {
                    empty++;
                }
                else if (empty > 0)
                {
                    Block b = _grid[r, c];
                    _grid[r - empty, c] = b;
                    _grid[r, c] = null;
                    b.Row = r - empty;

                    float dur = 0.08f + empty * 0.04f;
                    b.MoveToPosition(GetCellPosition(b.Row, b.Col), dur);
                    anyMoved = true;
                }
            }
        }

        if (anyMoved)
            yield return new WaitForSeconds(0.28f);
    }

    // ── 승리/이동 판단 ────────────────────────────────────────
    public bool IsCleared()
    {
        foreach (Block b in _grid)
            if (b != null) return false;
        return true;
    }

    public bool HasAnyMove()
    {
        for (int r = 0; r < _rows; r++)
            for (int c = 0; c < _cols; c++)
                if (_grid[r, c] != null && FindConnectedGroup(_grid[r, c]).Count >= 2)
                    return true;
        return false;
    }

    // ── 남은 블록 수 ──────────────────────────────────────────
    public int RemainingBlocks()
    {
        int count = 0;
        foreach (Block b in _grid) if (b != null) count++;
        return count;
    }

    // ── 보드 초기화 ───────────────────────────────────────────
    private void ClearBoard()
    {
        if (_grid == null) return;
        foreach (Block b in _grid)
            if (b != null) Destroy(b.gameObject);
    }

    // ── 흔들기 ────────────────────────────────────────────────
    private IEnumerator ShakeBlock(Block b)
    {
        Vector3 orig = b.transform.localPosition;
        float[] shakes = { 8f, -8f, 6f, -6f, 0f };
        foreach (float dx in shakes)
        {
            b.transform.localPosition = orig + new Vector3(dx, 0, 0);
            yield return new WaitForSeconds(0.04f);
        }
        b.transform.localPosition = orig;
    }
}
