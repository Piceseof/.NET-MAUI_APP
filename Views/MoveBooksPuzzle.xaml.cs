namespace Games.Views;
using Games.Models;

public partial class MoveBooksPuzzle : ContentPage
{
    private bool isNavigating = false;  // 添加导航状态标志
    private Image draggedBook;
    private double startX, startY;
    private readonly Dictionary<Image, (double X, double Y)> originalPositions;
    private readonly Dictionary<Image, Image> bookStack;
    private readonly double[] axisPositions = new double[] { 210.0, 360.0, 499.0 }; // 更新为新的轴线位置

    // 添加物品栏相关字段
    private StackLayout _inventoryLayout;
    private BoxView _currentSelectedBackground;
    private Image _currentSelectedItem;
    private bool _hasPictureFragment = false;

    public MoveBooksPuzzle()
    {
        InitializeComponent();
        originalPositions = new Dictionary<Image, (double X, double Y)>();
        bookStack = new Dictionary<Image, Image>();
        this.Opacity = 0;  // 确保初始透明度为0

        // 初始化书本位置
        InitializeBooks();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        this.Opacity = 0;
        isNavigating = false;

        try
        {
            // 重置所有书本到初始位置
            Book1.Margin = new Thickness(130, 0, 68, 92);
            Book2.Margin = new Thickness(150, 0, 138, 135);
            Book3.Margin = new Thickness(449, 0, 138, 92);
            Book4.Margin = new Thickness(310, 0, 13, 92);
            Book5.Margin = new Thickness(315, 0, 138, 117);

            // 重置所有书本的效果
            foreach (var book in new[] { Book1, Book2, Book3, Book4, Book5 })
            {
                book.Scale = 1.0;
                book.Opacity = 1.0;
                book.ZIndex = 0;
                book.TranslationX = 0;
            }

            // 清空堆叠关系
            bookStack.Clear();
            draggedBook = null;

            // 重置状态
            _hasPictureFragment = false;

            // 初始化物品栏
            if (_inventoryLayout == null)
            {
                _inventoryLayout = this.FindByName<StackLayout>("InventoryLayout");
            }

            if (_inventoryLayout == null)
            {
                Console.WriteLine("Error: InventoryLayout not found");
                return;  // 如果找不到物品栏，直接返回而不设置页面可见
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
            var booksPictureFragment = await App.InventoryRepo.GetInventoryItemAsync("books_picture_fragment");
            if (booksPictureFragment != null && booksPictureFragment.IsCollected)  // 修改这里的条件判断
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

            // 最后再设置页面可见
            await this.FadeTo(1, 500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnAppearing: {ex.Message}");
            // 确保页面在出错时也是可见的
            this.Opacity = 1;
        }
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        // 重置所有状态
        draggedBook = null;
        bookStack.Clear();
        foreach (var book in new[] { Book1, Book2, Book3, Book4, Book5 })
        {
            if (originalPositions.ContainsKey(book))
            {
                var (originalX, originalY) = originalPositions[book];
                book.Margin = new Thickness(originalX, 0, book.Margin.Right, originalY);
            }
            book.ZIndex = 0;
        }
    }

    private void InitializeBooks()
    {
        // 设置初始位置
        originalPositions[Book1] = (130, 92);
        originalPositions[Book2] = (150, 135);
        originalPositions[Book3] = (449, 92);
        originalPositions[Book4] = (310, 92);
        originalPositions[Book5] = (315, 117);

        // 为每本书添加拖拽手势
        foreach (var book in new[] { Book1, Book2, Book3, Book4, Book5 })
        {
            var panGesture = new PanGestureRecognizer();
            panGesture.TouchPoints = 1;  // 设置单点触控
            panGesture.PanUpdated += OnBookPanUpdated;
            book.GestureRecognizers.Add(panGesture);
        }
    }

    private bool isDragging = false;
    private double startDragX, startDragY;

    private void OnBookPanUpdated(object sender, PanUpdatedEventArgs e)
    {
        if (sender is Image book)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    // 在拖动开始时检查书籍是否可以移动
                    if (CanMoveBook(book))
                    {
                        isDragging = true;
                        draggedBook = book;
                        startDragX = book.Margin.Left;
                        startDragY = book.Margin.Bottom;
                        book.ZIndex = 100;
                        
                        // 显示可移动的视觉反馈
                        book.Scale = 1.1;
                    }
                    else
                    {
                        // 显示不可移动的提示
                        ShowHint("这本书不能移动，因为上面有其他书籍");
                    }
                    break;

                case GestureStatus.Running:
                    if (isDragging && draggedBook != null && draggedBook == book)
                    {
                        // 计算新位置
                        double newX = startDragX + e.TotalX;
                        double newY = startDragY - e.TotalY;

                        // 更新书本位置
                        draggedBook.Margin = new Thickness(
                            newX,
                            0,
                            draggedBook.Margin.Right,
                            newY
                        );
                    }
                    break;

                case GestureStatus.Completed:
                    if (isDragging && draggedBook != null && draggedBook == book)
                    {
                        try
                        {
                            // 恢复书籍的缩放
                            book.Scale = 1.0;
                            
                            // 找到最近的轴线
                            double bookCenterX = draggedBook.Margin.Left + draggedBook.Width / 2;
                            double nearestAxis = axisPositions
                                .OrderBy(x => Math.Abs(x - bookCenterX))
                                .First();

                            // 找到目标书籍（如果有）
                            var targetBook = FindTargetBook(draggedBook);

                            if (targetBook != null)
                            {
                                // 将书放在目标书上
                                PlaceBookOnTarget(draggedBook, targetBook);
                            }
                            else
                            {
                                // 将书放在轴线上
                                PlaceBookOnAxis(draggedBook, nearestAxis);
                            }

                            isDragging = false;
                            draggedBook = null;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error in OnBookPanUpdated: {ex.Message}");
                            // 出错时恢复书籍位置
                            ReturnBookToOriginalPosition(book);
                            isDragging = false;
                            draggedBook = null;
                        }
                    }
                    break;

                case GestureStatus.Canceled:
                    if (isDragging && draggedBook != null && draggedBook == book)
                    {
                        // 恢复书籍的缩放
                        book.Scale = 1.0;
                        
                        // 取消拖动时恢复书籍位置
                        ReturnBookToOriginalPosition(book);
                        isDragging = false;
                        draggedBook = null;
                    }
                    break;
            }
        }
    }

