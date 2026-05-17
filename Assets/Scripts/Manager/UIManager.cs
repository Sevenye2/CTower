using CardTower.UI;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager instance;
    public void Awake()
    {
        instance = this;
    }

    public BlackUI blackUI;
    public BattleHUD battleHUD;
    public MainMenuUI mainMenuUI;
    public ShopUI shopUI;
    public GameOverUI gameOverUI;

}
