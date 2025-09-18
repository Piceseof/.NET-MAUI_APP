namespace Games.Views;

public partial class ComputerPuzzle : ContentPage
{
	private bool isNavigating = false;
	private StackLayout _inventoryLayout;
	private BoxView _currentSelectedBackground;
	private Image _currentSelectedItem;

	public ComputerPuzzle()
	{
		InitializeComponent();
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		this.Opacity = 0;
		isNavigating = false;

		try
		{
			// 初始化物品栏
			_inventoryLayout = this.FindByName<StackLayout>("InventoryLayout");
			
			// 重置密码输入框和消息label
			PasswordEntry.Text = string.Empty;
			MessageLabel.IsVisible = false;
			
			// 检查当前门的状态
			bool isDoorUnlocked = await App.GameStateRepo.GetStateAsync("DoorUnlocked");
			if (!isDoorUnlocked)
			{
				// 如果门仍然锁着，确保消息标签被重置
				MessageLabel.Text = "";
				MessageLabel.IsVisible = false;
			}
			
			// 清空物品栏显示
			for (int i = 0; i < _inventoryLayout.Children.Count; i++)
			{
				_inventoryLayout.Children[i] = new Image
				{
					Source = "inventory.png",
					Aspect = Aspect.AspectFill,
					WidthRequest = 50,
					HeightRequest = 50
				};
			}
			
			// 从数据库加载物品
			var items = await App.InventoryRepo.GetInventoryItemsAsync();
			
			// 如果有任何画碎片，在物品栏显示
			var anyPictureFragment = items.FirstOrDefault(i => i.ItemName.Contains("picture_fragment"));
			if (anyPictureFragment != null)
			{
				UpdateInventorySlot(anyPictureFragment.SlotIndex, "sofa_picture_fragment");
			}
			
			// 加载其他非画碎片物品 - 过滤掉已使用的物品
			foreach (var item in items.Where(i => 
				!i.ItemName.Contains("picture_fragment") && 
				!i.IsUsed && // 添加这个条件来过滤掉已使用的物品
				i.IsCollected))
			{
				UpdateInventorySlot(item.SlotIndex, item.ItemName);
			}
			
			await this.FadeTo(1, 500);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnAppearing: {ex.Message}");
			this.Opacity = 1;
		}
	}

	private void UpdateInventorySlot(int slotIndex, string itemName)
	{
		if (slotIndex < 0 || slotIndex >= _inventoryLayout.Children.Count)
			return;

		var grid = new Grid
		{
			WidthRequest = 50,
			HeightRequest = 50
		};

		var originalInventory = new Image
		{
			Source = "inventory.png",
			Aspect = Aspect.AspectFill,
			WidthRequest = 50,
			HeightRequest = 50
		};

		var inventoryItem = new Image
		{
			Source = itemName + ".png",
			WidthRequest = 40,
			HeightRequest = 40,
			Aspect = Aspect.AspectFill,
			Margin = new Thickness(5)
		};

		var whiteBackground = new BoxView
		{
			Color = Colors.White,
			IsVisible = false,
			WidthRequest = 50,
			HeightRequest = 50
		};

		grid.Children.Add(originalInventory);
		grid.Children.Add(whiteBackground);
		grid.Children.Add(inventoryItem);

		var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += async (s, e) => 
		{
			await OnInventoryItemClicked(grid, itemName, whiteBackground, inventoryItem);
		};
		grid.GestureRecognizers.Add(tapGestureRecognizer);

		_inventoryLayout.Children[slotIndex] = grid;
	}

	private async Task OnInventoryItemClicked(Grid grid, string itemName, BoxView whiteBackground, Image inventoryItem)
	{
		if (string.IsNullOrEmpty(itemName) || itemName == "inventory.png")
			return;

		if (_currentSelectedBackground != null && _currentSelectedItem != null)
		{
			_currentSelectedBackground.IsVisible = false;
			_currentSelectedItem.Scale = 1.0;
		}

		whiteBackground.IsVisible = true;
		await inventoryItem.ScaleTo(1.2, 100);

		_currentSelectedBackground = whiteBackground;
		_currentSelectedItem = inventoryItem;

		// 播放物品选择音效
		await App.AudioService.PlayItemPickupSound();

		string itemInfo;
		if (itemName.Contains("picture_fragment"))
		{
			var items = await App.InventoryRepo.GetInventoryItemsAsync();
			int fragmentCount = items.Count(i => i.ItemName.Contains("picture_fragment"));
			itemInfo = $"画碎片 {fragmentCount}/5";
		}
		else
		{
			itemInfo = GetItemInfo(itemName);
		}

		ShowHint(itemInfo);
	}

