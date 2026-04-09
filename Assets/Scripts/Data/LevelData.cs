using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "ColorPop/LevelData")]
public class LevelData : ScriptableObject
{
    [Header("Board Settings")]
    public int rows = 8;
    public int cols = 8;
    public int colorCount = 4;   // 2~5

    [Header("Clear Condition")]
    [Tooltip("달성해야 할 목표 점수 (0이면 전부 지우기)")]
    public int targetScore = 0;

    [Header("Star Thresholds")]
    public int star2Score = 500;
    public int star3Score = 1000;

    [Header("Visual")]
    public string levelTitle = "Level 1";
    public Color backgroundColor = new Color(0.12f, 0.12f, 0.18f);
}
