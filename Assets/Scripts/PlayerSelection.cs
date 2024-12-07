using UnityEngine;

public class PlayerSelection : MonoBehaviour
{
    public GameState gameState;

    public void SelectColor(int colorIndex)
    {
        GameObject shadow = GameObject.Find("UI/Canvas/Shadow");
        gameObject.SetActive(true);
        gameState.SetPlayerIndex(colorIndex);
        Debug.Log($"Гравець обрав колір: {colorIndex}");
        gameObject.SetActive(false); // Ховаємо UI вибору кольору
        shadow.SetActive(false);
    }
}