    private Image FindTopBookOnAxis(double axis)
    {
        var booksOnAxis = new[] { Book1, Book2, Book3, Book4, Book5 }
            .Where(b => Math.Abs((b.Margin.Left + b.Width / 2) - axis) < 30)
            .Where(b => !bookStack.ContainsKey(b)) // 只考虑最顶层的书
            .OrderByDescending(b => b.Margin.Bottom + b.Height) // 考虑书本高度
            .FirstOrDefault();
        return booksOnAxis;
    }

    private void PlaceBookOnAxis(Image book, double axis)
    {
        // 计算使书本中心对齐到轴线的左边距
        double newLeft = axis - book.Width / 2;

        // 找到该轴上所有的书，按底部位置从低到高排序
        var booksOnAxis = new[] { Book1, Book2, Book3, Book4, Book5 }
            .Where(b => b != book)
            .Where(b => Math.Abs((b.Margin.Left + b.Width / 2) - axis) < 30)
            .OrderByDescending(b => b.Margin.Bottom + b.Height);  // 修改为从高到低排序

        // 清除当前书的堆叠关系
        bookStack.Remove(book);

        double newBottom;

        // 找到轴上最高的书
        var highestBook = booksOnAxis.FirstOrDefault();  // 由于是从高到低排序，直接取第一个
        if (highestBook != null)
        {
            // 如果轴上有书，放在最高的书上面
            newBottom = highestBook.Margin.Bottom + highestBook.Height;
            bookStack[book] = highestBook;
        }
        else
        {
            // 如果轴上没有书，使用基准高度
            newBottom = 92;
        }

        // 使用动画移动到新位置
        var animation = new Animation();

        animation.Add(0, 1, new Animation((value) =>
        {
            book.Margin = new Thickness(value, 0, book.Margin.Right, book.Margin.Bottom);
        }, book.Margin.Left, newLeft));

        animation.Add(0, 1, new Animation((value) =>
        {
            book.Margin = new Thickness(book.Margin.Left, 0, book.Margin.Right, value);
        }, book.Margin.Bottom, newBottom));

        animation.Commit(book, "AlignAnimation", 16, 250, Easing.CubicInOut);
        originalPositions[book] = (newLeft, newBottom);

        // 检查是否完成谜题
        CheckPuzzleComplete();
    }

