namespace Games.Views;

public partial class AfterIntroduce : ContentPage
{
    private bool isNavigating = false;
    
    public AfterIntroduce()
    {
        InitializeComponent();
    }
    
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        
        // 淡入显示页面
        this.Opacity = 0;
        await this.FadeTo(1, 800, Easing.CubicOut);
        isNavigating = false;
    }
    
    private async void OnPageTapped(object sender, EventArgs e)
    {
        if (!isNavigating)
        {
            isNavigating = true;
            
            try
            {
                // 淡出效果
                await this.FadeTo(0, 500);
                
                // 重置游戏状态，以便下次开始新游戏时门是锁着的
                await App.GameStateRepo.SaveStateAsync("DoorUnlocked", false);
                
                // 导航回开始菜单
                await Shell.Current.GoToAsync("//StartGamePage");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnPageTapped: {ex.Message}");
                // 确保页面可见
                this.Opacity = 1;
                isNavigating = false;
            }
        }
    }
} 