using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OpenCursor.BrowserHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string LocalHostUrl = "http://localhost:12345/prompt/"; // URL to listen for POST requests
        private const string DeepSeekUrl = "https://chat.deepseek.com"; // Or OpenAI playground URL
        private const string OpenAiUrl = "https://chat.openai.com"; // Or OpenAI playground URL

        public MainWindow()
        {
            InitializeComponent();
            InitializeWebView2();
            StartHttpListener();
        }

        private async void InitializeWebView2()
        {
            // Initialize WebView2
            await WebView.EnsureCoreWebView2Async();

            // Navigate to the DeepSeek (or OpenAI) page
            WebView.CoreWebView2.Navigate(OpenAiUrl);
        }

        private void StartHttpListener()
        {
            Task.Run(() =>
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(LocalHostUrl);
                listener.Start();

                while (true)
                {
                    var context = listener.GetContext();
                    var request = context.Request;
                    var response = context.Response;

                    if (request.HttpMethod == "POST")
                    {
                        var body = new System.IO.StreamReader(request.InputStream, request.ContentEncoding).ReadToEnd();
                        ProcessCommand(body);
                    }

                    response.StatusCode = 200;
                    response.Close();
                }
            });
        }

        private async void ProcessCommand(string command)
        {
            // Here we assume the command is a "prompt" to inject into the browser
            // This is where you inject into WebView2 (DeepSeek/OpenAI)
            await WebView.CoreWebView2.ExecuteScriptAsync($"document.querySelector('textarea').value = `{command}`;");
            // Optionally, simulate hitting "Enter" after pasting the prompt:
            await WebView.CoreWebView2.ExecuteScriptAsync("document.querySelector('textarea').dispatchEvent(new Event('input'));");
        }
    }
}