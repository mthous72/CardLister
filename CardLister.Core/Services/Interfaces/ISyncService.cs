using System;
using System.Threading.Tasks;

namespace CardLister.Core.Services
{
    public interface ISyncService
    {
        Task<SyncResult> SyncAsync();
        Task<SyncStatus> GetStatusAsync();
    }

    public class SyncResult
    {
        public bool Success { get; set; }
        public int CardsPushed { get; set; }
        public int CardsPulled { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime SyncTime { get; set; }
    }

    public class SyncStatus
    {
        public bool IsConnected { get; set; }
        public DateTime? LastSync { get; set; }
        public DateTime? ServerLastUpdated { get; set; }
        public int? ServerCardCount { get; set; }
    }
}
