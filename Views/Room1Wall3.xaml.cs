namespace Games.Views;
using Games.Models;
using Games.Services;

public partial class Room1Wall3 : ContentPage
{
    private bool isNavigating = false;
    private StackLayout _inventoryLayout;
	private bool _hasPictureFragment = false;
	private BoxView _currentSelectedBackground;
	private Image _currentSelectedItem;

	public Room1Wall3()
	{
		InitializeComponent();
		
		// 添加页面背景的点击事件（排除物品栏区域）
		var backgroundTapGesture = new TapGestureRecognizer();
		backgroundTapGesture.Tapped += OnBackgroundTapped;
		RoomOneWallImage.GestureRecognizers.Add(backgroundTapGesture);
	}

	private void OnBackgroundTapped(object sender, EventArgs e)
	{
		// 点击背景时取消物品选中状态
		if (_currentSelectedBackground != null && _currentSelectedItem != null)
		{
			_currentSelectedBackground.IsVisible = false;
			_currentSelectedItem.Scale = 1.0;
			_currentSelectedBackground = null;
			_currentSelectedItem = null;
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_inventoryLayout == null)
		{
			_inventoryLayout = this.FindByName<StackLayout>("InventoryLayout");
		}

		// 重置状态
		_hasPictureFragment = false;
		
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
		
		// 检查当前房间的画碎片是否已被收集
		var sofaPictureFragment = await App.InventoryRepo.GetInventoryItemAsync("sofa_picture_fragment");
		if (sofaPictureFragment != null && sofaPictureFragment.IsCollected)
		{
			_hasPictureFragment = true;
			SofaPictureFragment.IsVisible = false;
		}
		else
		{
			SofaPictureFragment.IsVisible = true;
		}

		// 如有任何画碎片，在物品栏显示
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
			Source = itemName,
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
		try
		{
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
				int fragmentCount = items.Count(i => i.ItemName.Contains("picture_fragment") && i.IsCollected);
				itemInfo = $"画碎片 {fragmentCount}/5";
			}
			else
			{
				itemInfo = GetItemInfo(itemName);
			}

			// 使用新的显示提示方法
			ShowHint(itemInfo);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnInventoryItemClicked: {ex.Message}");
		}
	}

	private string GetItemInfo(string itemName)
	{
		return itemName switch
		{
            "torch" => "一个紫外线手电筒",
            "knife" => "一把小刀",
            "dishcloth" => "衣服的一角",
            "battery" => "一节电池",
			// 添加其他物品的描述...
			_ => "未知物品"
		};
	}

	private async void OnSofaPictureFragmentClicked(object sender, EventArgs e)
	{
		if (!_hasPictureFragment && _inventoryLayout != null)
		{
			int slotIndex;
			// 先检查是否有任何画碎片
			var anyPictureFragment = (await App.InventoryRepo.GetInventoryItemsAsync())
				.FirstOrDefault(i => i.ItemName.Contains("picture_fragment"));
				
			if (anyPictureFragment != null)
			{
				// 如果已经有任何画碎片，使用相同的位置
				slotIndex = anyPictureFragment.SlotIndex;
			}
			else
			{
				// 找到第一个空的物品栏位置
				slotIndex = -1;
				for (int i = 0; i < _inventoryLayout.Children.Count; i++)
				{
					if (_inventoryLayout.Children[i] is Image slot && 
						slot.Source.ToString().EndsWith("inventory.png"))
					{
						slotIndex = i;
						break;
					}
				}
			}

			if (slotIndex != -1)
			{
				// 保存到数据库
				var newItem = new InventoryItem
				{
					ItemName = "sofa_picture_fragment",
					SlotIndex = slotIndex,
					IsCollected = true
				};
				await App.InventoryRepo.SaveInventoryItemAsync(newItem);

				// 更新UI和状态
				SofaPictureFragment.IsVisible = false;
				UpdateInventorySlot(slotIndex, "sofa_picture_fragment");
				_hasPictureFragment = true;

				// 显示提示文本和动画效果
				var items = await App.InventoryRepo.GetInventoryItemsAsync();
				int fragmentCount = items.Count(i => i.ItemName.Contains("picture_fragment"));
				FragmentHintLabel.Text = $"画碎片 {fragmentCount}/5";
				FragmentHintLabel.Opacity = 0;
				FragmentHintLabel.IsVisible = true;

				// 同时执行文本和物品栏动画
				if (_inventoryLayout.Children[slotIndex] is Grid grid && 
					grid.Children.LastOrDefault() is Image inventoryItem)
				{
					// 启动字幕淡入动画但不等待它完成
					FragmentHintLabel.FadeTo(1, 500);
					
					// 执行画碎片的放大缩小动画
					await inventoryItem.ScaleTo(1.2, 100);
					await inventoryItem.ScaleTo(1.0, 100);
					
					// 等待显示时间后淡出字幕
					await Task.Delay(1800);
					await FragmentHintLabel.FadeTo(0, 500);
				}

				FragmentHintLabel.IsVisible = false;
			}
		}
	}

	public async void OnLeftButtonClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//Room1Wall2");
	}

	public async void OnRightButtonClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//Room1Wall4");
	}

	public async void OnSettingClicked(object sender, EventArgs e)
	{
		await App.SettingRepo.UpdateBackPage(1, "Room1Wall3");
		await Shell.Current.GoToAsync("//SettingPage");
	}
    public async void OnBookshelfClicked(object sender, EventArgs e)
    {
        await this.FadeTo(0, 500);
        await Shell.Current.GoToAsync("//MoveBooksPuzzle");
    }

	public async void OnBookShelfMoveClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//BookShelfMove");
	}
    private void bookShelfTapped(object sender, EventArgs e)
    {
        // 更新提示文字
        ShowHint("书架里好像有什么");
        

       
    }
    
    private void ladderTapped(object sender, EventArgs e)
    {

        // 更新提示文字
        ShowHint("太高了！");
        
       
    }
    private void backTapped(object sender, EventArgs e)
    {
		if (!_hasPictureFragment)
		{
            // 更新提示文字
           
            ShowHint("沙发下面好像有什么东西");
          
			
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