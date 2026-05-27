using UnityEngine;

public class MainMenuScreen : UIScreen
{
    [Header("Buttons")]
    [SerializeField] private UIButton newGameButton;
    [SerializeField] private UIButton loadGameButton;
    [SerializeField] private UIButton settingsButton;
    [SerializeField] private UIButton quitButton;

    [Header("Info")]
    [SerializeField] private UILabel versionText;

    protected override void OnInitialize()
    {
        if (newGameButton != null)
        {
            newGameButton.SetText("新游戏");
            newGameButton.OnClicked += HandleNewGame;
        }

        if (loadGameButton != null)
        {
            loadGameButton.SetText("加载存档");
            loadGameButton.SetInteractable(false);
        }

        if (settingsButton != null)
        {
            settingsButton.SetText("设置");
            settingsButton.SetInteractable(false);
        }

        if (quitButton != null)
        {
            quitButton.SetText("退出游戏");
            quitButton.OnClicked += HandleQuit;
        }

        if (versionText != null)
            versionText.SetText(Application.version);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        if (newGameButton != null) newGameButton.OnClicked -= HandleNewGame;
        if (quitButton != null) quitButton.OnClicked -= HandleQuit;
    }

    private void HandleNewGame()
    {
        uiService.RequestNewGame();
    }

    private void HandleQuit()
    {
        uiService.RequestQuit();
    }
}
