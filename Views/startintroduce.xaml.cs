namespace Games.Views;

public partial class StartIntroduce : ContentPage
{
	private bool isNavigating = false;
	
	public StartIntroduce()
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
			
			// 淡出效果
			await this.FadeTo(0, 500);
			
			// 导航到第一个房间
			await Shell.Current.GoToAsync("//Room1Wall1");
		}
	}
}