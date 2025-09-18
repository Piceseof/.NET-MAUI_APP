using Games.Services;
using Games.Models;

namespace Games.Views;

public partial class SettingPage : ContentPage
{
	private readonly SettingDatabase _settingRepo;
	private bool isNavigating = false;
	
	public SettingPage()
	{
		InitializeComponent();
		_settingRepo = App.SettingRepo;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		
		try
		{
			// 初始化设置
			await InitializeSettings();
			
			// 更新游戏时长显示
			await UpdateGameTimeDisplay();
			
			// 淡入显示页面
			await this.FadeTo(1, 500);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnAppearing: {ex.Message}");
		}
	}

	private async Task InitializeSettings()
	{
		try
		{
			// 从设置中读取各项设置
			var setting = await _settingRepo.GetSettingById(1);
			if (setting != null)
			{
				// 背景音乐开关
				MusicSwitch.IsToggled = setting.IsMusicEnabled;
				
				// 背景音乐音量
				MusicVolumeSlider.Value = setting.MusicVolume;
				
				// 确保音频服务已重置
				await App.AudioService.ResetAudio();
				
				// 根据保存的设置初始化音乐状态
				if (setting.IsMusicEnabled)
				{
					await App.AudioService.PlayBackgroundMusic();
				}
				else
				{
					App.AudioService.PauseBackgroundMusic();
				}
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"初始化设置时出错: {ex.Message}");
		}
	}
	
	private async Task UpdateGameTimeDisplay()
	{
		try
		{
			// 临时停止计时
			await App.StopGameTimeTracking();
			
			// 获取总游戏时长
			long totalGameSeconds = await _settingRepo.GetGamePlayTimeAsync(1);
			
			// 更新显示
			GameTimeLabel.Text = App.FormatGameTime(totalGameSeconds);
			
			// 重新开始计时
			await App.StartGameTimeTracking();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error updating game time: {ex.Message}");
		}
	}
	
	public async void OnRefreshTimeClicked(object sender, EventArgs e)
	{
		await UpdateGameTimeDisplay();
	}

	public async void OnBackClicked(object sender, EventArgs e)
	{
        string backPage = await _settingRepo.GetBackPageById(1);
        await Shell.Current.GoToAsync($"//{backPage}");
    }

	public async void OnMusicToggled(object sender, ToggledEventArgs e)
	{
		try
		{
			await App.SettingRepo.UpdateMusicSetting(1, e.Value);
			
			// 先重置音频服务，确保资源正确初始化
			await App.AudioService.ResetAudio();
			
			// 根据开关状态控制音乐
			if (e.Value)
			{
				await App.AudioService.PlayBackgroundMusic();
			}
			else
			{
				App.AudioService.PauseBackgroundMusic();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnMusicToggled: {ex.Message}");
		}
	}

	public async void OnMusicVolumeChanged(object sender, ValueChangedEventArgs e)
	{
		try
		{
			// 更新背景音乐音量
			await App.SettingRepo.UpdateMusicVolume(1, e.NewValue);
			await App.AudioService.SetBackgroundMusicVolume(e.NewValue);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnMusicVolumeChanged: {ex.Message}");
		}
	}

	public async void OnResetGameClicked(object sender, EventArgs e)
	{
		// 使用系统消息框显示确认对话框，而不是自定义Frame
		bool confirm = await DisplayAlert(
			"重置游戏", 
			"确定要重置游戏吗？所有进度将丢失！", 
			"确认重置", 
			"取消");
			
		if (confirm)
		{
			// 用户点击确认，执行重置操作
			await ResetGame();
		}
	}
	
	// 将重置游戏的逻辑抽取到单独的方法
	private async Task ResetGame()
	{
		if (isNavigating) return;
		
		isNavigating = true;
		
		try
		{
            // 重置设置数据
            await _settingRepo.ResetGame(1);
            // 1. 重置游戏状态数据
            await App.GameStateRepo.ResetGameStatesAsync();
			
			// 2. 重置物品栏数据
			await App.InventoryRepo.ResetInventoryAsync();
			
			// 3. 重置游戏时长
			await _settingRepo.UpdateGamePlayTimeAsync(1, 0);
			await _settingRepo.UpdateLastSessionStartTimeAsync(1, DateTimeOffset.Now.ToUnixTimeSeconds());
			
			// 4. 保留音乐设置但重置其他设置
			var setting = await _settingRepo.GetSettingById(1);
			if (setting != null)
			{
				bool musicEnabled = setting.IsMusicEnabled;
				double musicVolume = setting.MusicVolume;
				
				// 重置设置
				setting.IsMusicEnabled = musicEnabled;
				setting.MusicVolume = musicVolume;
				setting.GamePlayTimeSeconds = 0;
				setting.LastSessionStartTime = DateTimeOffset.Now.ToUnixTimeSeconds();
				setting.Archive = 0; // 将Archive设置为0，表示无存档
				setting.LastActivePage = "Room1Wall1";
 

                await _settingRepo.UpdateSetting(setting);
			}
			
			// 5. 更新游戏时长显示
			GameTimeLabel.Text = "0小时0分0秒";
			
			// 6. 显示重置成功的消息
			await DisplayAlert("成功", "游戏已重置成功！所有游戏进度已清除。", "确定");
			
			// 7. 淡出当前页面准备导航
			await this.FadeTo(0, 300);
			
			// 8. 导航到主菜单页面
			await Shell.Current.GoToAsync("//StartGamePage");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"游戏重置出错: {ex.Message}");
			await DisplayAlert("错误", $"重置游戏时出现错误：{ex.Message}", "确定");
			this.Opacity = 1; // 确保页面可见
		}
		finally
		{
			isNavigating = false; // Ensure isNavigating is reset
		}
	}
}