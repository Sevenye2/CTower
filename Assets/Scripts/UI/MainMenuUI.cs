using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    public Button continueBtn;
    
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        continueBtn.gameObject.SetActive(SaveDataManager.instance.isPlaying);
    }

    public void ContinueGameBtnClick()
    {
        
    }

    public void NewGameBtnClick()
    {
        BlackUI.instance.DoFade(1,1, ()=>
        {
            SaveDataManager.instance.New();
            BattleManager.instance.Begin();
            gameObject.SetActive(false);
        });

    }

}
