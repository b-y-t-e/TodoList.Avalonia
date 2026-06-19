using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(TodoListControl.Tests.TestAppBuilder))]

namespace TodoListControl.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());
}

public class App : Application
{
}
