using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WingTextEditor.Dependency_Objects
{
    public static class TextBoxHelper
    {
        public static string GetSelectedText(DependencyObject obj)
        {
            return (string)obj.GetValue(SelectedTextProperty);
        }
        public static void SetSelectedText(DependencyObject obj, string value)
        {
            obj.SetValue(SelectedTextProperty, value);
        }
        public static readonly DependencyProperty SelectedTextProperty =
            DependencyProperty.RegisterAttached(
                "SelectedText",
                typeof(string),
                typeof(TextBoxHelper),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    SelectedTextChanged));

   

        private static void SelectedTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (dependencyObject is not TextBox textBox)
            {
                return;
            }
            var oldValue = eventArgs.OldValue as string;
            var newValue = eventArgs.NewValue as string;

            if (oldValue == null && newValue != null)
            {
                textBox.SelectionChanged += SelectionChangedForSelectedText;
            }
            else if (oldValue != null && newValue == null)
            {
                textBox.SelectionChanged -= SelectionChangedForSelectedText;
            }

            string selectedTemp = textBox.SelectedText;
      

            if (newValue is not null && newValue != textBox.SelectedText)
            {
               // textBox.SelectedText = newValue;
            }

            if (newValue == selectedTemp)
            {
             /*   textBox.Focus();
                textBox.SelectAll();*/

            }

       

            

        }
        private static void SelectionChangedForSelectedText(object sender, RoutedEventArgs eventArgs)
        {
            if (sender is TextBox textBox)
            {
                SetSelectedText(textBox, textBox.SelectedText);

            }
        }

        public static readonly DependencyProperty CaretPositionProperty = DependencyProperty.RegisterAttached(
            "CaretIndex", typeof(int), typeof(TextBoxHelper), new FrameworkPropertyMetadata(
                -1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, CaretPositionChanged));

        private static void CaretPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TextBox textBox)
            {
                return;
            }
            int oldValue = (int)e.OldValue;
            int newValue = (int)e.NewValue;

            if (oldValue == -1 && newValue != -1)
            {
                textBox.SelectionChanged += TextBox_SelectionChanged;
            }
            else if (oldValue != -1 && newValue == -1)
            {
                textBox.SelectionChanged -= TextBox_SelectionChanged;
            }

            if (newValue!=-1 && newValue != textBox.CaretIndex)
            {
                textBox.CaretIndex = newValue;
            }

        }

        private static void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                SetCaretIndex(textBox, textBox.CaretIndex);
            }
        }
        public static int GetCaretIndex(DependencyObject obj)
        {
            return (int)obj.GetValue(CaretPositionProperty);
        }
        public static void SetCaretIndex(DependencyObject obj, int value)
        {
            obj.SetValue(CaretPositionProperty, value);
        }

        //-------------------------

        public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached(
    "FocusControl", typeof(bool), typeof(TextBoxHelper), new FrameworkPropertyMetadata(
        default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsFocusedChanged));

        private static void IsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            TextBox textBox=d as TextBox;

            if ((bool)e.NewValue == true && (bool)e.OldValue!=true)
            {
             
                textBox.IsKeyboardFocusedChanged += TextBox_IsKeyboardFocusedChanged; 
            }
            else if((bool)e.NewValue != true && (bool)e.OldValue == true)
                textBox.IsKeyboardFocusedChanged -= TextBox_IsKeyboardFocusedChanged;

            if(textBox.IsKeyboardFocused!=(bool)e.NewValue)
                textBox.Focus();

        }
        private static void TextBox_IsKeyboardFocusedChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                SetCaretIndex(textBox, textBox.CaretIndex);
            }
        }
        public static bool GetFocusControl(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }
        public static void SetFocusControl(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);

        }


    }
}
