using System.Diagnostics;

namespace CardLister.Services
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
