using UnityEngine;

public class GameOverUI : MonoBehaviour
{
    
    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void BackToMainMenu()
    {
        BlackUI.instance.DoFade(1,1,() =>
        {
            UIManager.instance.gameOverUI.gameObject.SetActive(false);
            UIManager.instance.mainMenuUI.gameObject.SetActive(true);
        });

        
    }



}
