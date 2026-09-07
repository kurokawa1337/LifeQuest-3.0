using System.Windows;

namespace LifeQuest.Services;

public static class ThemeManager
{
    public static string Current { get; private set; } = "light";

    public static void Apply(string theme)
    {
        theme = theme == "dark" ? "dark" : "light";
        Current = theme;

        var uri = new Uri($"Themes/{(theme == "dark" ? "Dark" : "Light")}.xaml", UriKind.Relative);
        var dict = new ResourceDictionary { Source = uri };

        var app = Application.Current;
        if (app == null) return;

        var merged = app.Resources.MergedDictionaries;
        for (int i = merged.Count - 1; i >= 0; i--)
        {
            if (merged[i].Contains("ThemeMarker"))
            {
                merged.RemoveAt(i);
            }
        }
        merged.Insert(0, dict);
    }
}
