using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSelection : MonoBehaviour
{
    public GameState gameState;

    public void SelectColor(int colorIndex)
    {
        GameObject shadow = GameObject.Find("UI/Canvas/Shadow");
        gameObject.SetActive(true);
        gameState.SetPlayerIndex(colorIndex);
        Debug.Log($"Гравець обрав колір: {colorIndex}");
        gameObject.SetActive(false);
        shadow.SetActive(false);
    }
    public void Quit()
    {
        Debug.Log("Гра завершується...");
        Application.Quit();
    }
    public void RestartGame()
    {
        // Скидання позицій гравців
        foreach (var player in gameState.PlayerPositions.Keys)
        {
            for (int i = 0; i < gameState.PlayerPositions[player].Count; i++)
            {
                gameState.ResetPawn(player, i); // Викликаємо ResetPawn для кожної фішки
            }
        }

        // Скидання стану кубика
        gameState.dice.ResetDice();

        // Початок із першого гравця
        gameState.currentPlayer = 0;

        // Відображення стану (якщо потрібно)
        Debug.Log("Гра перезапущена!");
        
        GameObject shadow = GameObject.Find("UI/Canvas/Shadow");
        if (shadow == null)
        {
            Debug.LogWarning("Shadow component is missing");
        }
        if (gameState.endGamePanel != null && gameState.endText != null)
        {
            gameState.endGamePanel.SetActive(false);
            shadow.SetActive(false);
        }
        Time.timeScale = 1;
    }


}
