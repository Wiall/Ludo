using System.Collections.Generic;
using UnityEngine;

public class LudoGame : MonoBehaviour
{
    private int[,] boardState; // Стан ігрового поля
    private int currentPlayer; // Чий хід
    private int diceValue; // Результат кидка кубика
    public GameState gameState;

    void Start()
    {
        InitializeGame();
    }

    void InitializeGame()
    {
        boardState = new int[4, 40];
        currentPlayer = 0;
    }

    public int MinimaxDecision(int depth, bool isMaximizing, int playerIndex)
    {
        if (depth == 0 || IsTerminalState())
        {
            return EvaluateState(playerIndex);
        }

        if (isMaximizing)
        {
            int maxEval = int.MinValue;
            foreach (var move in GetAllPossibleMoves(playerIndex))
            {
                SimulateMove(move, playerIndex);
                int eval = MinimaxDecision(depth - 1, false, playerIndex);
                UndoMove(move, playerIndex);
                maxEval = Mathf.Max(maxEval, eval);
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (var move in GetAllPossibleMoves(1 - playerIndex))
            {
                SimulateMove(move, 1 - playerIndex);
                int eval = MinimaxDecision(depth - 1, true, playerIndex);
                UndoMove(move, 1 - playerIndex);
                minEval = Mathf.Min(minEval, eval);
            }
            return minEval;
        }
    }

    public void SimulateMove(int move, int playerIndex)
    {

    }

    public void UndoMove(int move, int playerIndex)
    {

    }


    bool IsTerminalState()
    {
        return false;
    }

    int EvaluateState(int playerIndex)
    {
        int score = 0;

        foreach (var pos in gameState.playerPositions[playerIndex])
        {
            if (pos != -1) score += pos;

            if (pos >= gameState.GetTotalCells() - 6) score += 10;
        }

        foreach (var opponentIndex in gameState.playerPositions.Keys)
        {
            if (opponentIndex != playerIndex)
            {
                foreach (var pos in gameState.playerPositions[opponentIndex])
                {
                    if (pos != -1) score -= pos;

                    foreach (var playerPos in gameState.playerPositions[playerIndex])
                    {
                        if (Mathf.Abs(playerPos - pos) <= 6) score -= 5;
                    }
                }
            }
        }

        return score;
    }

    List<int> GetAllPossibleMoves(int playerIndex)
    {
        List<int> moves = new List<int>();
        foreach (var pawnIndex in gameState.playerPositions[playerIndex])
        {
            if (pawnIndex != -1)
            {
                int newPosition = pawnIndex + gameState.diceValue;
                if (newPosition < gameState.GetTotalCells())
                {
                    moves.Add(newPosition);
                }
            }
        }
        return moves;
    }
}

