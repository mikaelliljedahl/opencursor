using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using System.Threading.Tasks;

namespace OpenCursor.Host.Pages
{
    public partial class Settings : ComponentBase
    {
        [Inject]
        public SettingsService SettingsService { get; set; } = default!;

        private AppSettings? _settings;
        private string? _saveMessage;

        protected override async Task OnInitializedAsync()
        {
            _settings = await SettingsService.LoadAsync();
        }

        private async Task SaveSettings()
        {
            await SettingsService.SaveAsync(_settings!);
            _saveMessage = "Settings saved.";
        }
    }
}
