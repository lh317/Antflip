// Copyright 2021 lh317
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

using Microsoft.Xaml.Behaviors;

using ModernWpf.Controls;
using ModernWpf.Media.Animation;

namespace Antflip
{
    /// <summary>
    /// Performs navigation for a <see cref="ModernWpf.Controls.NavigationView" />
    /// or another master/detail navigation system based on user input.
    /// </summary>
    /// <details>
    /// Attach this action to the actual navigation host, such as
    /// <see cref="ModernWpf.Controls.Frame" /> but attach the event trigger to
    /// appropriate event on the navigation view, such as
    /// <see cref="ModernWpf.Controls.NavigationView.ItemInvoked" /> or
    /// <see cref="ModernWpf.Controls.NavigationView.SelectionChanged" />. Set
    /// <see cref="ItemConverter" /> to a <see cref="IValueConverter" /> that
    /// converts from the selected navigation item to the <c>typeof(Page)</c> to
    /// be loaded.  Additionally, set <see cref="SettingsPageType" /> to the
    /// <c>typeof(Page)</c> for the Settings in your application, if it exists.
    /// </details>
    /// <example>
    /// <code>
    /// <ui:NavigationView x:Name="NavigationView" MenuItemsSource="{Binding MenuItems}">
    ///   <ui:Frame>
    ///     <Behaviors:Interaction.Triggers>
    ///       <Behaviors:EventTrigger EventName="SelectionChanged"
    ///                               SourceObject="{Binding ElementName=NavigationView}">
    ///         <NavigationAction ItemConverter="{Binding MenuItemToPage, Mode=OneTime}"
    ///                           SettingsPageType="{Binding SettingsPage, Mode=OneTime}" />
    ///       </Behaviors:EventTrigger>
    ///     </Behaviors:Interaction.Triggers>
    ///   </ui:Frame>
    /// </ui:NavigationView>
    /// </code>
    /// </example>
    public class NavigationAction : TriggerAction<Frame>
    {
        /// <summary>Identifies the <see cref="ItemConverter" /> dependency property.</summary>
        public static readonly DependencyProperty ItemConverterProperty = DependencyProperty.Register(
            nameof(ItemConverter), typeof(IValueConverter), typeof(NavigationAction)
        );

        /// <summary>Identifies the <see cref="ItemConverterParameter" /> dependency property.</summary>
        public static readonly DependencyProperty ItemConverterParameterProperty = DependencyProperty.Register(
            nameof(ItemConverterParameter), typeof(object), typeof(NavigationAction)
        );

        /// <summary>Identifies the <see cref="SettingsPage" /> dependency property.</summary>
        public static readonly DependencyProperty SettingsPageTypeProperty = DependencyProperty.Register(
            nameof(SettingsPageType),
            typeof(Type),
            typeof(NavigationAction),
            new PropertyMetadata(),
            new ValidateValueCallback(IsValidSettingsPageType)
        );

        /// <summary>
        /// Gets or sets the <see cref="IValueConverter" /> that converts the
        /// navigation view menu item to the <c>typeof(Page)</c> to load.
        /// </summary>
        public IValueConverter? ItemConverter {
            get => (IValueConverter)GetValue(ItemConverterProperty);
            set => SetValue(ItemConverterProperty, value);
        }

        /// <summary>
        /// Gets or sets the parameter that is passed to the <see cref="ItemConverter" />.
        /// </summary>
        public object? ItemConverterParameter {
            get => GetValue(ItemConverterParameterProperty);
            set => SetValue(ItemConverterParameterProperty, value);
        }

        /// <summary>
        /// Gets or sets the <c>typeof(Page)</c> which is loaded when navigating
        /// to settings.
        /// </summary>
        public Type? SettingsPageType {
            get { return (Type)GetValue(SettingsPageTypeProperty); }
            set { SetValue(SettingsPageTypeProperty, value); }
        }

        protected override void Invoke(object? parameter) {
            switch(parameter) {
                case NavigationViewItemInvokedEventArgs ie:
                    this.Navigate(ie);
                    break;
                case NavigationViewSelectionChangedEventArgs se:
                    this.Navigate(se);
                    break;
                default:
                    throw new ArgumentException(String.Format(
                        CultureInfo.CurrentCulture, Messages.NavigationActionWrongEventType, parameter?.GetType().Name
                    ));
            }
        }

        [DebuggerStepThrough]
        protected virtual void Navigate(NavigationViewItemInvokedEventArgs e) {
            this.DoNavigate(e.IsSettingsInvoked, e.InvokedItem, e.RecommendedNavigationTransitionInfo);
        }

        [DebuggerStepThrough]
        protected virtual void Navigate(NavigationViewSelectionChangedEventArgs e) {
            this.DoNavigate(e.IsSettingsSelected, e.SelectedItem, e.RecommendedNavigationTransitionInfo);
        }

        protected virtual bool DoNavigate(bool doSettings, object? item, NavigationTransitionInfo transition) {
            if (null == this.AssociatedObject)
                throw new InvalidOperationException();
            if (doSettings) {
                if (null != this.SettingsPageType) {
                    return this.AssociatedObject.Navigate(this.SettingsPageType, null, transition);
                }
                throw new InvalidOperationException();
            } else {
                if (null != this.ItemConverter) {
                    item = this.ItemConverter.Convert(
                        item, typeof(Page), this.ItemConverterParameter, CultureInfo.CurrentCulture
                    );
                }
                return this.AssociatedObject.Navigate(item as Type, null, transition);
            }
        }

        public static bool IsValidSettingsPageType(object? value) {
            return null == value || typeof(Page).IsAssignableFrom(value as Type);
        }
    }
}
