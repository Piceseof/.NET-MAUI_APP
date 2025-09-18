namespace Games.Views;
using Games.Models;
using Games.Services;
using System;
using Microsoft.Maui.Controls;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui;

public partial class ScalePage : ContentPage
{
    private StackLayout _inventoryLayout;
    private BoxView _currentSelectedBackground;
    private Image _currentSelectedItem;

    // 天平游戏相关
    private Dictionary<string, int> ballWeights = new Dictionary<string, int>
    {
        {"blackball2", 2},
        {"blackball3", 3},
        {"redball", 5},
        {"greenball", 1},
        {"blueball", 7},
        {"yellowball", 8}
    };

    private List<Image> leftPlateBalls = new List<Image>();
    private List<Image> rightPlateBalls = new List<Image>();

    public ScalePage()
    {
        InitializeComponent();
        SetupBalls();
        
        // 添加页面背景的点击事件
        var backgroundTapGesture = new TapGestureRecognizer();
        backgroundTapGesture.Tapped += OnBackgroundTapped;
        BackgroundImage.GestureRecognizers.Add(backgroundTapGesture);
    }

    private void OnBackgroundTapped(object sender, EventArgs e)
    {
        // 先检查是否有选中的物品，如果有则取消选中
        if (_currentSelectedBackground != null && _currentSelectedItem != null)
        {
            _currentSelectedBackground.IsVisible = false;
            _currentSelectedItem.Scale = 1.0;
            _currentSelectedBackground = null;
            _currentSelectedItem = null;
        }
        // 没有选中物品时，检查玩家是否拥有蓝色和黄色的球
        else
        {
            // 异步检查但同步执行
            Task.Run(async () => 
            {
                // 查询物品栏，检查是否有蓝色和黄色的球
                var blueYellowBallItem = await App.InventoryRepo.GetInventoryItemAsync("blue_and_yellow_ball");
                
                // 如果没有球或者球未被收集，则显示提示
                if (blueYellowBallItem == null || !blueYellowBallItem.IsCollected)
                {
                    // 在主线程上显示提示
                    MainThread.BeginInvokeOnMainThread(() => 
                    {
                        ShowHint("是不是少了俩个球呀");
                    });
                }
            }).Wait(); // 等待异步操作完成
        }
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
            
            // 检查是否有蓝黄球组合物品
            var hasBlueAndYellowBall = items.Any(i => i.ItemName == "blue_and_yellow_ball" && i.IsCollected);
            
            // 获取蓝球和黄球的Image控件
            var blueBall = BallsContainer.Children.FirstOrDefault(
                x => x is Image img && (img.Source as FileImageSource)?.File == "blueball.png") as Image;
            var yellowBall = BallsContainer.Children.FirstOrDefault(
                x => x is Image img && (img.Source as FileImageSource)?.File == "yellowball.png") as Image;

            // 根据是否有蓝黄球组合来控制显示
            if (blueBall != null) blueBall.IsVisible = hasBlueAndYellowBall;
            if (yellowBall != null) yellowBall.IsVisible = hasBlueAndYellowBall;

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
            _ => "未知物品"
        };
    }

    // 天平游戏相关方法
    private void SetupBalls()
    {
        foreach (var child in BallsContainer.Children)
        {
            if (child is Image ballImage)
            {
                var tapGesture = new TapGestureRecognizer();
                tapGesture.Tapped += OnBallTapped;
                ballImage.GestureRecognizers.Add(tapGesture);
            }
        }
    }

    private void OnBallTapped(object sender, EventArgs e)
    {
        if (sender is Image tappedBall)
        {
            DisplayActionSheet("选择放置位置", "取消", null, "左盘", "右盘")
                .ContinueWith(async (task) =>
                {
                    var result = await task;
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        if (result == "左盘")
                        {
                            MoveBallToPlate(tappedBall, LeftPlateItems, leftPlateBalls);
                        }
                        else if (result == "右盘")
                        {
                            MoveBallToPlate(tappedBall, RightPlateItems, rightPlateBalls);
                        }
                    });
                });
        }
    }

    private void MoveBallToPlate(Image ball, FlexLayout plate, List<Image> plateBalls)
    {
        var ballSource = ball.Source;
        var newBall = new Image
        {
            Source = ballSource,
            WidthRequest = 40,
            HeightRequest = 40
        };

        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += (s, e) => RemoveBallFromPlate(newBall, plate, plateBalls);
        newBall.GestureRecognizers.Add(tapGesture);

        plate.Children.Add(newBall);
        plateBalls.Add(newBall);
        ball.IsEnabled = false;
        ball.Opacity = 0;

        UpdateWeights();
    }

    private void RemoveBallFromPlate(Image ball, FlexLayout plate, List<Image> plateBalls)
    {
        plate.Children.Remove(ball);
        plateBalls.Remove(ball);

        var originalBall = BallsContainer.Children.FirstOrDefault(
            x => x is Image img && img.Source == ball.Source) as Image;
        if (originalBall != null)
        {
            originalBall.IsEnabled = true;
            originalBall.Opacity = 1;
        }

        UpdateWeights();
    }

    private void UpdateWeights()
    {
        int leftWeight = CalculateWeight(leftPlateBalls);
        int rightWeight = CalculateWeight(rightPlateBalls);

        LeftWeightLabel.Text = $"左盘重量: {leftWeight}";
        RightWeightLabel.Text = $"右盘重量: {rightWeight}";

        if (leftPlateBalls.Count > 0 && rightPlateBalls.Count > 0)
        {
            string hintMessage = leftWeight == rightWeight ? "天平平衡了！" : (leftWeight > rightWeight ? "左边更重" : "右边更重");
            Color hintColor = leftWeight == rightWeight ? Colors.Green : Colors.Red;

            // Using the new ShowHint method to display the message
            ShowHint(hintMessage);
        }
        else
        {
             // Optionally clear the hint if no balls are on the plates
        }
    }

    private int CalculateWeight(List<Image> balls)
    {
        int weight = 0;
        foreach (var ball in balls)
        {
            var imageSource = ball.Source as FileImageSource;
            if (imageSource != null)
            {
                var ballName = Path.GetFileNameWithoutExtension(imageSource.File);
                if (ballWeights.ContainsKey(ballName))
                {
                    weight += ballWeights[ballName];
                }
            }
        }
        return weight;
    }

    public async void OnBackClicked(object sender, EventArgs e)
    {
        await this.FadeTo(0, 500);
        await Shell.Current.GoToAsync("//Room1Wall2");
    }

    public async void OnSettingClicked(object sender, EventArgs e)
    {
        await App.SettingRepo.UpdateBackPage(1, "ScalePage");
        await Shell.Current.GoToAsync("//SettingPage");
    }

    // 处理点击眼睛图片的事件
    private void OnEyeImageTapped(object sender, EventArgs e)
    {
        // 显示半透明黑色覆盖层（屏幕变暗）
        DarkOverlay.IsVisible = true;
        DarkOverlay.ZIndex = 100;
        
        // 显示两个圆形图片并提高亮度
        circleone.IsVisible = true;
        circletwo.IsVisible = true;
        circlethree.IsVisible = true;
        circlefour.IsVisible = true;

        // 提高圆形图片的亮度（设置在暗覆盖层上方）
        circleone.ZIndex = 101;
        circletwo.ZIndex = 101;
        circlethree.ZIndex = 101;
        circlefour.ZIndex = 101;
    }
    
    // 处理点击覆盖层的事件（屏幕任意位置）
    private void OnOverlayTapped(object sender, EventArgs e)
    {
        // 隐藏半透明黑色覆盖层（屏幕恢复亮度）
        DarkOverlay.IsVisible = false;
        
        // 隐藏两个圆形图片
        circleone.IsVisible = false;
        circletwo.IsVisible = false;
        circlethree.IsVisible = false;
        circlefour.IsVisible = false;
    }
} 