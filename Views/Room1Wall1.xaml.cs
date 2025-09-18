namespace Games.Views;
using Games.Models;

public partial class Room1Wall1 : ContentPage
{
	private StackLayout _inventoryLayout;
	private BoxView _currentSelectedBackground;
	private Image _currentSelectedItem;

	public Room1Wall1()
	{
		InitializeComponent();
		
		// 添加页面背景的点击事件
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
		
		// 设置初始透明度为0，淡入显示
		this.Opacity = 0;
		
		try
		{
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
			
			// 淡入效果
			await this.FadeTo(1, 500);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error in OnAppearing: {ex.Message}");
			// 确保页面可见
			this.Opacity = 1;
		}
	}

	//更新物品槽位方法
	private void UpdateInventorySlot(int slotIndex, string itemName)
	{
		if (slotIndex < 0 || slotIndex >= _inventoryLayout.Children.Count)
			return;
		//创建物品格子
		var grid = new Grid
		{
			WidthRequest = 50,
			HeightRequest = 50
		};
		//创建背景图片
		var originalInventory = new Image
		{
			Source = "inventory.png",
			Aspect = Aspect.AspectFill,
			WidthRequest = 50,
			HeightRequest = 50
		};
		//创建物品图片
		var inventoryItem = new Image
		{
			Source = itemName,
			WidthRequest = 40,
			HeightRequest = 40,
			Aspect = Aspect.AspectFill,
			Margin = new Thickness(5)
		};
        // 创建选中效果背景
        var whiteBackground = new BoxView
		{
			Color = Colors.White,
			IsVisible = false,
			WidthRequest = 50,
			HeightRequest = 50
		};
        // 组装UI
        grid.Children.Add(originalInventory);
		grid.Children.Add(whiteBackground);
		grid.Children.Add(inventoryItem);
        // 添加点击事件
        var tapGestureRecognizer = new TapGestureRecognizer();
		tapGestureRecognizer.Tapped += async (s, e) => 
		{
			await OnInventoryItemClicked(grid, itemName, whiteBackground, inventoryItem);
		};
		grid.GestureRecognizers.Add(tapGestureRecognizer);
        // 更新物品栏
        _inventoryLayout.Children[slotIndex] = grid;
	}
    //显示物品信息
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
		FragmentHintLabel.Background = new SolidColorBrush(Color.FromRgba(0, 0, 0, 0.20));
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

