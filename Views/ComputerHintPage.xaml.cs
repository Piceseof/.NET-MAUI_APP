namespace Games.Views;
using Games.Models;

public partial class ComputerHintPage : ContentPage
{
    private StackLayout _inventoryLayout;
    private BoxView _currentSelectedBackground;
    private Image _currentSelectedItem;

    public ComputerHintPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.Opacity = 0;

        try
        {
            // 清除可能存在的二进制标签
            var existingLabel = this.Content.FindByName<Label>("BinaryLabel");
            if (existingLabel != null)
            {
                (this.Content as Grid)?.Children.Remove(existingLabel);
            }

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

    private async void OnBackgroundTapped(object sender, EventArgs e)
    {
        try
        {
            // 获取台灯状态
            var lightState = await App.GameStateRepo.GetStateAsync("light_state");
            bool isLightOn = true;  // 默认灯是开着的
            if (lightState != null)
            {
                isLightOn = Convert.ToBoolean(lightState);
            }

            // 检查是否点击的是白色区域、选中了手电筒，且台灯处于关闭状态
            if (sender is Frame && 
                _currentSelectedItem?.Source.ToString().Contains("torch") == true && 
                !isLightOn)  // 只有在灯关闭的时候才显示二进制码
            {
                // 直接在屏幕中央显示二进制代码
                var binaryLabel = new Label
                {
                    Text = "l    o    m    e",
                    TextColor = Colors.Black,
                    FontSize = 20,
                    FontAttributes = FontAttributes.Bold,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    IsVisible = true
                };

                // 移除之前的标签（如果存在）
                var existingLabel = this.Content.FindByName<Label>("BinaryLabel");
                if (existingLabel != null)
                {
                    (this.Content as Grid)?.Children.Remove(existingLabel);
                }

                // 添加新标签并设置名称
                binaryLabel.StyleId = "BinaryLabel";  // 使用 StyleId 来代替 x:Name
                (this.Content as Grid)?.Children.Add(binaryLabel);
                Grid.SetColumn(binaryLabel, 0);

                // 取消手电筒的选中效果
                if (_currentSelectedBackground != null && _currentSelectedItem != null)
                {
                    _currentSelectedBackground.IsVisible = false;
                    _currentSelectedItem.Scale = 1.0;
                    _currentSelectedBackground = null;
                    _currentSelectedItem = null;
                }
            }
            else
            {
                // 点击背景时取消物品选中状态
                if (_currentSelectedBackground != null && _currentSelectedItem != null)
                {
                    _currentSelectedBackground.IsVisible = false;
                    _currentSelectedItem.Scale = 1.0;
                    _currentSelectedBackground = null;
                    _currentSelectedItem = null;
                }

                // 移除二进制代码标签（如果存在）
                var binaryLabel = this.Content.FindByName<Label>("BinaryLabel");
                if (binaryLabel != null)
                {
                    (this.Content as Grid)?.Children.Remove(binaryLabel);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnBackgroundTapped: {ex.Message}");
        }
    }

    public async void OnBackClicked(object sender, EventArgs e)
    {
        await this.FadeTo(0, 500);
        await Shell.Current.GoToAsync("//Room1Wall2");
    }

    public async void OnSettingClicked(object sender, EventArgs e)
    {
        await App.SettingRepo.UpdateBackPage(1, "ComputerHintPage");
        await Shell.Current.GoToAsync("//SettingPage");
    }
} 