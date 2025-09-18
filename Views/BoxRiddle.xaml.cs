using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Maui.Controls.PlatformConfiguration.WindowsSpecific;
using Microsoft.Maui.Graphics;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Games.Models;

namespace Games.Views;

public partial class BoxRiddle : ContentPage
{
   
    private Image currentPiece;
    private Point startPosition;
    private const double PIECE_SIZE = 60;
    private const double SLOT_MARGIN_LEFT = 23;
    private const double SLOT_MARGIN_RIGHT = 243;
    private const double SLOT_TOP = 14;
    private const double SLOT_UPPER = 74;
    private const double SLOT_LOWER = 134;
    private const double SLOT_BOTTOM = 189;
    private const double SNAP_DISTANCE = 30;           // 增加吸附距离，使更容易对齐
    private const double SLOT_CENTER = 133;  // 中间位置的X坐标
    private const double HORIZONTAL_SNAP_DISTANCE = 40; // 增加横向吸附距离

    // 记录每个位置的方块
    private Dictionary<Point, Image> positionOccupancy = new Dictionary<Point, Image>();
    
    // 所有可能的位置
    private readonly List<Point> validPositions = new List<Point>
    {
        // 左侧位置
        new Point(SLOT_MARGIN_LEFT, SLOT_TOP),    // 左上
        new Point(SLOT_MARGIN_LEFT, SLOT_UPPER),  // 左上中
        new Point(SLOT_MARGIN_LEFT, SLOT_LOWER),  // 左下中
        new Point(SLOT_MARGIN_LEFT, SLOT_BOTTOM), // 左下

        // 中间位置
        new Point(SLOT_CENTER, SLOT_UPPER),       // 中上
        new Point(SLOT_CENTER, SLOT_LOWER),       // 中

        // 右侧位置
        new Point(SLOT_MARGIN_RIGHT, SLOT_TOP),    // 右上
        new Point(SLOT_MARGIN_RIGHT, SLOT_UPPER),  // 右上中
        new Point(SLOT_MARGIN_RIGHT, SLOT_LOWER),  // 右下中
        new Point(SLOT_MARGIN_RIGHT, SLOT_BOTTOM)  // 右下
    };

    // 修改正确位置的定义
    private readonly Dictionary<string, Point> correctPositions = new Dictionary<string, Point>
    {
        {"p3.png", new Point(SLOT_MARGIN_LEFT, SLOT_TOP)},     // 左上（竖线符号）
        {"p4.png", new Point(SLOT_MARGIN_LEFT, SLOT_UPPER)},   // 左中上（圆点符号）
        {"p5.png", new Point(SLOT_MARGIN_LEFT, SLOT_BOTTOM)},  // 左下（方框符号）
        {"p1.png", new Point(SLOT_MARGIN_RIGHT, SLOT_TOP)},    // 右上（弧线符号）
        {"p2.png", new Point(SLOT_MARGIN_RIGHT, SLOT_UPPER)},  // 右中上（三角形符号）
        {"p6.png", new Point(SLOT_MARGIN_RIGHT, SLOT_BOTTOM)}, // 右下符号）
    };

    private StackLayout _inventoryLayout;
    private BoxView _currentSelectedBackground;
    private Image _currentSelectedItem;
    private bool _hasKnife = false;

    public BoxRiddle()
    {
        InitializeComponent();
        
        // 添加页面背景的点击事件
        var backgroundTapGesture = new TapGestureRecognizer();
        backgroundTapGesture.Tapped += OnBackgroundTapped;
        
        // 将事件绑定到背景图片上
        var backgroundImage = this.FindByName<Image>("riddlebackground");
        if (backgroundImage != null)
        {
            backgroundImage.GestureRecognizers.Add(backgroundTapGesture);
        }
    }

    // 添加背景点击事件处理方法
    private async void OnBackgroundTapped(object sender, EventArgs e)
    {
        // 点击背景时取消物品栏的选中状态
        if (_currentSelectedBackground != null && _currentSelectedItem != null)
        {
            _currentSelectedBackground.IsVisible = false;
            _currentSelectedItem.Scale = 1.0;
            _currentSelectedBackground = null;
            _currentSelectedItem = null;
        }
        
        // 检查是否已经获得了小刀
        bool hasKnife = await App.GameStateRepo.GetStateAsync("HasKnife");
        
        // 如果尚未获得小刀，显示提示文字
        if (!hasKnife && !FragmentHintLabel.IsVisible)
        {
            ShowHint("图片的下面有图案，把拼图拼到对应的图片上面哦~");
        }
    }

