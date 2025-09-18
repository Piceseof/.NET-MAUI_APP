namespace Games.Views;
using Games.Models;

public partial class Room1Wall4 : ContentPage
{
	private StackLayout _inventoryLayout;
	private bool _hasPictureFragment = false;
	private BoxView _currentSelectedBackground;
	private Image _currentSelectedItem;
	private bool _hasCloth = false;
	private bool isNavigating = false;

	public Room1Wall4()
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
		
		// 设置初始透明度为0
		this.Opacity = 0;
		
		// 重置导航标志，确保门可以再次点击
		isNavigating = false;
		
		try 
		{
			// 初始化物品栏
			_inventoryLayout = this.FindByName<StackLayout>("InventoryLayout");
			if (_inventoryLayout == null)
			{
				Console.WriteLine("Error: InventoryLayout not found");
				return;
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
			var clothPictureFragment = await App.InventoryRepo.GetInventoryItemAsync("cloth_picture_fragment");
			if (clothPictureFragment != null && clothPictureFragment.IsCollected)
			{
				_hasPictureFragment = true;
				ClothPictureFragment.IsVisible = false;
			}
			else
			{
				ClothPictureFragment.IsVisible = true;
			}

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

			// 检查是否已经获得过抹布
			var dishCloth = await App.InventoryRepo.GetInventoryItemAsync("dishcloth");
			_hasCloth = dishCloth != null && dishCloth.IsCollected;

			// 最后淡入显示页面
			await this.FadeTo(1, 500);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnAppearing: {ex.Message}");
			// 确保页面至少是可见的
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

			// 使用ShowHint方法显示提示
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

        };
	}

