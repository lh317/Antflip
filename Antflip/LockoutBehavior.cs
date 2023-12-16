// Copyright 2023 lh317
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
using System.Windows;

using Microsoft.Xaml.Behaviors;

using ModernWpf.Controls;

namespace Antflip
{

    public class LockoutBehavior : Behavior<Frame>
    {
        /// <summary>Identifies the <see cref="LockoutPageType" /> dependency property.</summary>
        public static readonly DependencyProperty LockoutPageTypeProperty = DependencyProperty.Register(
            nameof(LockoutPageType),
            typeof(Type),
            typeof(LockoutBehavior),
            new PropertyMetadata(),
            new ValidateValueCallback(IsValidLockoutPageType)
        );

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register(
            nameof(IsEnabled),
            typeof(bool),
            typeof(LockoutBehavior),
            new FrameworkPropertyMetadata(true, IsEnabledPropertyChanged)
        );

        private static void IsEnabledPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((LockoutBehavior)d).DoLockOut(!(bool)e.NewValue);
        }

        public static bool IsValidLockoutPageType(object? value) {
            return null == value || typeof(Page).IsAssignableFrom(value as Type);
        }

        /// <summary>
        /// Gets or sets the <c>typeof(Page)</c> which is loaded when navigating to lock out.
        /// </summary>
        public Type? LockoutPageType {
            get { return (Type)GetValue(LockoutPageTypeProperty); }
            set { SetValue(LockoutPageTypeProperty, value); }
        }

        public bool IsEnabled {
            get { return (bool)GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        protected bool DoLockOut(bool enable) {
            if (null == this.AssociatedObject) {
               throw new InvalidOperationException();
            }
            if (enable) {
                if (null != this.LockoutPageType) {
                    return this.AssociatedObject.Navigate(this.LockoutPageType);
                }
                throw new InvalidOperationException();
            } else if (this.AssociatedObject.CanGoBack) {
                this.AssociatedObject.GoBack();
                return true;
            }
            return false;
        }
    }
}