    private void PlaceBookOnTarget(Image draggedBook, Image targetBook)
    {
        // 清除旧的堆叠关系
        bookStack.Remove(draggedBook);

        // 找到目标书所在轴
        double targetAxis = axisPositions
            .OrderBy(x => Math.Abs((targetBook.Margin.Left + targetBook.Width / 2) - x))
            .First();

        // 计算新位置（中心对齐到轴线，直接贴合目标书）
        double newLeft = targetAxis - draggedBook.Width / 2;
        double newBottom = targetBook.Margin.Bottom + targetBook.Height; // 直接贴合没有间隙

        // 检查是否会与其他书重叠
        var otherBooksOnAxis = new[] { Book1, Book2, Book3, Book4, Book5 }
            .Where(b => b != draggedBook && b != targetBook)
            .Where(b => Math.Abs((b.Margin.Left + b.Width / 2) - targetAxis) < 30)
            .Where(b => Math.Abs(b.Margin.Bottom - newBottom) < 1);

        if (otherBooksOnAxis.Any())
        {
            ReturnBookToOriginalPosition(draggedBook);
            return;
        }

        // 更新堆叠关系

        bookStack[draggedBook] = targetBook;

        // 用动画移动到新位置
        var animation = new Animation();

        animation.Add(0, 1, new Animation((value) =>
        {
            draggedBook.Margin = new Thickness(
                value,
                0,
                draggedBook.Margin.Right,
                draggedBook.Margin.Bottom
            );
        }, draggedBook.Margin.Left, newLeft));

        animation.Add(0, 1, new Animation((value) =>
        {
            draggedBook.Margin = new Thickness(
                draggedBook.Margin.Left,
                0,
                draggedBook.Margin.Right,
                value
            );
        }, draggedBook.Margin.Bottom, newBottom));

        animation.Commit(draggedBook, "StackAnimation", 16, 250, Easing.CubicInOut);

        // 检查是否完成谜题
        CheckPuzzleComplete();
    }

    private void ReturnBookToOriginalPosition(Image book)
    {
        if (!originalPositions.ContainsKey(book)) return;

        var (originalX, originalY) = originalPositions[book];

        var animation = new Animation();

        animation.Add(0, 1, new Animation((value) =>
        {
            book.Margin = new Thickness(
                value,
                0,
                book.Margin.Right,
                book.Margin.Bottom
            );
        }, book.Margin.Left, originalX));

        animation.Add(0, 1, new Animation((value) =>
        {
            book.Margin = new Thickness(
                book.Margin.Left,
                0,
                book.Margin.Right,
                value
            );
        }, book.Margin.Bottom, originalY));

        animation.Commit(book, "ReturnAnimation", 16, 250, Easing.SpringOut);
    }

