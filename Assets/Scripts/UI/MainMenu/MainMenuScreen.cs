using TMPro;
using UnityEngine;

public class MainMenuScreen : UIScreen
{
    [Header("Buttons")]
    [SerializeField] private UIButton newGameButton;
    [SerializeField] private UIButton loadGameButton;
    [SerializeField] private UIButton settingsButton;
    [SerializeField] private UIButton quitButton;

    [Header("Info")]
    [SerializeField] private TMP_Text versionText;

    protected override void OnInitialize()
    {
        if (newGameButton != null)
        {
            newGameButton.Label = "新游戏";
            newGameButton.OnClicked += HandleNewGame;
        }

        if (loadGameButton != null)
        {
            loadGameButton.Label = "加载存档";
            loadGameButton.Interactable = false;
        }

        if (settingsButton != null)
        {
            settingsButton.Label = "设置";
            settingsButton.Interactable = false;
        }

        if (quitButton != null)
        {
            quitButton.Label = "退出游戏";
            quitButton.OnClicked += HandleQuit;
        }

        if (versionText != null)
            versionText.text = Application.version;
    }

    private void OnDestroy()
    {
        if (newGameButton != null) newGameButton.OnClicked -= HandleNewGame;
        if (quitButton != null) quitButton.OnClicked -= HandleQuit;
    }

    private void HandleNewGame()
    {
        uiManager.RequestNewGame();
    }

    private void HandleQuit()
    {
        uiManager.RequestQuit();
    }
}
