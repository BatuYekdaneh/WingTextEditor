using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WingTextEditor.Dependency_Objects
{
    public static class WidthHeightHelper
    {
        public static readonly DependencyProperty WidthProperty = DependencyProperty.RegisterAttached(
            "RenderedWidth", typeof(double),typeof(WidthHeightHelper),new FrameworkPropertyMetadata(
                0.1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,WidthChanged));

        private static void WidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            TabControl tabControl = (TabControl)d;

            if((double)e.NewValue != 0.1 && (double)e.OldValue == 0.1)
                tabControl.SizeChanged += TabControl_SizeChanged;
            else if((double)e.NewValue == 0.1 && (double)e.OldValue != 0.1)
                tabControl.SizeChanged -= TabControl_SizeChanged;

            double width = (double)e.NewValue;

            if (width != tabControl.ActualWidth)
                tabControl.Width = width;

        

        }

        private static void TabControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            TabControl tabControl = sender as TabControl;
            if(tabControl != null)
            {
                SetRenderedWidth(tabControl,tabControl.ActualWidth);
            }

        }

        public static double GetRenderedWidth(DependencyObject obj)
        {
            return (double)obj.GetValue(WidthProperty);
        }
        public static void SetRenderedWidth(DependencyObject obj, double value)
        {
            obj.SetValue(WidthProperty, value);
        }


    }
}
