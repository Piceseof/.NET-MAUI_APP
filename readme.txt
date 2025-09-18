# 密室逃脱游戏项目文件说明

本文件夹包含了密室逃脱游戏开发相关的所有文件，涵盖了游戏的各个方面，如界面设计、音效处理、数据管理等。以下是对项目中所有文件的详细描述：

## 核心程序文件

- `App.xaml` 和 `App.xaml.cs`：应用程序的主入口点，管理应用生命周期和全局资源。
- `MauiProgram.cs`：定义应用程序的初始化和依赖注入。
- `AppShell.xaml` 和 `AppShell.xaml.cs`：应用程序的导航框架和路由系统。
- `MainPage.xaml` 和 `MainPage.xaml.cs`：应用程序的主页面。

## 数据模型 (Models)

- `Setting.cs`：定义了游戏设置相关的数据模型，包含音乐设置、音量控制、游戏时长等属性。
- `InventoryItem.cs`：定义了游戏中的物品栏项目的数据模型，管理玩家收集的物品。
- `GameState.cs`：存储游戏状态信息，如关卡完成情况、玩家成就、谜题解锁状态等。

## 视图 (Views)

### 主要界面
- `StartGamePage.xaml` 和 `StartGamePage.xaml.cs`：游戏开始页面，提供新游戏和继续游戏选项。
- `SettingPage.xaml` 和 `SettingPage.xaml.cs`：设置页面，允许用户调整音乐、音量和查看游戏时长统计。

### 房间界面
- `Room1Wall1.xaml` 和 `Room1Wall1.xaml.cs`：第一房间第一面墙的界面，包含血迹、图片线索等互动元素。
- `Room1Wall2.xaml` 和 `Room1Wall2.xaml.cs`：第一房间第二面墙的界面，包含架子、保险箱等互动元素。
- `Room1Wall3.xaml` 和 `Room1Wall3.xaml.cs`：第一房间第三面墙的界面，包含更多谜题和互动元素。
- `Room1Wall4.xaml` 和 `Room1Wall4.xaml.cs`：第一房间第四面墙的界面，包含额外的线索和出口。

### 谜题界面
- `ComputerPuzzle.xaml` 和 `ComputerPuzzle.xaml.cs`：电脑密码谜题界面，玩家需要输入正确密码。
- `ComputerHintPage.xaml` 和 `ComputerHintPage.xaml.cs`：电脑提示页面，提供解密线索。
- `LightSwitchPage.xaml` 和 `LightSwitchPage.xaml.cs`：电灯开关谜题，控制房间灯光。
- `PuzzlePiecesSlove.xaml` 和 `PuzzlePiecesSlove.xaml.cs`：拼图解谜界面，玩家需要点击正确的图块。
- `BookShelfMove.xaml` 和 `BookShelfMove.xaml.cs`：书架移动谜题，玩家需要按正确顺序移动书籍。
- `MoveBooksPuzzle.xaml` 和 `MoveBooksPuzzle.xaml.cs`：另一个书籍移动谜题，要求按颜色排列书籍。
- `BoxRiddle.xaml` 和 `BoxRiddle.xaml.cs`：盒子谜题界面，玩家需要正确放置拼图块。
- `SafeDepositBox.xaml` 和 `SafeDepositBox.xaml.cs`：保险箱谜题，需要输入正确的数字组合。
- `ScalePage.xaml` 和 `ScalePage.xaml.cs`：天平称重解密，需要称出四个球的重量得到保险箱密码。
- `CabinettRightRiddle.xaml` 和 `CabinettRightRiddle.xaml.cs`：右侧柜子谜题，包含图像序列谜题。
- `CabinettLeftRiddle.xaml` 和 `CabinettLeftRiddle.xaml.cs`：左侧柜子谜题，包含隐藏图片碎片。
- `PictureDisplayPage.xaml` 和 `PictureDisplayPage.xaml.cs`：图片显示页面，展示收集到的图片碎片。
- `PaperDisplayPage.xaml` 和 `PaperDisplayPage.xaml.cs`：开始解密时的文字提示。
- `AfterIntroduce.xaml` 和 `AfterIntroduce.xaml.cs`：解密完成时的通关界面。
- `startintroduce.xaml` 和 `startintroduce.xaml.cs`：开始解密进入解密界面前的界面。

## 服务 (Services)

- `AudioService.cs`：处理游戏中的音频播放，包括背景音乐和音效控制，支持音量调节和音效播放。
- `SettingDatabase.cs`：管理游戏设置的数据库操作，保存和加载玩家的偏好设置。
- `InventoryDatabase.cs`：管理游戏物品栏的数据库操作，包括添加、删除和查询物品。
- `GameStateDatabase.cs`：管理游戏状态的数据库操作，包括保存游戏进度和加载保存点。

## 平台特定代码 (Platforms)

### Windows
- `Platforms/Windows/App.xaml.cs`：Windows 平台的应用程序入口和初始化代码。
- `Platforms/Windows/Package.appxmanifest`：Windows 应用包清单文件。

### Android
- `Platforms/Android/MainActivity.cs`：Android 平台的主活动类。
- `Platforms/Android/MainApplication.cs`：Android 应用程序类。
- `Platforms/Android/AndroidManifest.xml`：Android 清单文件。

### iOS
- `Platforms/iOS/AppDelegate.cs`：iOS 平台的应用程序委托类。
- `Platforms/iOS/Program.cs`：iOS 的程序入口点。
- `Platforms/iOS/Info.plist`：iOS 信息属性列表文件。

## 工具和辅助类 (Utilities)

- `Constants.cs`：定义了整个项目中使用的常量，如数据库路径、游戏配置值等。
- `Helpers/DialogHelper.cs`：提供对话框显示和用户交互的辅助方法。
- `Helpers/NavigationHelper.cs`：提供页面导航的辅助方法。

## 资源文件 (Resources)

- `Resources/Images/`：包含游戏中使用的所有图像资源，包括房间背景、道具、按钮和UI元素。
- `Resources/Raw/`：包含游戏中使用的原始资源文件，如音频文件、背景音乐和音效。
- `Resources/Fonts/`：包含游戏中使用的字体资源，用于文本显示。
- `Resources/AppIcon/`：包含应用程序图标文件。
- `Resources/Splash/`：包含应用启动画面资源。

## 配置文件

- `.gitignore`：Git版本控制忽略文件配置。
- `Games.csproj`：项目配置文件，定义了项目结构和依赖关系。
- `README.md`：项目说明文档（Markdown格式）。
- `readme.txt`：本文件，提供项目文件详细说明。

此项目是一个完整的密室逃脱游戏，通过上述文件的协同工作，提供了一个沉浸式的游戏体验。每个文件都有特定的功能和责任，确保游戏能够顺利运行并提供良好的用户体验。游戏主要由解谜元素组成，玩家需要通过收集线索、解决谜题来完成游戏目标。