using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class Dice : MonoBehaviour
{
    public TMP_Text diceText;
    public GameState gameState;
    public int diceValue;

    public void RollDice()
    {
        if (gameState.diceRolled)
        {
            Debug.Log("Кубик уже кинуто! Виконайте хід або завершіть його.");
            return;
        }
        diceValue = Random.Range(1, 7);
        diceText.text = "Dice: " + diceValue;
        if (gameState != null)
        {
            gameState.diceValue = diceValue;
            Debug.Log($"Dice value is {diceValue}");
        }
        else
        {
            Debug.LogWarning("GameState не встановлений у Dice.");
        }
        gameState.diceRolled = true;
    }
    public void ResetDice() => diceValue = -1;
}

