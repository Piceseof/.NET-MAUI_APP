namespace Games;
using Games.Views;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // 注册路由
        Routing.RegisterRoute("StartGamePage", typeof(Views.StartGamePage));
        Routing.RegisterRoute("SettingPage", typeof(Views.SettingPage));
        
        // 确保其他页面也被注册
        Routing.RegisterRoute("Room1Wall1", typeof(Views.Room1Wall1));
        Routing.RegisterRoute(nameof(StartIntroduce), typeof(StartIntroduce));
        Routing.RegisterRoute(nameof(AfterIntroduce), typeof(AfterIntroduce));
    }
}

