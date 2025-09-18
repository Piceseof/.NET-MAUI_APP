using Android.Views;
using Android.OS;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using Android.App;
using Android.Content.PM;
using Microsoft.Maui.Controls;

namespace Games
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize, ScreenOrientation = ScreenOrientation.Landscape)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // 设置全屏显示
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            Window.AddFlags(WindowManagerFlags.LayoutNoLimits);
            
            // 隐藏系统UI（状态栏和导航栏）
            var uiOptions = (int)Window.DecorView.SystemUiVisibility;
            uiOptions |= (int)SystemUiFlags.LowProfile;
            uiOptions |= (int)SystemUiFlags.Fullscreen;
            uiOptions |= (int)SystemUiFlags.HideNavigation;
            uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
            uiOptions |= (int)SystemUiFlags.LayoutFullscreen;
            uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
        }

        public override void OnWindowFocusChanged(bool hasFocus)
        {
            base.OnWindowFocusChanged(hasFocus);
            
            if (hasFocus)
            {
                // 当窗口获得焦点时重新应用全屏设置
                var uiOptions = (int)Window.DecorView.SystemUiVisibility;
                uiOptions |= (int)SystemUiFlags.LowProfile;
                uiOptions |= (int)SystemUiFlags.Fullscreen;
                uiOptions |= (int)SystemUiFlags.HideNavigation;
                uiOptions |= (int)SystemUiFlags.ImmersiveSticky;
                uiOptions |= (int)SystemUiFlags.LayoutFullscreen;
                uiOptions |= (int)SystemUiFlags.LayoutHideNavigation;
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)uiOptions;
            }
        }
    }
}