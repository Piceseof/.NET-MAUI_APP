namespace Games.Views;
using Games.Models;

public partial class LightSwitchPage : ContentPage
{
    private StackLayout _inventoryLayout;
    private BoxView _currentSelectedBackground;
    private Image _currentSelectedItem;
    private bool _isLightOn = false;
    private Image _lightImage;
    private Label _fragmentHintLabel;

    public LightSwitchPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.Opacity = 0;

        try
        {
            if (_inventoryLayout == null)
            {
                _inventoryLayout = this.FindByName<StackLayout>("InventoryLayout");
            }

            if (_inventoryLayout == null)
            {
                Console.WriteLine("Error: InventoryLayout not found");
                return;
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

            // 加载其他非画碎片物品
            foreach (var item in items.Where(i => !i.ItemName.Contains("picture_fragment")))
            {
                if (item.IsCollected)
                {
                    UpdateInventorySlot(item.SlotIndex, item.ItemName);
                }
            }

            // 初始化灯的图片控件
            _lightImage = this.FindByName<Image>("LightImage");
            
            // 从数据库加载灯的状态，如果没有状态则默认为开启
            var lightState = await App.GameStateRepo.GetStateAsync("light_state");
            if (lightState == null)
            {
                // 如果数据库中没有状态，设置为开启状态
                _isLightOn = true;
                await App.GameStateRepo.SaveStateAsync("light_state", true);
            }
            else
            {
                _isLightOn = Convert.ToBoolean(lightState);
            }

            if (_lightImage != null)
            {
                _lightImage.Source = _isLightOn ? "light_on.png" : "light_off.png";
            }

            await this.FadeTo(1, 500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnAppearing: {ex.Message}");
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
            if (itemName == "battery")
            {
                itemInfo = "一节电池";
                
                // 如果点击的是电池，处理电池相关逻辑
                if (!_isLightOn)
                {
                    await UseBattery();
                }
            }
            else if (itemName.Contains("picture_fragment"))
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
        ShowHint("灯光？？书本？？？");
    }

    public async void OnBackClicked(object sender, EventArgs e)
    {
        await this.FadeTo(0, 500);
        await Shell.Current.GoToAsync("//Room1Wall2");
    }

    public async void OnSettingClicked(object sender, EventArgs e)
    {
        await App.SettingRepo.UpdateBackPage(1, "LightSwitchPage");
        await Shell.Current.GoToAsync("//SettingPage");
    }

    public async void OnLightImageClicked(object sender, EventArgs e)
    {
        try
        {
            if (_currentSelectedBackground == null) // 没有选中任何物品
            {
                // 不论灯的状态如何，都显示需要电池的提示
                if(!_isLightOn)
                ShowHint("需要电池");
                return;
            }

            // 检查是否选中了电池
            if (_currentSelectedItem?.Source.ToString().Contains("battery") == true)
            {
                // 先取消选中效果
                if (_currentSelectedBackground != null && _currentSelectedItem != null)
                {
                    _currentSelectedBackground.IsVisible = false;
                    _currentSelectedItem.Scale = 1.0;
                    _currentSelectedBackground = null;
                    _currentSelectedItem = null;
                }

                // 使用电池
                await UseBattery();
                // 根据当前状态切换灯
                await ToggleLight();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnLightImageClicked: {ex.Message}");
        }
    }

    private async Task ToggleLight()
    {
        _isLightOn = !_isLightOn;
        if (_lightImage != null)
        {
            _lightImage.Source = _isLightOn ? "light_on.png" : "light_off.png";
        }
        
        // 保存灯的状态到数据库
        bool stateToSave = _isLightOn;  // 先保存到布尔变量
        await App.GameStateRepo.SaveStateAsync("light_state", stateToSave);  // 直接传递布尔值

        // 显示开关灯的提示文字
        ShowHint(_isLightOn ? "开灯" : "关灯");
        if(_isLightOn)
            ShowHint(_isLightOn ? "开灯" : "关灯");
    }

    private async Task UseBattery()
    {
        try
        {
            // 从数据库中删除电池
            var items = await App.InventoryRepo.GetInventoryItemsAsync();
            var battery = items.FirstOrDefault(i => i.ItemName == "battery");
            if (battery != null)
            {
                await App.InventoryRepo.DeleteInventoryItemAsync(battery);
            }

            // 清空选中状态
            if (_currentSelectedBackground != null && _currentSelectedItem != null)
            {
                _currentSelectedBackground.IsVisible = false;
                _currentSelectedItem.Scale = 1.0;
                _currentSelectedBackground = null;
                _currentSelectedItem = null;
            }

            // 刷新物品栏显示
            OnAppearing();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UseBattery: {ex.Message}");
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