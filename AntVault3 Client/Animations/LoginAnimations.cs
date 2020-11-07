using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Animation;
using System;

namespace AntVault3_Client.Animations
{
    class LoginAnimations
    {
        internal static void MoveLabel(Label CurrentControll)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Thickness OldThickNess = new Thickness(-200, 0, 0, 0);
                Thickness NewThickness = new Thickness(1024, CurrentControll.Margin.Top, CurrentControll.Margin.Right, CurrentControll.Margin.Bottom);
                ThicknessAnimation MoveAnimation = new ThicknessAnimation(OldThickNess, NewThickness, TimeSpan.FromSeconds(14));
                MoveAnimation.Completed += (sender, e) => MoveAnimation_Completed(sender, e, CurrentControll);
                MoveAnimation.RepeatBehavior = RepeatBehavior.Forever;
                CurrentControll.BeginAnimation(Label.MarginProperty, MoveAnimation);
            });
        }

        internal static void MoveAnimation_Completed(object sender, EventArgs e, Label CurrentControll)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentControll.Margin = new Thickness(-200, 0, 0, 0);
            });
        }
    }
}