    protected override async void OnAppearing()
    {
        try
        {
            base.OnAppearing();

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

            // 检查是否已经获得过刀
            bool hasKnife = await App.GameStateRepo.GetStateAsync("HasKnife");
            _hasKnife = hasKnife;

            // 从数据库加载物品
            var items = await App.InventoryRepo.GetInventoryItemsAsync();
            foreach (var item in items)
            {
                if (item.IsCollected)
                {
                    UpdateInventorySlot(item.SlotIndex, item.ItemName);
                }
            }

            InitializePuzzle();
            await this.FadeTo(1, 500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"OnAppearing error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }
    }

    private void InitializePuzzle()
    {
        positionOccupancy.Clear();
        foreach (var child in PuzzleGrid.Children)
        {
            if (child is Image image)
            {
                var position = new Point(image.Margin.Left, image.Margin.Top);
                positionOccupancy[position] = image;
            }
        }
    }

    private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        var piece = (Image)sender;

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                currentPiece = piece;
                startPosition = new Point(piece.Margin.Left, piece.Margin.Top);
                piece.ZIndex = 100;
                break;

            case GestureStatus.Running:
                if (currentPiece != null)   

                {
                    double currentX = currentPiece.Margin.Left;
                    double currentY = currentPiece.Margin.Top;
                    double newX = currentX;
                    double newY = currentY;

                    // 判断是否在第三个凹槽位或附近
                    bool isInLowerRow = Math.Abs(currentY - SLOT_LOWER) < SNAP_DISTANCE * 1.5;
                    bool isInUpperRow = Math.Abs(currentY - SLOT_UPPER) < SNAP_DISTANCE * 1.5;

                    // 在第三个凹槽位置或中间位置时允许自由移动
                    if (isInLowerRow || (isInUpperRow && Math.Abs(currentX - SLOT_CENTER) < HORIZONTAL_SNAP_DISTANCE))
                    {
                        // 处理横向移动
                        newX = currentX + e.TotalX * 1.2;
                        newX = Math.Clamp(newX, SLOT_MARGIN_LEFT, SLOT_MARGIN_RIGHT);
                        
                        // 横向吸附
                        if (Math.Abs(newX - SLOT_MARGIN_LEFT) < HORIZONTAL_SNAP_DISTANCE)
                            newX = SLOT_MARGIN_LEFT;
                        else if (Math.Abs(newX - SLOT_CENTER) < HORIZONTAL_SNAP_DISTANCE)
                            newX = SLOT_CENTER;
                        else if (Math.Abs(newX - SLOT_MARGIN_RIGHT) < HORIZONTAL_SNAP_DISTANCE)
                            newX = SLOT_MARGIN_RIGHT;

                        // 处理竖直移动
                        newY = currentY + e.TotalY * 1.2;
                        
                        // 根据当前X位置限制Y的移动范围
                        if (Math.Abs(newX - SLOT_CENTER) < HORIZONTAL_SNAP_DISTANCE)
                            newY = Math.Clamp(newY, SLOT_UPPER, SLOT_LOWER);
                        else
                            newY = Math.Clamp(newY, SLOT_TOP, SLOT_BOTTOM);
                        
                        // 竖直吸附
                        if (Math.Abs(newY - SLOT_TOP) < SNAP_DISTANCE && 
                            Math.Abs(newX - SLOT_CENTER) >= HORIZONTAL_SNAP_DISTANCE)
                            newY = SLOT_TOP;
                        else if (Math.Abs(newY - SLOT_UPPER) < SNAP_DISTANCE)
                            newY = SLOT_UPPER;
                        else if (Math.Abs(newY - SLOT_LOWER) < SNAP_DISTANCE)
                            newY = SLOT_LOWER;
                        else if (Math.Abs(newY - SLOT_BOTTOM) < SNAP_DISTANCE && 
                                 Math.Abs(newX - SLOT_CENTER) >= HORIZONTAL_SNAP_DISTANCE)
                            newY = SLOT_BOTTOM;
                    }
                    // 在左右两列的其他位置时
                    else if (Math.Abs(currentX - SLOT_MARGIN_LEFT) < HORIZONTAL_SNAP_DISTANCE || 
                             Math.Abs(currentX - SLOT_MARGIN_RIGHT) < HORIZONTAL_SNAP_DISTANCE)
                    {
                        // 只允许竖直移动
                        newY = currentY + e.TotalY * 1.2;
                        newY = Math.Clamp(newY, SLOT_TOP, SLOT_BOTTOM);
                        
                        // 竖直吸附
                        if (Math.Abs(newY - SLOT_TOP) < SNAP_DISTANCE)
                            newY = SLOT_TOP;
                        else if (Math.Abs(newY - SLOT_UPPER) < SNAP_DISTANCE)
                            newY = SLOT_UPPER;
                        else if (Math.Abs(newY - SLOT_LOWER) < SNAP_DISTANCE)
                            newY = SLOT_LOWER;
                        else if (Math.Abs(newY - SLOT_BOTTOM) < SNAP_DISTANCE)
                            newY = SLOT_BOTTOM;

                        // 保持在最近的列
                        if (Math.Abs(currentX - SLOT_MARGIN_LEFT) < HORIZONTAL_SNAP_DISTANCE)
                            newX = SLOT_MARGIN_LEFT;
                        else
                            newX = SLOT_MARGIN_RIGHT;
                    }

                    // 检查新位置是否被占用
                    var targetPosition = new Point(newX, newY);
                    if (!positionOccupancy.ContainsKey(targetPosition) || 
                        positionOccupancy[targetPosition] == currentPiece)
                    {
                        currentPiece.Margin = new Thickness(newX, newY, 0, 0);
                    }
                }
                break;

            case GestureStatus.Completed:
                if (currentPiece != null)
                {
                    var oldPosition = startPosition;
                    var currentPosition = new Point(currentPiece.Margin.Left, currentPiece.Margin.Top);

                    // 如果当前位置是有效位置且未被占用
                    if (validPositions.Contains(currentPosition) && 
                        (!positionOccupancy.ContainsKey(currentPosition) || currentPosition == oldPosition))
                    {
                        positionOccupancy.Remove(oldPosition);
                        positionOccupancy[currentPosition] = currentPiece;
                    }
                    else
                    {
                        // 返回原位
                        currentPiece.Margin = new Thickness(oldPosition.X, oldPosition.Y, 0, 0);
                    }

                    currentPiece.ZIndex = 1;
                    currentPiece = null;
                    CheckPuzzleComplete();
                }
                break;
        }
    }

