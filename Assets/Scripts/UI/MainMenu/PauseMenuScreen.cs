using UnityEngine;

public class PauseMenuScreen : UIScreen
{
    [SerializeField] UIButton continueBtn, settingsBtn, saveBtn, mainMenuBtn;

    protected override void OnInitialize()
    {
        continueBtn.SetText("继续游戏");
        continueBtn.OnClicked += HandleContinue;
        settingsBtn.SetText("设置");
        settingsBtn.SetInteractable(false);
        saveBtn.SetText("保存");
        saveBtn.SetInteractable(false);
        mainMenuBtn.SetText("返回主菜单");
        mainMenuBtn.OnClicked += HandleMainMenu;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        continueBtn.OnClicked -= HandleContinue;
        mainMenuBtn.OnClicked -= HandleMainMenu;
    }

    void HandleContinue() => uiManager.RequestResume();
    void HandleMainMenu() => uiManager.RequestMainMenu();
}