    private async void CheckPuzzleComplete()
    {
        try
        {
            var booksOnThirdAxis = new[] { Book1, Book2, Book3, Book4, Book5 }
                .Where(b => Math.Abs((b.Margin.Left + b.Width / 2) - axisPositions[2]) < 30)
                .OrderBy(b => b.Margin.Bottom)
                .ToList();

            if (booksOnThirdAxis.Count != 5)
            {
                return;
            }

            if (booksOnThirdAxis[0] != Book1 ||
                booksOnThirdAxis[1] != Book2 ||
                booksOnThirdAxis[2] != Book3 ||
                booksOnThirdAxis[3] != Book4 ||
                booksOnThirdAxis[4] != Book5)
            {
                return;
            }

            if (!_hasPictureFragment)
            {
                // 找到一个空的物品栏位置或已有画碎片的位置
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
                        ItemName = "books_picture_fragment",
                        SlotIndex = slotIndex,
                        IsCollected = true
                    };
                    await App.InventoryRepo.SaveInventoryItemAsync(newItem);

                    // 更新UI
                    UpdateInventorySlot(slotIndex, "sofa_picture_fragment");
                    _hasPictureFragment = true;

                    // 更新获取最新的画碎片数量
                    items = await App.InventoryRepo.GetInventoryItemsAsync();
                    int fragmentCount = items.Count(i => i.ItemName.Contains("picture_fragment"));

                    // 显示提示文本和动画效果
                    string hintText = $"画碎片 {fragmentCount}/5";
                    ShowHint(hintText);

                    // 执行物品栏动画
                    if (_inventoryLayout.Children[slotIndex] is Grid grid &&
                        grid.Children.LastOrDefault() is Image inventoryItem)
                    {
                        // 执行画碎片的放大缩小动画
                        await inventoryItem.ScaleTo(1.2, 100);
                        await inventoryItem.ScaleTo(1.0, 100);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in CheckPuzzleComplete: {ex.Message}");
        }
    }

    // 添加ShowHint方法，与Room1Wall1中的实现类似
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

    private bool CheckColorsMatch(Image upperBook, Image lowerBook)
    {
        // 获取上下两本书的颜色
        var upperColor = GetBookColor(upperBook);
        var lowerColor = GetBookColor(lowerBook);

        // 检查相邻的颜色是否匹配
        return upperColor == lowerColor;
    }

    private string GetBookColor(Image book)
    {
        // 根据图片中显示的颜色来判断
        if (book == Book1) return "green";  // 第一本书顶部是绿色
        if (book == Book2) return "green";  // 第二本书底部是绿色
        if (book == Book3) return "brown";  // 第三本书是棕色
        if (book == Book4) return "brown";  // 第四本书顶部是棕色
        if (book == Book5) return "brown";  // 第五本书底部是棕色
        return "";
    }

    private bool IsOnThirdAxis(Image book)
    {
        // 检查book1是否在第三个轴上
        var bookBounds = book.Bounds;
        return Math.Abs(bookBounds.Center.X - 419) < 50; // 使用Book3的X坐标作为参考
    }

    public async void OnBackClicked(object sender, EventArgs e)
    {
        if (!isNavigating && this.Opacity > 0)  // 检查导航状态和页面是否可见
        {
            isNavigating = true;
            await this.FadeTo(0, 500);
            await Shell.Current.GoToAsync("//Room1Wall3");
        }
    }

    public async void OnSettingClicked(object sender, EventArgs e)
    {
        if (!isNavigating && this.Opacity > 0)
        {
            isNavigating = true;
            await App.SettingRepo.UpdateBackPage(1, "MoveBooksPuzzle");
            await Shell.Current.GoToAsync("//SettingPage");
        }
    }

    private bool CanMoveBook(Image book)
    {
        // 检查这本书是否是堆栈中的顶部书籍
        foreach (var pair in bookStack)
        {
            if (pair.Value == book)
            {
                // 如果这本书是另一本书的底部，则不能移动
                return false;
            }
        }

        // 检查这本书是否在第三轴上且不是顶部书籍
        if (IsOnThirdAxis(book))
        {
            // 获取第三轴上的所有书
            var booksOnThirdAxis = new[] { Book1, Book2, Book3, Book4, Book5 }
                .Where(b => IsOnThirdAxis(b))
                .ToList();

            // 计算这本书的Y位置
            double bookY = book.Margin.Bottom;

            // 检查是否有书在这本书的上面
            foreach (var otherBook in booksOnThirdAxis)
            {
                if (otherBook != book)
                {
                    double otherBookY = otherBook.Margin.Bottom;
                    
                    // 如果另一本书的Y位置大于这本书的Y位置，且X位置接近，则表示另一本书在这本书上面
                    if (otherBookY > bookY && Math.Abs(otherBook.Margin.Left - book.Margin.Left) < 30)
                    {
                        return false;
                    }
                }
            }
        }

        // 检查这本书是否在其他轴上且有其他书在其上方
        double bookCenterX = book.Margin.Left + book.Width / 2;
        double bookTop = book.Margin.Bottom + book.Height;
        
        foreach (var otherBook in new[] { Book1, Book2, Book3, Book4, Book5 })
        {
            if (otherBook != book)
            {
                double otherBookCenterX = otherBook.Margin.Left + otherBook.Width / 2;
                double otherBookBottom = otherBook.Margin.Bottom;
                
                // 如果另一本书在这本书的上方（X位置接近且Y位置高于这本书的顶部）
                if (Math.Abs(otherBookCenterX - bookCenterX) < 30 && otherBookBottom > bookTop - 20)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private Image FindTargetBook(Image draggedBook)
    {
        // 获取所有其他书籍
        var otherBooks = new[] { Book1, Book2, Book3, Book4, Book5 }
            .Where(b => b != draggedBook)
            .ToList();

        // 检查拖动的书是否在其他书上方
        foreach (var targetBook in otherBooks)
        {
            // 检查目标书是否已经有书在上面
            bool hasBookOnTop = false;
            foreach (var pair in bookStack)
            {
                if (pair.Value == targetBook)
                {
                    hasBookOnTop = true;
                    break;
                }
            }

            // 如果目标书已经有书在上面，则跳过
            if (hasBookOnTop)
            {
                continue;
            }

            // 检查拖动的书是否在目标书上方
            if (IsOverBook(draggedBook, targetBook))
            {
                // 检查颜色是否匹配
                if (CheckColorsMatch(draggedBook, targetBook))
                {
                    return targetBook;
                }
                else
                {
                    // 如果颜色不匹配，显示提示
                    ShowHint("书的颜色不匹配哦");
                    return null;
                }
            }
        }

        return null;
    }

    private bool IsOverBook(Image draggedBook, Image targetBook)
    {
        // 获取两本书的中心点和位置
        double draggedCenterX = draggedBook.Margin.Left + draggedBook.Width / 2;
        double targetCenterX = targetBook.Margin.Left + targetBook.Width / 2;
        double draggedBottom = draggedBook.Margin.Bottom;
        double targetBottom = targetBook.Margin.Bottom;

        // 检查是否在同一轴线上
        bool onSameAxis = Math.Abs(draggedCenterX - targetCenterX) < 50; // 放宽判定范围

        // 垂直位置（确保拖动的书在目标书上方）
        bool verticallyAligned = draggedBottom < targetBottom + targetBook.Height;

        return onSameAxis && verticallyAligned;
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
    private void tipClicked(object sender, EventArgs e)
    {
        
            ShowHint("汉诺塔？你说会不会画碎片就在最上面那本书里夹着，要抖一抖");
        
    }
}