using TMPro;
using UnityEngine;

public class ScorePanel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    private int currentScore = 0;

    public void IncreaseScore()
    {
        currentScore++;
        scoreText.text = currentScore.ToString();
    }

    public void DecreaseScore()
    {
        currentScore-=10;
        scoreText.text = currentScore.ToString();
    }
}