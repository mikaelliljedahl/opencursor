using OpenCursor.Client;

// Set up
Console.CursorVisible = false;

var browser = new DirectoryBrowser();
var ui = new UIRenderer(browser);
var navigator = new KeyboardNavigator(browser, ui);

// Start
browser.LoadDirectory(Directory.GetCurrentDirectory());

bool running = true;
while (running)
{
    ui.Draw();
    running = navigator.HandleKeyPress();
}
