using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Antflip.Controls {
    public class LEDLabel : ContentControl {
        static LEDLabel() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(LEDLabel), new FrameworkPropertyMetadata(typeof(LEDLabel)));

            // LEDlabel is not a tabstop nor focusable.
            IsTabStopProperty.OverrideMetadata(typeof(LEDLabel), new FrameworkPropertyMetadata(false));
            FocusableProperty.OverrideMetadata(typeof(LEDLabel), new FrameworkPropertyMetadata(false));
        }

        /// <summary>
        ///     The DependencyProperty for the IsChecked property.
        /// </summary>
        public static readonly DependencyProperty IsCheckedProperty =
                DependencyProperty.Register(
                        "IsChecked",
                        typeof(bool),
                        typeof(LEDLabel),
                        new FrameworkPropertyMetadata(
                                new Nullable<bool>(false),
                                FrameworkPropertyMetadataOptions.None,
                                new PropertyChangedCallback(OnIsCheckedChanged)
                        )
                );

        public static readonly DependencyProperty IsConnectedProperty =
                DependencyProperty.Register(
                        "IsConnected",
                        typeof(bool),
                        typeof(LEDLabel),
                        new FrameworkPropertyMetadata(
                                new Nullable<bool>(true),
                                FrameworkPropertyMetadataOptions.None
                        )
                );


        /// <summary>
        ///     Indicates whether the ToggleButton is checked
        /// </summary>
        public bool IsChecked
        {
            get
            {
                return (bool)GetValue(IsCheckedProperty);
            }
            set
            {
                SetValue(IsCheckedProperty, value);
            }
        }

        public bool IsConnected {
            get {
                return (bool)GetValue(IsConnectedProperty);
            }
            set {
                SetValue(IsConnectedProperty, value);
            }
        }


        /// <summary>
        ///     Called when IsChecked is changed on "d."
        /// </summary>
        /// <param name="d">The object on which the property was changed.</param>
        /// <param name="e">EventArgs that contains the old and new values for this property</param>
        private static void OnIsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            LEDLabel label = (LEDLabel)d;
            bool newValue = (bool) e.NewValue;

            if (newValue == true)
            {
                VisualStateManager.GoToState(label, "Checked", true);

            }
            else
            {
                VisualStateManager.GoToState(label, "Unchecked", true);
            }
        }

        /// <summary>
        ///     Called when IsChecked becomes true.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnChecked(RoutedEventArgs e)
        {

            RaiseEvent(e);
        }

        /// <summary>
        ///     Called when IsChecked becomes false.
        /// </summary>
        /// <param name="e">Event arguments for the routed event that is raised by the default implementation of this method.</param>
        protected virtual void OnUnchecked(RoutedEventArgs e)
        {
            RaiseEvent(e);
        }

        public override void OnApplyTemplate() {
            base.OnApplyTemplate();
            VisualStateManager.GoToState(this, this.IsChecked ? "Checked" : "Unchecked", false);
        }
    }
}
