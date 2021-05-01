using Microsoft.Xaml.Behaviors;

using ModernWpf.Controls;

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
    /// <see cref="ItemConverter" /> to a <see cref="IValueConveter" /> that
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
    public class ContainerAction : TriggerAction<ListView>
    {
        protected override void Invoke(object parameter) {
            throw new System.NotImplementedException();
        }
    }
}