	private string GetItemInfo(string itemName)
	{
		return itemName switch
		{
			"torch" => "一个紫外线手电筒",
			"knife" => "一把小刀",
			"dishcloth" => "衣服的一角",
			"battery" => "一节电池",
			_ => "未知物品"
		};
	}

	private void OnBackgroundTapped(object sender, EventArgs e)
	{
		if (_currentSelectedBackground != null && _currentSelectedItem != null)
		{
			_currentSelectedBackground.IsVisible = false;
			_currentSelectedItem.Scale = 1.0;
			_currentSelectedBackground = null;
			_currentSelectedItem = null;
		}
	}

	public async void OnBackClicked(object sender, EventArgs e)
	{
		if (!isNavigating && this.Opacity > 0)
		{
			isNavigating = true;
			await this.FadeTo(0, 500);
			
			await Shell.Current.GoToAsync("//Room1Wall4");
		}
	}

	public async void OnSettingClicked(object sender, EventArgs e)
	{
		if (!isNavigating && this.Opacity > 0)
		{
			isNavigating = true;
			await App.SettingRepo.UpdateBackPage(1, "ComputerPuzzle");
			await Shell.Current.GoToAsync("//SettingPage");
		}
	}

	private async void OnConfirmClicked(object sender, EventArgs e)
	{
		// 检查密码是否正确（假设密码是 "lome"）
		if (PasswordEntry.Text == "lome")
		{
			MessageLabel.TextColor = Colors.Green;
			MessageLabel.Text = "密码正确！";
			MessageLabel.IsVisible = true;
			
			// 显示提示消息
			ShowHint("门锁已经打开！！！");
			
			// 更新游戏状态，标记门已解锁
			await App.GameStateRepo.SaveStateAsync("DoorUnlocked", true);
			
			// 清除密码输入框
			PasswordEntry.Text = string.Empty;
		}
		else
		{
			// 密码错误的处理
			MessageLabel.TextColor = Colors.Red;
			MessageLabel.Text = "密码错误，请重试";
			MessageLabel.IsVisible = true;
			
			// 清除密码输入框
			PasswordEntry.Text = string.Empty;
		}
	}

	private void ShowHint(string message)
	{
		// 设置基本文本内容
		FragmentHintLabel.Text = message;
		FragmentHintLabel.Opacity = 0;
		FragmentHintLabel.IsVisible = true;
		
		// 确保所有类型的提示都显示在顶部游戏区域中央
		FragmentHintLabel.VerticalOptions = LayoutOptions.Start;
		FragmentHintLabel.HorizontalOptions = LayoutOptions.Center;
		FragmentHintLabel.Margin = new Thickness(0, 10, 0, 0);
		
		// 文本格式设置
		FragmentHintLabel.FontSize = 24;
		FragmentHintLabel.FontAttributes = FontAttributes.Bold;
		FragmentHintLabel.CharacterSpacing = 0.5;
		FragmentHintLabel.MaxLines = 3;
		FragmentHintLabel.LineBreakMode = LineBreakMode.WordWrap;
		FragmentHintLabel.HorizontalTextAlignment = TextAlignment.Center;
		FragmentHintLabel.VerticalTextAlignment = TextAlignment.Center;
		FragmentHintLabel.Padding = new Thickness(25, 15, 25, 15);
		
		// 尺寸控制 - 确保宽度适中，适合非物品栏区域
		FragmentHintLabel.WidthRequest = 600;
		FragmentHintLabel.MaximumWidthRequest = 700;
		
		// 视觉效果
		FragmentHintLabel.Background = new SolidColorBrush(Color.FromRgba(0, 0, 0, 0.2));
		FragmentHintLabel.Shadow = new Shadow
		{
			Brush = Brush.Black,
			Offset = new Point(3, 3),
			Radius = 5,
			Opacity = 0.7f
		};
		
		// 淡入动画
		FragmentHintLabel.FadeTo(1, 500, Easing.CubicOut);
		
		// 延迟后淡出
		Microsoft.Maui.Controls.Application.Current.Dispatcher.DispatchAsync(async () =>
		{
			await Task.Delay(2200);
			await FragmentHintLabel.FadeTo(0, 500, Easing.CubicIn);
			FragmentHintLabel.IsVisible = false;
		});
	}
}