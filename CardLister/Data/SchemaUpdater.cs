using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CardLister.Data
{
    public static class SchemaUpdater
    {
        public static async Task EnsureVerificationTablesAsync(CardListerDbContext db)
        {
            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS set_checklists (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Manufacturer TEXT NOT NULL,
                    Brand TEXT NOT NULL,
                    Year INTEGER NOT NULL,
                    Sport TEXT,
                    Cards TEXT NOT NULL DEFAULT '[]',
                    KnownVariations TEXT NOT NULL DEFAULT '[]',
                    TotalBaseCards INTEGER NOT NULL DEFAULT 0,
                    CachedAt TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'
                );");

            await db.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_set_checklists_Manufacturer_Brand_Year_Sport
                ON set_checklists (Manufacturer, Brand, Year, Sport);");

            await db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS missing_checklists (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Manufacturer TEXT NOT NULL,
                    Brand TEXT NOT NULL,
                    Year INTEGER NOT NULL,
                    Sport TEXT,
                    HitCount INTEGER NOT NULL DEFAULT 1,
                    FirstSeen TEXT NOT NULL DEFAULT '0001-01-01T00:00:00',
                    LastSeen TEXT NOT NULL DEFAULT '0001-01-01T00:00:00'
                );");

            await db.Database.ExecuteSqlRawAsync(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_missing_checklists_Manufacturer_Brand_Year_Sport
                ON missing_checklists (Manufacturer, Brand, Year, Sport);");
        }
    }
}
