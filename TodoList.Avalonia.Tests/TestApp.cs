using global::Avalonia;
using global::Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(TodoList.Avalonia.Tests.TestAppBuilder))]

namespace TodoList.Avalonia.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public class App : Application
{
}