    private async void OnClothPictureFragmentClicked(object sender, EventArgs e)
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
                    ItemName = "cloth_picture_fragment",
                    SlotIndex = slotIndex,
                    IsCollected = true
                };
                await App.InventoryRepo.SaveInventoryItemAsync(newItem);

                // 更新UI和状态
                ClothPictureFragment.IsVisible = false;
                UpdateInventorySlot(slotIndex, "sofa_picture_fragment"); // 使用相同的图片
                _hasPictureFragment = true;

                // 显示提示文本和动画效果
                var items = await App.InventoryRepo.GetInventoryItemsAsync();
                int fragmentCount = items.Count(i => i.ItemName.Contains("picture_fragment"));
                FragmentHintLabel.Text = $"画碎片 {fragmentCount}/5";
                FragmentHintLabel.Opacity = 0;
                FragmentHintLabel.IsVisible = true;

                // 同时执行文本和物品栏动画
                var textAnimation = new List<Task>
                {
                    FragmentHintLabel.FadeTo(1, 500),
                    Task.Delay(2000),
                    FragmentHintLabel.FadeTo(0, 500)
                };

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
		await Shell.Current.GoToAsync("//Room1Wall3");
	}

	public async void OnRightButtonClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//Room1Wall1");
	}

	public async void OnSettingClicked(object sender, EventArgs e)
	{
		await App.SettingRepo.UpdateBackPage(1, "Room1Wall4");
		await Shell.Current.GoToAsync("//SettingPage");
	}
    public async void OnLaptopClicked(object sender, EventArgs e)
    {
        await this.FadeTo(0, 500);
        await Shell.Current.GoToAsync("//ComputerPuzzle");
    }
    public async void OnClothClicked(object sender, EventArgs e)
	{
		// 检查是否已经获得过抹布
		if (_hasCloth)
		{
			ShowHint("你已经拿到了衣服的一角");
			return;
		}

		// 检查是否选中了小刀
		if (_currentSelectedItem?.Source?.ToString()?.Contains("knife") != true)
		{
			ShowHint("你猜为什么会给你一把刀呢");
			return;
		}

		// 找到第一个空的物品栏位置
		int slotIndex = -1;
		for (int i = 0; i < _inventoryLayout.Children.Count; i++)
		{
			if (_inventoryLayout.Children[i] is Image slot && 
				slot.Source.ToString().EndsWith("inventory.png"))
			{
				slotIndex = i;
				break;
			}
		}

		if (slotIndex != -1)
		{
			try {
				// 保存到数据库 - 添加衣服碎片
				var newItem = new InventoryItem
				{
					ItemName = "dishcloth",
					SlotIndex = slotIndex,
					IsCollected = true,
					IsUsed = false
				};
				await App.InventoryRepo.SaveInventoryItemAsync(newItem);

				// 更新UI和状态
				UpdateInventorySlot(slotIndex, "dishcloth");
				_hasCloth = true;

				// 显示获得物品的提示
				ShowHint("获得了衣服的一角");

				// 显示动画效果
				if (_inventoryLayout.Children[slotIndex] is Grid grid && 
					grid.Children.LastOrDefault() is Image inventoryItem)
				{
					// 执行物品放大缩小动画
					await inventoryItem.ScaleTo(1.2, 100);
					await inventoryItem.ScaleTo(1.0, 100);
				}

				// 从数据库获取小刀并标记为已使用
				var knife = await App.InventoryRepo.GetInventoryItemAsync("knife");
				if (knife != null)
				{
					knife.IsUsed = true;  // 标记小刀为已使用
					await App.InventoryRepo.SaveInventoryItemAsync(knife);
					
					// 查找小刀在物品栏中的位置
					int knifeIndex = -1;
					for (int i = 0; i < _inventoryLayout.Children.Count; i++)
					{
						if (_inventoryLayout.Children[i] is Grid g && 
							g.Children.LastOrDefault() is Image img && 
							img.Source?.ToString()?.Contains("knife") == true)
						{
							knifeIndex = i;
							break;
						}
					}
					
				
				}

				// 取消小刀的选中状态
				if (_currentSelectedBackground != null && _currentSelectedItem != null)
				{
					_currentSelectedBackground.IsVisible = false;
					_currentSelectedItem.Scale = 1.0;
					_currentSelectedBackground = null;
					_currentSelectedItem = null;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Error in OnClothClicked: {ex.Message}");
			}
		}
	}
    private async void doorTapped(object sender, EventArgs e)
    {
        // 防止多次快速点击
        if (isNavigating)
            return;
        
        try
        {
            // 检查门是否已解锁
            bool isDoorUnlocked = await App.GameStateRepo.GetStateAsync("DoorUnlocked");
            
            if (isDoorUnlocked)
            {
                // 门已解锁，导航到结束页面
                isNavigating = true;
                Console.WriteLine("Door is unlocked, navigating to AfterIntroduce");
                
                // 淡出效果
                await this.FadeTo(0, 500);
                
                // 导航到结束页面
                await Shell.Current.GoToAsync("//AfterIntroduce");
            }
            else
            {
                // 门仍然锁着，显示提示
                Console.WriteLine("Door is locked, showing hint");
                ShowHint("门锁了");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Door click error: {ex.Message}");
            ShowHint("门锁了");
        }
    }
    
    private void hangerTapped(object sender, EventArgs e)
    {
		if (!_hasPictureFragment)
		{
			ShowHint("衣架后面是不是有什么东西");
		}
    }
    
    private void clothTapped(object sender, EventArgs e)
    {
        ShowHint("你猜为什么会给你一把刀呢");
    }

    private void SomeClickEvent(object sender, EventArgs e)
    {
        // 使用新的ShowHint方法显示提示
        ShowHint("提示信息");
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


    private void boxTapped(object sender, EventArgs e)
    {
        ShowHint("箱子上有一个密码锁");
    }

    private void computerTapped(object sender, EventArgs e)
    {
        ShowHint("电脑需要密码才能打开");
    }

    private void deskTapped(object sender, EventArgs e)
    {
        ShowHint("桌子上有一台电脑");
    }

    private async void OnPictureFragmentClicked(object sender, EventArgs e)
    {
        try
        {
            if (_hasPictureFragment)
            {
                ShowHint("你已经收集了这个画碎片");
                return;
            }

            // 找到一个空的物品栏位置
            var items = await App.InventoryRepo.GetInventoryItemsAsync();
            var anyPictureFragment = items.FirstOrDefault(i => i.ItemName.Contains("picture_fragment"));
            int slotIndex;

            if (anyPictureFragment != null)
            {
                slotIndex = anyPictureFragment.SlotIndex;
            }
            else
            {
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
                // 先保存到数据库
                var newItem = new InventoryItem
                {
                    ItemName = "cloth_picture_fragment",
                    SlotIndex = slotIndex,
                    IsCollected = true
                };
                await App.InventoryRepo.SaveInventoryItemAsync(newItem);

                // 更新UI
                UpdateInventorySlot(slotIndex, "cloth_picture_fragment");
                _hasPictureFragment = true;

                // 隐藏碎片
                ClothPictureFragment.IsVisible = false;

                // 更新获取最新的画碎片数量
                items = await App.InventoryRepo.GetInventoryItemsAsync();
                int fragmentCount = items.Count(i => i.ItemName.Contains("picture_fragment"));

                // 显示提示文本
                ShowHint($"画碎片 {fragmentCount}/5");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnPictureFragmentClicked: {ex.Message}");
        }
    }

    public async void OnDoorClicked(object sender, EventArgs e)
    {
        if (isNavigating)
            return;
        
        try
        {
            // 检查门是否已解锁
            bool isDoorUnlocked = await App.GameStateRepo.GetStateAsync("DoorUnlocked");
            
            if (isDoorUnlocked)
            {
                // 门已解锁，导航到结束页面
                isNavigating = true;
                await this.FadeTo(0, 500);
                await Shell.Current.GoToAsync("//AfterIntroduce");
            }
            else
            {
                // 门仍然锁着，显示提示
                ShowHint("门锁了");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Door click error: {ex.Message}");
            ShowHint("门锁了");
        }
    }
}