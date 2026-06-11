using System;
using System.Linq;
using System.Windows;

namespace RotinaClone.App.Helpers
{
    public static class ThemeManager
    {
        public static bool IsDarkTheme { get; private set; } = true;

        public static void SwitchTheme(bool isDark)
        {
            IsDarkTheme = isDark;
            var app = System.Windows.Application.Current;
            if (app == null) return;

            // Remove existing theme dictionaries (DarkTheme or LightTheme)
            var existingTheme = app.Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source != null && (d.Source.OriginalString.Contains("DarkTheme.xaml") || d.Source.OriginalString.Contains("LightTheme.xaml")));
            if (existingTheme != null)
            {
                app.Resources.MergedDictionaries.Remove(existingTheme);
            }

            // Create the new theme dictionary
            var newTheme = new ResourceDictionary
            {
                Source = new Uri(isDark ? "Themes/DarkTheme.xaml" : "Themes/LightTheme.xaml", UriKind.Relative)
            };

            // Find the SharedResources dictionary to insert after it
            var sharedResources = app.Resources.MergedDictionaries.FirstOrDefault(d =>
                d.Source != null && d.Source.OriginalString.Contains("SharedResources.xaml"));
            if (sharedResources != null)
            {
                int insertIndex = app.Resources.MergedDictionaries.IndexOf(sharedResources) + 1;
                app.Resources.MergedDictionaries.Insert(insertIndex, newTheme);
            }
            else
            {
                // Fallback: insert at the beginning
                app.Resources.MergedDictionaries.Insert(0, newTheme);
            }
        }
    }
}
