using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Workbench.Behaviors
{
    public static class InputMaskBehavior
    {
        // 1. 创建一个附加属性来启用或禁用行为
        public static readonly DependencyProperty IsBinaryOnlyProperty =
            DependencyProperty.RegisterAttached(
                "IsBinaryOnly", // 属性名
                typeof(bool),   // 属性类型
                typeof(InputMaskBehavior), // 所属类
                new PropertyMetadata(false, OnIsBinaryOnlyChanged)); // 默认值和回调

        public static void SetIsBinaryOnly(DependencyObject element, bool value)
        {
            element.SetValue(IsBinaryOnlyProperty, value);
        }

        public static bool GetIsBinaryOnly(DependencyObject element)
        {
            return (bool)element.GetValue(IsBinaryOnlyProperty);
        }

        // 2. 当附加属性的值改变时，这个回调函数会被调用
        private static void OnIsBinaryOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is TextBox textBox))
            {
                return; // 只对TextBox生效
            }

            if ((bool)e.NewValue)
            {
                // 如果属性设置为 true, 则挂接事件
                textBox.PreviewTextInput += TextBox_PreviewTextInput;
                DataObject.AddPastingHandler(textBox, TextBox_Pasting);
            }
            else
            {
                // 如果属性设置为 false, 则移除事件，防止内存泄漏
                textBox.PreviewTextInput -= TextBox_PreviewTextInput;
                DataObject.RemovePastingHandler(textBox, TextBox_Pasting);
            }
        }

        // 3. 将之前的事件处理逻辑放到这里
        private static void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text != "0" && e.Text != "1")
            {
                e.Handled = true;
            }
        }

        private static void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!text.All(c => c == '0' || c == '1'))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }
    }
}
