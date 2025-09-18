using Games.Services;  // 添加这行
namespace Games.Views;

public partial class StartGamePage : ContentPage
{
    private bool isNavigating = false;

    public StartGamePage()
    {
        InitializeComponent();
    }

    private async Task InitializeBtnAsync()
    {
        try
        {
            // 获取主Grid
            if (Content is Grid mainGrid)
            {
                // 移除所有StackLayout（按钮容器）
                var stackLayouts = mainGrid.Children.OfType<StackLayout>().ToList();
                foreach (var stackLayout in stackLayouts)
                {
                    mainGrid.Children.Remove(stackLayout);
                }
            }

            if (!await App.SettingRepo.IsSettingExists(1))
            {
                await App.SettingRepo.AddNewSetting(1, 0, "StartGamePage");
            }

            var archive = await App.SettingRepo.GetArchiveById(1);
            if (archive > 0)
            {
                StartGameBtn2();
            }
            else 
            {
                StartGameBtn1();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"初始化按钮出错: {ex.Message}");
            StartGameBtn1(); // 出错时显示默认按钮
        }
    }

    public async void OnSettingClicked(object sender, EventArgs e)
    {
        await App.SettingRepo.UpdateBackPage(1, "StartGamePage");
        await Shell.Current.GoToAsync("//SettingPage");
    }

    public async void OnStartGameClicked(object sender, EventArgs e)
    {
        if (!isNavigating)
        {
            isNavigating = true;
            
            try
            {
                // 淡出效果
                await this.FadeTo(0, 500);
                
                // 如果是"新游戏"按钮，重置游戏状态
                // 检查发送者是否包含"新游戏"文本
                if (sender is Button button && button.Text == "新游戏")
                {
                    Console.WriteLine("Starting new game, resetting game states");
                    
                    // 重置门锁状态
                    await App.GameStateRepo.SaveStateAsync("DoorUnlocked", false);
                    
                    // 完全重置物品栏
                    await App.InventoryRepo.ResetInventoryAsync();
                    
                    // 重置其他游戏状态
                    // ...
                }
                
                // 导航到介绍页面
                await Shell.Current.GoToAsync("//StartIntroduce");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnStartGameClicked: {ex.Message}");
                // 确保页面可见
                this.Opacity = 1;
                isNavigating = false;
            }
        }
    }

    public void StartGameBtn1()
    {
        var startgameBtn = new Button
        {
            Text = "新游戏",
            HorizontalOptions = LayoutOptions.Center,
            BackgroundColor = Colors.Purple,
            TextColor = Colors.White,
            Margin = new Thickness(0, 5),
            WidthRequest = 100,
            HeightRequest = 50
        };
        startgameBtn.Clicked += OnStartGameClicked;

        var stackLayout = new StackLayout
        {
            Orientation = StackOrientation.Vertical,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 10,
            Margin = new Thickness(0, 100, 0, 0),  // 向下移动100单位
            TranslationX = 0,  // 水平偏移，正值向右
            TranslationY = 100 // 垂直偏移，正值向下
        };
        stackLayout.Children.Add(startgameBtn);

        if (Content is Grid grid)
        {
            grid.Children.Add(stackLayout);
        }
    }

    public void StartGameBtn2()
    {
        var continueGameBtn = new Button
        {
            Text = "继续游戏",
            HorizontalOptions = LayoutOptions.Center,
            BackgroundColor = Colors.Purple,
            TextColor = Colors.White,
            Margin = new Thickness(0, 5),
            WidthRequest = 100,
            HeightRequest = 50
        };
        continueGameBtn.Clicked += OnStartGameClicked;

        var newGameBtn = new Button
        {
            Text = "新游戏",
            HorizontalOptions = LayoutOptions.Center,
            BackgroundColor = Colors.Purple,
            TextColor = Colors.White,
            Margin = new Thickness(0, 5),
            WidthRequest = 100,
            HeightRequest = 50
        };
        newGameBtn.Clicked += OnStartGameClicked;

        var stackLayout = new StackLayout
        {
            Orientation = StackOrientation.Vertical,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 0,
            Margin = new Thickness(0, 0, 0, 30),  // 向下移动100单位
            TranslationX = 0,  // 水平偏移，正值向右
            TranslationY = 70 // 垂直偏移，正值向下
        };
        stackLayout.Children.Add(continueGameBtn);
        stackLayout.Children.Add(newGameBtn);

        if (Content is Grid grid)
        {
            grid.Children.Add(stackLayout);
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.Opacity = 0;
        await InitializeBtnAsync();
        await this.FadeTo(1, 750);
        isNavigating = false;
        await CheckAndPlayBackgroundMusic();
    }

    private async Task CheckAndPlayBackgroundMusic()
    {
        try
        {
            var setting = await App.SettingRepo.GetSettingById(1);
            if (setting != null && setting.IsMusicEnabled)
            {
                await App.AudioService.PlayBackgroundMusic();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"检查并播放背景音乐时出错: {ex.Message}");
        }
    }
}