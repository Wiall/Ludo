using System.Collections.Generic;
using UnityEngine;

public class Pawn : MonoBehaviour
{
    public int playerIndex;
    public int pawnIndex;
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

        if (currentCellIndex == -1)
        {
            if (diceValue == 6)
            {
                currentCellIndex = gameState.GetStartingCell(playerIndex);
                transform.position = gameState.GetCellPosition(currentCellIndex);
                gameState.UpdatePlayerPosition(playerIndex, pawnIndex, currentCellIndex);
                gameState.stepCounters[playerIndex][pawnIndex] = 0;
            }
        }
        else
        {
            currentCellIndex = gameState.CalculateOverflowPosition(currentCellIndex);
            transform.position = gameState.GetCellPosition(currentCellIndex);
            gameState.UpdatePlayerPosition(playerIndex, pawnIndex, currentCellIndex);
            if (gameState.stepCounters[playerIndex][pawnIndex] >= 40)
            {
                Debug.Log($"Фішка {pawnIndex} гравця {playerIndex} завершила коло та перходить у HomeRow");
                gameState.MoveToHomeRow(playerIndex, pawnIndex);
                return;
            }
        }
        diceValue = -1;
    }
    
    private void HandleCapture(int cellIndex)
    {
        for (int otherPlayer = 0; otherPlayer < 4; otherPlayer++)
        {
            if (otherPlayer == playerIndex) continue;

            for (int otherPawnIndex = 0; otherPawnIndex < gameState.playerPositions[otherPlayer].Count; otherPawnIndex++)
            {
                if (gameState.playerPositions[otherPlayer][otherPawnIndex] == cellIndex)
                {
                    gameState.ResetPawn(otherPlayer, otherPawnIndex);
                }
            }
        }
    }

    public void Anim()
    {
        OnSelect();
    }
    public void OnSelect()
    {
        LeanTween.scale(gameObject, originalScale * 1.2f, 0.2f).setEaseOutBack()
            .setOnComplete(() =>
            {
                LeanTween.scale(gameObject, originalScale, 0.2f).setEaseInBack();
            });
    }
    void OnMouseDown()
    {
        if (gameState.currentPlayer == playerIndex)
        {
            if (!gameState.diceRolled)
            {
                Debug.Log("Киньте кубик перед тим, як рухати фішку!");
                return;
            }
            OnSelect();
            MovePawn(gameState.diceValue);
            HandleCapture(currentCellIndex);
            gameState.dice.ResetDice();
            gameState.NextPlayer();
        }
    }
}