	public async void OnLeftButtonClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//Room1Wall4");
	}

	public async void OnRightButtonClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//Room1Wall2");
	}

	public async void OnSettingClicked(object sender, EventArgs e)
	{
		await App.SettingRepo.UpdateBackPage(1, "Room1Wall1");
		await Shell.Current.GoToAsync("//SettingPage");
	}

	public async void OnPaperImageClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//PaperDisplayPage");
	}

	public async void OnPictureCornerClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//PictureDisplayPage");
	}

	public async void OnBoxClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//BoxRiddle");
	}
	public async void OnFrameClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//PuzzlePiecesSlove");
	}

	public async void OnCabinettLeftClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//CabinettLeftRiddle");
	}

	public async void OnCabinettRightClicked(object sender, EventArgs e)
	{
		await this.FadeTo(0, 500);
		await Shell.Current.GoToAsync("//CabinettRightRiddle");
	}

    private async void OnBloodOneClicked(object sender, EventArgs e)
    {
        var clothItem = await App.InventoryRepo.GetInventoryItemAsync("dishcloth");
        if (clothItem == null || !clothItem.IsCollected)
        {
            // 如果没有衣服的一角，显示提示
            ShowHint("这有污渍，需要用什么擦一下");
            return;
        }
        // 检查是否选中了衣服的一角且血迹未被擦过
        if (_currentSelectedItem?.Source?.ToString()?.Contains("dishcloth") == true)
        {
            bool isBloodOneCleaned = await App.GameStateRepo.GetStateAsync("IsBloodOneCleaned");
            if (!isBloodOneCleaned)
            {
                BloodOne.IsVisible = false;
                NineSymbol.IsVisible = true;
                await App.GameStateRepo.SaveStateAsync("IsBloodOneCleaned", true);

                // 取消衣服的一角的选中状态
                if (_currentSelectedBackground != null && _currentSelectedItem != null)
                {
                    _currentSelectedBackground.IsVisible = false;
                    _currentSelectedItem.Scale = 1.0;
                    _currentSelectedBackground = null;
                    _currentSelectedItem = null;
                }
            }
        }
    }

    private async void OnBloodThreeClicked(object sender, EventArgs e)
    {
        var clothItem = await App.InventoryRepo.GetInventoryItemAsync("dishcloth");
        if (clothItem == null || !clothItem.IsCollected)
        {
            // 如果没有衣服的一角，显示提示
            ShowHint("这里有污渍，需要用什么擦一下");
            return;
        }
        if (_currentSelectedItem?.Source?.ToString()?.Contains("dishcloth") == true)
        {
            bool isBloodThreeCleaned = await App.GameStateRepo.GetStateAsync("IsBloodThreeCleaned");
            if (!isBloodThreeCleaned)
            {
                BloodThree.IsVisible = false;
                ThreeSymbol.IsVisible = true;
                await App.GameStateRepo.SaveStateAsync("IsBloodThreeCleaned", true);

                if (_currentSelectedBackground != null && _currentSelectedItem != null)
                {
                    _currentSelectedBackground.IsVisible = false;
                    _currentSelectedItem.Scale = 1.0;
                    _currentSelectedBackground = null;
                    _currentSelectedItem = null;
                }
            }
        }
    }

    private async void OnBloodFourClicked(object sender, EventArgs e)
    {
        var clothItem = await App.InventoryRepo.GetInventoryItemAsync("dishcloth");
        if (clothItem == null || !clothItem.IsCollected)
        {
            // 如果没有衣服的一角，显示提示
            ShowHint("这里有污渍，需要用什么擦一下");
            return;
        }
        if (_currentSelectedItem?.Source?.ToString()?.Contains("dishcloth") == true)
        {
            bool isBloodFourCleaned = await App.GameStateRepo.GetStateAsync("IsBloodFourCleaned");
            if (!isBloodFourCleaned)
            {
                BloodFour.IsVisible = false;
                OneSymbol.IsVisible = true;
                await App.GameStateRepo.SaveStateAsync("IsBloodFourCleaned", true);

                if (_currentSelectedBackground != null && _currentSelectedItem != null)
                {
                    _currentSelectedBackground.IsVisible = false;
                    _currentSelectedItem.Scale = 1.0;
                    _currentSelectedBackground = null;
                    _currentSelectedItem = null;
                }
            }
        }
    }

    private async void OnBloodHiddenClicked(object sender, EventArgs e)
    {
        var clothItem = await App.InventoryRepo.GetInventoryItemAsync("dishcloth");
        if (clothItem == null || !clothItem.IsCollected)
        {
            // 如果没有衣服的一角，显示提示
            ShowHint("这里有污渍，需要用什么擦一下");
            return;
        }

        if (_currentSelectedItem?.Source?.ToString()?.Contains("dishcloth") == true)
        {
            bool isBloodHiddenCleaned = await App.GameStateRepo.GetStateAsync("IsBloodHiddenCleaned");
            if (!isBloodHiddenCleaned)
            {
                BloodHidden.IsVisible = false;
                FourSymbol.IsVisible = true;
                await App.GameStateRepo.SaveStateAsync("IsBloodHiddenCleaned", true);

                if (_currentSelectedBackground != null && _currentSelectedItem != null)
                {
                    _currentSelectedBackground.IsVisible = false;
                    _currentSelectedItem.Scale = 1.0;
                    _currentSelectedBackground = null;
                    _currentSelectedItem = null;
                }
            }
        }
    }
	private void FrameTapped(object sender, EventArgs e)
	{
		// 更新提示文字
		ShowHint("画的左下角是不是有什么？");
	}

	private void cabinetClicked(object sender, EventArgs e)
	{
		// 更新提示文字
		ShowHint("柜子里好像有什么东西");
	}
}