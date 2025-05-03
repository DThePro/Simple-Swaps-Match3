using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class HighScore : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI highScoreText;
    public static HighScore Instance { get; private set; }

    private int _highScore;

    public int HScore
    {
        get => _highScore;

        set
        {
            if (value > _highScore)
            {
                _highScore = value;
                highScoreText.SetText($"Hi: {value}");
                SaveManager.Instance.Save();
            }
        }
    }

    private void Awake()
    {
        Instance = this;
        SaveManager.Instance.Load();
        _highScore = SaveManager.Instance.Load();
        highScoreText.SetText($"Hi: {_highScore}");
    }
}
