using System.Diagnostics;
using CardLister.Core.Services;

namespace CardLister.Desktop.Services
{
    public class SystemBrowserService : IBrowserService
    {
        public void OpenUrl(string url)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
