using Prism.Services.Dialogs;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Shell;

namespace Common.Controls
{
    public class NoTitleBarWindow : Window, IDialogWindow
    {
        private WindowInteropHelper _interopHelper = null;
        /// <summary>
        /// Contains helper for accessing this window handle.
        /// </summary>
        protected WindowInteropHelper InteropHelper
        {
            get
            {
                if (_interopHelper == null)
                {
                    _interopHelper = new WindowInteropHelper(this);
                }
                return _interopHelper;
            }
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            OnExtendsContentIntoTitleBarChanged(default, ExtendsContentIntoTitleBar);
            base.OnSourceInitialized(e);
        }

        /// <summary>
        /// Property for <see cref="ExtendsContentIntoTitleBar"/>.
        /// </summary>
        public static readonly DependencyProperty ExtendsContentIntoTitleBarProperty =
            DependencyProperty.Register(
                nameof(ExtendsContentIntoTitleBar),
                typeof(bool),
                typeof(NoTitleBarWindow),
                new PropertyMetadata(false)
            );


        public static readonly DependencyProperty TitleBarContentProperty =
            DependencyProperty.Register(
                nameof(TitleBarContent),
                typeof(object),
                typeof(NoTitleBarWindow),
                new PropertyMetadata(null, OnExtendsContentIntoTitleBarChanged)
            );

        public static readonly DependencyProperty WindChromeCaptionHeightProperty =
            DependencyProperty.Register(
                nameof(WindChromeCaptionHeight),
                typeof(double),
                typeof(NoTitleBarWindow),
                new PropertyMetadata(18.0, OnExtendsContentIntoTitleBarChanged)
            );

        #region property

        public double WindChromeCaptionHeight
        {
            get => (double)GetValue(WindChromeCaptionHeightProperty);
            set => SetValue(WindChromeCaptionHeightProperty, value);
        }


        /// <summary>
        /// Gets or sets a value that specifies whether the default title bar of the window should be hidden to create space for app content.
        /// </summary>
        public bool ExtendsContentIntoTitleBar
        {
            get => (bool)GetValue(ExtendsContentIntoTitleBarProperty);
            set => SetValue(ExtendsContentIntoTitleBarProperty, value);
        }

        public object TitleBarContent
        {
            get => (object)GetValue(TitleBarContentProperty);
            set => SetValue(TitleBarContentProperty, value);
        }
        public IDialogResult Result { get; set; }

        #endregion

        /// <summary>
        /// Private <see cref="ExtendsContentIntoTitleBar"/> property callback.
        /// </summary>
        private static void OnExtendsContentIntoTitleBarChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e
        )
        {
            if (!(d is NoTitleBarWindow window))
                return;

            if (e.OldValue == e.NewValue)
                return;

            window.OnExtendsContentIntoTitleBarChanged(e.OldValue, e.NewValue);
        }

        /// <summary>
        /// This virtual method is called when <see cref="ExtendsContentIntoTitleBar"/> is changed.
        /// </summary>
        protected virtual void OnExtendsContentIntoTitleBarChanged(object oldValue, object newValue)
        {

            WindowChrome.SetWindowChrome(
                this,
                new WindowChrome
                {
                    CaptionHeight = WindChromeCaptionHeight,
                    GlassFrameThickness = new Thickness(-1),
                    UseAeroCaptionButtons = false
                }
            );
        }
    }
}
