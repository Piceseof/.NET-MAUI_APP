namespace Games.Views;
using Games.Models;

public partial class CabinettLeftRiddle : ContentPage
{
	private bool isNavigating = false;
	private int crackClickCount = 0;
	private StackLayout _inventoryLayout;
	private BoxView _currentSelectedBackground;
	private Image _currentSelectedItem;
	private bool _hasPictureFragment = false;

	public CabinettLeftRiddle()
	{
		InitializeComponent();
		
		// 添加页面背景的点击事件
		var backgroundTapGesture = new TapGestureRecognizer();
		backgroundTapGesture.Tapped += OnBackgroundTapped;
		RoomOneWallImage.GestureRecognizers.Add(backgroundTapGesture);
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
    public async void OnCrackClicked(object sender, EventArgs e)
    {
        if (!isNavigating && this.Opacity > 0)
        {
            // 检查柜子是否已经被打开过
            bool isCabinetBroken = await App.GameStateRepo.GetStateAsync("IsCabinetBroken");
            if (isCabinetBroken)
            {
                return; // 如果已经打开过，直接返回
            }

            crackClickCount++;

            switch (crackClickCount)
            {
                case 1:
                    CabinetImage.Source = "crack1.jpeg";
                    break;
                case 2:
                    CabinetImage.Source = "crack2.jpeg";
                    break;
                case 3:
                    CabinetImage.Source = "crack3.jpeg";
                    break;
                case 4:
                    CabinetImage.Source = "crack4.jpeg";
                    break;
                case 5:
                    CabinetImage.Source = "crack5.jpeg";
                    break;
                case 6:
                    if (!_hasPictureFragment && _inventoryLayout != null)
                    {
                        // 将柜子图片改成最后的样子
                        CabinetImage.Source = "crack5.jpeg";

                        // 记录柜子已被打开的状态
                        await App.GameStateRepo.SaveStateAsync("IsCabinetBroken", true);

                        int slotIndex;
                        // 先检查是否有任何画碎片
                        var anyPictureFragment = (await App.InventoryRepo.GetInventoryItemsAsync())
                            .FirstOrDefault(i => i.ItemName.Contains("picture_fragment"));

                        if (anyPictureFragment != null)
                        {
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
                                ItemName = "cabinet_picture_fragment",
                                SlotIndex = slotIndex,
                                IsCollected = true
                            };
                            await App.InventoryRepo.SaveInventoryItemAsync(newItem);

                            // 更新UI和状态
                            UpdateInventorySlot(slotIndex, "sofa_picture_fragment"); // 使用相同的图片
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
                    break;
            }
        }
    }
    protected override async void OnAppearing()
	{
		base.OnAppearing();
		this.Opacity = 0;
		isNavigating = false;
		
		// 检查柜子是否已经被打开过
		bool isCabinetBroken = await App.GameStateRepo.GetStateAsync("IsCabinetBroken");
		if (isCabinetBroken)
		{
			crackClickCount = 6;
			CabinetImage.Source = "crack5.jpeg";
		}
		else
		{
			crackClickCount = 0;
			CabinetImage.Source = "cabinettleft.png";
		}

		_hasPictureFragment = false;

		if (_inventoryLayout == null)
		{
			_inventoryLayout = this.FindByName<StackLayout>("InventoryLayout");
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
		
		// 检查当前房间的画碎片是否已被收集
		var cabinetPictureFragment = await App.InventoryRepo.GetInventoryItemAsync("cabinet_picture_fragment");
		if (cabinetPictureFragment != null && cabinetPictureFragment.IsCollected)
		{
			_hasPictureFragment = true;
		}

		// 如果有任何画碎片，在物品栏显示
		var anyPictureFragment = items.FirstOrDefault(i => i.ItemName.Contains("picture_fragment"));
		if (anyPictureFragment != null)
		{
			UpdateInventorySlot(anyPictureFragment.SlotIndex, "sofa_picture_fragment");
		}

		// 加载其他非画碎片物品
		foreach (var item in items.Where(i => !i.ItemName.Contains("picture_fragment")))
		{
			if (item.IsCollected)
			{
				UpdateInventorySlot(item.SlotIndex, item.ItemName);
			}
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

			ShowHint(itemInfo);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnInventoryItemClicked: {ex.Message}");
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

	public async void OnBackClicked(object sender, EventArgs e)
	{
		if (!isNavigating && this.Opacity > 0)
		{
			isNavigating = true;
			await this.FadeTo(0, 500);
			await Shell.Current.GoToAsync("//Room1Wall1");
		}
	}

	public async void OnSettingClicked(object sender, EventArgs e)
	{
		if (!isNavigating && this.Opacity > 0)
		{
			isNavigating = true;
			await App.SettingRepo.UpdateBackPage(1, "CabinettLeftRiddle");
			await Shell.Current.GoToAsync("//SettingPage");
		}
	}
    private void clClicked(object sender, EventArgs e)
    {
		if(!_hasPictureFragment)
        // 更新提示文字
        ShowHint("看得我手痒痒");
    }

}