    private Point FindNearestValidPosition(Point currentPosition)
    {
        // 找到最近的有效位置
        return validPositions
            .OrderBy(p => Math.Pow(p.X - currentPosition.X, 2) + Math.Pow(p.Y - currentPosition.Y, 2))
            .First();
    }

    private async void CheckPuzzleComplete()
    {
        // 检查是否所有拼图都放在了正确位置
        bool allCorrect = true;
        foreach (var kvp in correctPositions)
        {
            string pieceName = kvp.Key;
            Point correctPosition = kvp.Value;

            // 查找对应的拼图片段
            Image piece = null;
            foreach (var child in PuzzleGrid.Children)
            {
                if (child is Image image && image.Source.ToString().EndsWith(pieceName))
                {
                    piece = image;
                    break;
                }
            }

            if (piece == null || !IsCloseToPosition(piece, correctPosition))
            {
                allCorrect = false;
                break;
            }
        }

        if (allCorrect && !_hasKnife)
        {
            // 如果所有拼图都正确，并且还没有获得小刀
            _hasKnife = true;
            await App.GameStateRepo.SaveStateAsync("HasKnife", true);

            // 在物品栏中添加小刀
            int emptySlot = -1;
            for (int i = 0; i < _inventoryLayout.Children.Count; i++)
            {
                if (_inventoryLayout.Children[i] is Image img && 
                    img.Source.ToString().EndsWith("inventory.png"))
                {
                    emptySlot = i;
                    break;
                }
            }

            if (emptySlot != -1)
            {
                // 保存到数据库
                var newItem = new InventoryItem
                {
                    ItemName = "knife",
                    SlotIndex = emptySlot,
                    IsCollected = true
                };
                await App.InventoryRepo.SaveInventoryItemAsync(newItem);

                // 更新UI
                UpdateInventorySlot(emptySlot, "knife");

                // 显示提示
                ShowHint("获得了一把小刀");
            }
        }
    }

    private bool IsCloseToPosition(Image piece, Point position)
    {
        double pieceLeft = piece.Margin.Left;
        double pieceTop = piece.Margin.Top;
        
        return Math.Abs(pieceLeft - position.X) < SNAP_DISTANCE && 
               Math.Abs(pieceTop - position.Y) < SNAP_DISTANCE;
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

        string imageSource = itemName;
        if (itemName.Contains("picture_fragment"))
        {
            imageSource = "sofa_picture_fragment";
        }

        var inventoryItem = new Image
        {
            Source = imageSource + ".png",
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
            _ => "未知物品"
        };
    }

    public async void OnBackClicked(object sender, EventArgs e)
    {
        await this.FadeTo(0, 500);
        await Shell.Current.GoToAsync("//Room1Wall1");
    }

    public async void OnSettingClicked(object sender, EventArgs e)
    {
        await App.SettingRepo.UpdateBackPage(1, "BoxRiddle");
        await Shell.Current.GoToAsync("//SettingPage");
    }

    
}