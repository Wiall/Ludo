using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class GameState : MonoBehaviour
{
    public int currentPlayer;
    [FormerlySerializedAs("playerIndex")] public int playersIndex;
    public Dictionary<int, List<int>> PlayerPositions;
    public Dictionary<int, List<int>> StepCounters;
    public int diceValue;
    public bool diceRolled;
    public Dice dice;
    public GameObject endGamePanel;
    public TMP_Text endText;


    void Start()
    {
        currentPlayer = 0;
        diceValue = 0;
        diceRolled = false;
        PlayerPositions = new Dictionary<int, List<int>>();
        StepCounters = new Dictionary<int, List<int>>();
        for (int i = 0; i < 4; i++)
        {
            PlayerPositions[i] = new List<int> { -1, -1, -1, -1 };
            StepCounters[i] = new List<int> { 0, 0, 0, 0 };
        }
        GameObject shadow = GameObject.Find("UI/Canvas/Shadow");
        shadow.SetActive(true);
    }

    public void SetPlayerIndex(int index)
    {
        currentPlayer = index;
        playersIndex = index;
        Debug.Log($"Гравець обрав колір: {index}");
    }

    public void NextPlayer()
    {
        currentPlayer = (currentPlayer + 1) % 4;
        diceRolled = false;

        if (IsAiPlayer(currentPlayer))
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
        return (playerInd != playersIndex);
    }
    
    public void UpdatePlayerPosition(int playerIndex, int pawnIndex, int newPosition)
    {
        StepCounters[playerIndex][pawnIndex] += diceValue;
        PlayerPositions[playerIndex][pawnIndex] = newPosition;
        Debug.Log($"Кількість поточних кроків гравця {playerIndex}: {StepCounters[playerIndex][pawnIndex]}");
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
        PlayerPositions[playerIndex][pawnIndex] = -1;
        GameObject pawn = FindPawnObject(playerIndex, pawnIndex);
        if (pawn != null)
        {
            Pawn pawnScript = pawn.GetComponent<Pawn>();
            pawn.transform.position = pawnScript.startPosition;
            pawnScript.currentCellIndex = -1;
            StepCounters[playerIndex][pawnIndex] = 0;
            Debug.Log($"Фішка {pawnIndex} гравця {playerIndex} повернута на старт.");
        }
    }

    private GameObject FindPawnObject(int playerIndex, int pawnIndex)
    {
        return GameObject.Find($"Pawn_{playerIndex}_{pawnIndex}");
    }
    
    //------------------ Логіка ходу ШІ ----------------------
    private void MakeAiMove()
    {
        StartCoroutine(PerformAiMoves());
    }

    private IEnumerator PerformAiMoves()
    {
        yield return new WaitForSeconds(0.5f);
        dice.RollDice();

        int bestMoveIndex = MinimaxDecision(currentPlayer);
        if (bestMoveIndex != -1)
        {
            MovePawn(currentPlayer, bestMoveIndex);
        }

        dice.ResetDice();
        NextPlayer();
    }

    private int MinimaxDecision(int playerIndex)
    {
        int bestPawnIndex = -1;
        int bestScore = int.MinValue;

        for (int pawnIndex = 0; pawnIndex < PlayerPositions[playerIndex].Count; pawnIndex++)
        {
            int currentPosition = PlayerPositions[playerIndex][pawnIndex];

            if (currentPosition == -1 && diceValue == 6)
            {
                return pawnIndex;
            }

            if (currentPosition != -1)
            {
                int newPosition = CalculateOverflowPosition(currentPosition);
                if (newPosition >= GetTotalCells()) continue;

                int score = EvaluateMove(playerIndex, newPosition);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestPawnIndex = pawnIndex;
                }
            }
        }

        return bestPawnIndex;
    }

    private void MovePawn(int playerIndex, int pawnIndex)
    {
        int currentPosition = PlayerPositions[playerIndex][pawnIndex];

        if (currentPosition == -1)
        {
            int startCell = GetStartingCell(playerIndex);
            PlayerPositions[playerIndex][pawnIndex] = startCell;
            GameObject pawn = FindPawnObject(playerIndex, pawnIndex);
            if (pawn != null)
            {
                StepCounters[playerIndex][pawnIndex] = 0;
                pawn.transform.position = GetCellPosition(startCell);
                Pawn pawnScript = pawn.GetComponent<Pawn>();
                pawnScript.Anim();
                HandleCapture(playerIndex, startCell);
            }
        }
        else
        {
            int newPosition = CalculateOverflowPosition(currentPosition);
            HandleCapture(playerIndex, newPosition);
            PlayerPositions[playerIndex][pawnIndex] = newPosition;
            StepCounters[playerIndex][pawnIndex] += diceValue;

            if (StepCounters[playerIndex][pawnIndex] >= 40)
            {
                MoveToHomeRow(playerIndex, pawnIndex);
                return;
            }

            GameObject pawn = FindPawnObject(playerIndex, pawnIndex);
            if (pawn != null)
            {
                pawn.transform.position = GetCellPosition(newPosition);
                Pawn pawnScript = pawn.GetComponent<Pawn>();
                pawnScript.Anim();
            }
        }
    }

    private int EvaluateMove(int playerIndex, int newPosition)
    {
        int score = 0;

        if (newPosition >= GetTotalCells() - 6) score += 10;

        foreach (var opponentIndex in PlayerPositions.Keys)
        {
            if (opponentIndex == playerIndex) continue;
            foreach (var opponentPosition in PlayerPositions[opponentIndex])
            {
                if (newPosition == opponentPosition) score += 20; 
                if (Mathf.Abs(newPosition - opponentPosition) <= 6) score -= 5;
            }
        }

        return score;
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

            for (int otherPawnIndex = 0; otherPawnIndex < PlayerPositions[otherPlayer].Count; otherPawnIndex++)
            {
                if (PlayerPositions[otherPlayer][otherPawnIndex] == cellIndex)
                {
                    ResetPawn(otherPlayer, otherPawnIndex);
                    Debug.Log($"Фішку гравця {otherPlayer} захоплено гравцем {playerIndex} на клітинці {cellIndex+10}!");
                }
            }
        }
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
                PlayerPositions[playerIndex][pawnIndex] = -10;
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

        foreach (int position in PlayerPositions[playerIndex])
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

    public void TestWin() => EndGame(1);

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