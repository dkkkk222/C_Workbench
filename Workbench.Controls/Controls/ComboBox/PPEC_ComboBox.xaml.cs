using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Workbench.Controls.Models;

namespace Workbench.Controls
{
    /// <summary>
    /// PPEC_ComboBox.xaml 的交互逻辑
    /// </summary>
    public partial class PPEC_ComboBox : UserControl
    {
        public PPEC_ComboBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TitleHorizontalAlignmentProperty =
            DependencyProperty.Register("TitleHorizontalAlignment", typeof(HorizontalAlignment), typeof(PPEC_ComboBox));
        public HorizontalAlignment TitleHorizontalAlignment
        {
            get { return (HorizontalAlignment)GetValue(TitleHorizontalAlignmentProperty); }
            set { SetValue(TitleHorizontalAlignmentProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(PPEC_ComboBox), new PropertyMetadata(""));
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleWidthProperty =
            DependencyProperty.Register("TitleWidth", typeof(double), typeof(PPEC_ComboBox), new PropertyMetadata(100d));
        public double TitleWidth
        {
            get { return (double)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(PPEC_ComboBox));
        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public new static readonly DependencyProperty HeightProperty =
            DependencyProperty.Register("Height", typeof(double), typeof(PPEC_ComboBox), new PropertyMetadata(25d));
        public new double Height
        {
            get { return (double)GetValue(HeightProperty); }
            set { SetValue(HeightProperty, value); }
        }

        public new static readonly DependencyProperty WidthProperty =
            DependencyProperty.Register("Width", typeof(double), typeof(PPEC_ComboBox));
        public new double Width
        {
            get { return (double)GetValue(WidthProperty); }
            set { SetValue(WidthProperty, value); }
        }

        public static readonly DependencyProperty DisplayMemberPathProperty =
            DependencyProperty.Register("DisplayMemberPath", typeof(string), typeof(PPEC_ComboBox));
        public string DisplayMemberPath
        {
            get { return (string)GetValue(DisplayMemberPathProperty); }
            set { SetValue(DisplayMemberPathProperty, value); }
        }

        public static readonly DependencyProperty SelectedValuePathProperty =
            DependencyProperty.Register("SelectedValuePath", typeof(string), typeof(PPEC_ComboBox));
        public string SelectedValuePath
        {
            get { return (string)GetValue(SelectedValuePathProperty); }
            set { SetValue(SelectedValuePathProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(object), typeof(PPEC_ComboBox));


        public object SelectedItem
        {
            get { return (object)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public static readonly RoutedEvent ComboBoxSelectionChangedEvent =
            EventManager.RegisterRoutedEvent("ComboBoxSelectionChanged", RoutingStrategy.Bubble, typeof(SelectionChangedEventHandler), typeof(PPEC_ComboBox));

        // .NET wrapper
        public event SelectionChangedEventHandler ComboBoxSelectionChanged
        {
            add { AddHandler(ComboBoxSelectionChangedEvent, value); }
            remove { RemoveHandler(ComboBoxSelectionChangedEvent, value); }
        }

        private void ccb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RaiseEvent(new SelectionChangedEventArgs(ComboBoxSelectionChangedEvent, e.RemovedItems, e.AddedItems));
        }

        public static readonly DependencyProperty SelectedValueProperty =
            DependencyProperty.Register("SelectedValue", typeof(object), typeof(PPEC_ComboBox));
        public object SelectedValue
        {
            get { return (object)GetValue(SelectedValueProperty); }
            set { SetValue(SelectedValueProperty, value); }
        }
    }
}
