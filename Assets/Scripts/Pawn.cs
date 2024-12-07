using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour
{
    public int playerIndex;
    public int pawnIndex;
    public Transform startSlot;
    public Vector3 startPosition;
    public int currentCellIndex;
    public GameState gameState;
    private Vector3 originalScale;

    void Start()
    {
        currentCellIndex = -1; // Початкове значення (фішка ще не на полі)
        startPosition = transform.position;
        gameObject.name = $"Pawn_{playerIndex}_{pawnIndex}";
        
        originalScale = transform.localScale;
    }

    // Метод для переміщення фішки
    public void MovePawn(int diceValue)
    {
        if (diceValue == -1)
        {
            Debug.Log("Ви ще не кинули кубик!"); return;
        }
        if (currentCellIndex == -2)
        {
            Debug.Log("Ця фішка вже в домашньому ряду!");
            return;
        }

        if (currentCellIndex == -1) // Якщо фішка ще не на полі
        {
            if (diceValue == 6) // Гравець має отримати 6, щоб вивести фішку
            {
                currentCellIndex = gameState.GetStartingCell(playerIndex); // Вихід на стартову клітинку
                transform.position = gameState.GetCellPosition(currentCellIndex); // Змінюємо позицію
                gameState.UpdatePlayerPosition(playerIndex, pawnIndex, currentCellIndex);
                gameState.stepCounters[playerIndex][pawnIndex] = 0;
            }
        }
        else
        {
            int totalCells = gameState.GetTotalCells();
            int startCell = gameState.GetStartingCell(playerIndex);

            // Розрахунок нової позиції з урахуванням "завершення кола"
            currentCellIndex = gameState.CalculateOverflowPosition(currentCellIndex);
            if (gameState.stepCounters[playerIndex][pawnIndex] >= 40)
            {
                Debug.Log($"Фішка {pawnIndex} гравця {playerIndex} завершила коло та перходить у HomeRow");
                gameState.MoveToHomeRow(playerIndex, pawnIndex);
                return;
            }

            transform.position = gameState.GetCellPosition(currentCellIndex);
            gameState.UpdatePlayerPosition(playerIndex, pawnIndex, currentCellIndex);
        }
        diceValue = -1;
    }


    // Метод для обробки захоплення (якщо на новій клітинці є ворожа фішка)
    private void HandleCapture(int cellIndex)
    {
        for (int otherPlayer = 0; otherPlayer < 4; otherPlayer++) // Для кожного гравця
        {
            if (otherPlayer == playerIndex) continue; // Пропускаємо власні фішки

            for (int otherPawnIndex = 0; otherPawnIndex < gameState.playerPositions[otherPlayer].Count; otherPawnIndex++)
            {
                if (gameState.playerPositions[otherPlayer][otherPawnIndex] == cellIndex) // Якщо фішка суперника на цій клітинці
                {
                    gameState.ResetPawn(otherPlayer, otherPawnIndex); // Повертаємо захоплену фішку на старт
                }
            }
        }
    }

    public void OnSelect()
    {
        // Анімація збільшення
        LeanTween.scale(gameObject, originalScale * 1.2f, 0.2f).setEaseOutBack()
            .setOnComplete(() =>
            {
                // Повернення до нормального розміру
                LeanTween.scale(gameObject, originalScale, 0.2f).setEaseInBack();
            });
    }
    void OnMouseDown()
    {
        if (gameState.currentPlayer == playerIndex) // Перевірка, чи це хід гравця
        {
            if (!gameState.diceRolled)
            {
                Debug.Log("Киньте кубик перед тим, як рухати фішку!");
                return;
            }
            OnSelect();
            MovePawn(gameState.diceValue); // Рух фішки
            HandleCapture(currentCellIndex); // Перевірка на захоплення
            gameState.dice.ResetDice();
            gameState.NextPlayer(); // Передача ходу
        }
    }

}