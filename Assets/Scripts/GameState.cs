using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public int currentPlayer;
    public int playerIndex;
    public Dictionary<int, List<int>> playerPositions;
    public Dictionary<int, List<int>> stepCounters;
    public int diceValue;
    public bool diceRolled;
    public Dice dice;
    public GameObject endGamePanel; // Панель для завершення гри
    public TMP_Text endText;


    void Start()
    {
        currentPlayer = 0;
        diceValue = 0;
        diceRolled = false;
        playerPositions = new Dictionary<int, List<int>>();
        stepCounters = new Dictionary<int, List<int>>();
        for (int i = 0; i < 4; i++)
        {
            playerPositions[i] = new List<int> { -1, -1, -1, -1 }; // -1 означає, що фігура ще не на полі
            stepCounters[i] = new List<int> { 0, 0, 0, 0 };
        }
        GameObject shadow = GameObject.Find("UI/Canvas/Shadow");
        shadow.SetActive(true);
    }

    public void SetPlayerIndex(int index)
    {
        currentPlayer = index;
        playerIndex = index;
        Debug.Log($"Гравець обрав колір: {index}");
    }

    public void NextPlayer()
    {
        currentPlayer = (currentPlayer + 1) % 4;
        diceRolled = false;

        if (IsAiPlayer(currentPlayer)) // Якщо це ШІ
        {
            Debug.Log($"Хід ШІ: гравець {currentPlayer}");
            MakeAiMove();
        }
        else
        {
            Debug.Log($"Хід гравця: {currentPlayer}");
        }
    }


    private bool IsAiPlayer(int playerInd)
    {
        return (playerInd != playerIndex);
    }
    
    public void UpdatePlayerPosition(int playerIndex, int pawnIndex, int newPosition)
    {
        stepCounters[playerIndex][pawnIndex] += diceValue;
        playerPositions[playerIndex][pawnIndex] = newPosition;
        Debug.Log($"Кількість поточних кроків гравця {playerIndex}: {stepCounters[playerIndex][pawnIndex]}");
    }

    // ----------------------- Логіка ходів фішок ---------------------------//

    public int GetStartingCell(int playerIndex)
    {
        return playerIndex * 10;
    }

    public Vector3 GetCellPosition(int cellIndex)
    {
        GameObject cell = GameObject.Find($"Board/Path/Cell {cellIndex + 10}");
        if (cell == null)
        {
            //Debug.LogError($"Клітинка Board/Path/Cell {cellIndex} не знайдена! Перевірте сцену.");
            return Vector3.zero;
        }

        return cell.transform.position;
    }


    public int GetTotalCells()
    {
        return 50;
    }

    public void ResetPawn(int playerIndex, int pawnIndex)
    {
        playerPositions[playerIndex][pawnIndex] = -1;
        GameObject pawn = FindPawnObject(playerIndex, pawnIndex);
        if (pawn != null)
        {
            Pawn pawnScript = pawn.GetComponent<Pawn>();
            pawn.transform.position = pawnScript.startPosition;
            pawnScript.currentCellIndex = -1;
            stepCounters[playerIndex][pawnIndex] = 0;
            Debug.Log($"Фішка {pawnIndex} гравця {playerIndex} повернута на старт: {pawnScript.startPosition}");
        }
    }

    private GameObject FindPawnObject(int playerIndex, int pawnIndex)
    {
        return GameObject.Find($"Pawn_{playerIndex}_{pawnIndex}");
    }

    public int GetPawnIndex(int playerIndex, int cellIndex)
    {
        for (int i = 0; i < playerPositions[playerIndex].Count; i++)
        {
            if (playerPositions[playerIndex][i] == cellIndex)
            {
                return i;
            }
        }
        return -1;
    }

    //------------------ Логіка ходу ШІ ----------------------
    public void MakeAiMove()
    {
        StartCoroutine(PerformAiMoves());
    }

    private IEnumerator PerformAiMoves()
    {
        yield return new WaitForSeconds(0.5f);
        dice.RollDice();

        if (diceValue == 6)
        {
            bool moved = TryMoveOutOfStart(currentPlayer);
            yield return new WaitForSeconds(0.5f); // Затримка перед наступною дією
            if (!moved)
            {
                yield return MoveBestPawnWithDelay(currentPlayer);
            }
        }
        else
        {
            yield return MoveBestPawnWithDelay(currentPlayer);
        }

        dice.ResetDice();
        NextPlayer();
    }

    private bool TryMoveOutOfStart(int playerIndex)
    {
        for (int i = 0; i < playerPositions[playerIndex].Count; i++)
        {
            if (playerPositions[playerIndex][i] == -1)
            {
                int startCell = GetStartingCell(playerIndex);
                playerPositions[playerIndex][i] = startCell;
                GameObject pawn = FindPawnObject(playerIndex, i);
                if (pawn != null)
                {
                    stepCounters[playerIndex][i] = 0;
                    pawn.transform.position = GetCellPosition(startCell);
                    Pawn pawnScript = pawn.GetComponent<Pawn>();
                    pawnScript.Anim();
                }
                Debug.Log($"ШІ {playerIndex} вивів фішку {i} зі старту.");
                return true;
            }
        }
        return false;
    }

    private IEnumerator MoveBestPawnWithDelay(int playerIndex)
    {
        int bestMove = -1;
        int bestScore = int.MinValue;
        int bestPawnIndex = -1;

        for (int i = 0; i < playerPositions[playerIndex].Count; i++)
        {
            int currentPosition = playerPositions[playerIndex][i];
            if (currentPosition == -1) continue; // Пропускаємо фішки, які ще на старті

            currentPosition = CalculateOverflowPosition(currentPosition);
            HandleCapture(playerIndex, currentPosition);

            if (currentPosition >= GetTotalCells()) continue; // Пропускаємо неможливі ходи

            // Розрахунок оцінки для цього ходу
            int score = EvaluateMove(playerIndex, i, currentPosition);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = currentPosition;
                bestPawnIndex = i;
            }
        }

        if (bestPawnIndex != -1)
        {
            playerPositions[playerIndex][bestPawnIndex] = bestMove;
            stepCounters[playerIndex][bestPawnIndex] += diceValue;
            GameObject pawn = FindPawnObject(playerIndex, bestPawnIndex);

            if (stepCounters[playerIndex][bestPawnIndex] >= 40)
            {
                Debug.Log($"Фішка {bestPawnIndex} гравця {playerIndex} завершила коло та перходить у HomeRow");
                MoveToHomeRow(playerIndex, bestPawnIndex);
                yield break; // Завершуємо виконання
            }

            if (pawn != null)
            {
                pawn.transform.position = GetCellPosition(bestMove);
                Pawn pawnScript = pawn.GetComponent<Pawn>();
                pawnScript.Anim();
            }

            Debug.Log($"ШІ {playerIndex} перемістив фішку {bestPawnIndex} на позицію {bestMove}.");

            yield return new WaitForSeconds(0.5f); // Затримка між рухами
        }
    }
    public int CalculateOverflowPosition(int currentPosition)
    {
        int newPosition;

        if (currentPosition + diceValue > 39)
        {
            int overflow = (currentPosition + diceValue) - 49;
            newPosition = 10 + (overflow - 1);
        }
        else
        {
            newPosition = currentPosition + diceValue;
        }
        
        return newPosition;
    }
    private void HandleCapture(int playerIndex, int cellIndex)
    {
        for (int otherPlayer = 0; otherPlayer < 4; otherPlayer++)
        {
            if (otherPlayer == playerIndex) continue;

            for (int otherPawnIndex = 0; otherPawnIndex < playerPositions[otherPlayer].Count; otherPawnIndex++)
            {
                if (playerPositions[otherPlayer][otherPawnIndex] == cellIndex)
                {
                    ResetPawn(otherPlayer, otherPawnIndex);
                    Debug.Log($"Фішку гравця {otherPlayer} захоплено гравцем {playerIndex} на клітинці {cellIndex+10}!");
                }
            }
        }
    }

    private int EvaluateMove(int playerIndex, int pawnIndex, int newPosition)
    {
        int score = 0;
        
        if (newPosition >= GetTotalCells() - 6) score += 10;

        foreach (var opponentIndex in playerPositions.Keys)
        {
            if (opponentIndex == playerIndex) continue;
            foreach (var opponentPosition in playerPositions[opponentIndex])
            {
                if (Mathf.Abs(newPosition - opponentPosition) <= 6) score -= 5;
            }
        }

        return score;
    }

    public void MoveToHomeRow(int playerIndex, int pawnIndex)
    {
        string homeObjectName = $"HomeRow_{GetPlayerColor(playerIndex)}/Coin_{pawnIndex + 1}";
        GameObject homePositionObject = GameObject.Find(homeObjectName);
    
        if (homePositionObject != null)
        {
            GameObject pawn = FindPawnObject(playerIndex, pawnIndex);
            if (pawn != null)
            {
                pawn.transform.position = homePositionObject.transform.position;
                playerPositions[playerIndex][pawnIndex] = -10;
                Debug.Log($"Фішка {pawnIndex} гравця {playerIndex} переміщена у домашній ряд на позицію {pawnIndex + 1}.");
                Pawn pawnScript = pawn.GetComponent<Pawn>();
                pawnScript.Anim();
                CheckForWin(playerIndex);
            }
            else
            {
                Debug.LogError($"Не вдалося знайти об'єкт фішки {pawnIndex} для гравця {playerIndex}.");
            }
        }
        else
        {
            Debug.LogError($"Не вдалося знайти позицію {homeObjectName} у домашньому ряді гравця {playerIndex}.");
        }
    }


    public void CheckForWin(int playerIndex)
    {
        int homeCount = 0;

        foreach (int position in playerPositions[playerIndex])
        {
            if (position < -1)
            {
                homeCount++;
            }
        }

        if (homeCount == 4)
        {
            Debug.Log($"Гравець {playerIndex} виграв гру!");
            EndGame(playerIndex);
        }
    }


    private void EndGame(int winnerIndex)
    {
        Debug.Log($"Вітаємо! Гравець {winnerIndex} виграв гру!");

        GameObject shadow = GameObject.Find("UI/Canvas/Shadow");
        if (shadow == null)
        {
            Debug.LogWarning("Shadow component is missing");
        }
        if (endGamePanel != null && endText != null)
        {
            endGamePanel.SetActive(true);
            shadow.SetActive(true);
            string color = GetPlayerColor(winnerIndex);
            endText.text = $"{color} player won!";
        }
        Time.timeScale = 0;
    }

    public void TestWin()
    {
        EndGame(1);
    }

    private string GetPlayerColor(int colorIndex)
    {
        switch (colorIndex)
        {
            case 0: return "Yellow";
            case 1: return "Green";
            case 2: return "Blue";
            case 3: return "Red";
            default: return "Unknown";
        }
    }

}