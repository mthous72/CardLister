using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CardLister.Models;

namespace CardLister.Data
{
    public static class ChecklistSeeder
    {
        public static async Task SeedIfEmptyAsync(CardListerDbContext db)
        {
            if (db.SetChecklists.Any())
                return;

            var now = DateTime.UtcNow;

            var checklists = new[]
            {
                // 2023 Panini Prizm Football
                new SetChecklist
                {
                    Manufacturer = "Panini",
                    Brand = "Prizm",
                    Year = 2023,
                    Sport = "Football",
                    TotalBaseCards = 400,
                    CachedAt = now,
                    Cards = new List<ChecklistCard>
                    {
                        new() { CardNumber = "1", PlayerName = "Patrick Mahomes", Team = "Kansas City Chiefs" },
                        new() { CardNumber = "10", PlayerName = "Josh Allen", Team = "Buffalo Bills" },
                        new() { CardNumber = "50", PlayerName = "Jalen Hurts", Team = "Philadelphia Eagles" },
                        new() { CardNumber = "75", PlayerName = "Lamar Jackson", Team = "Baltimore Ravens" },
                        new() { CardNumber = "88", PlayerName = "Justin Jefferson", Team = "Minnesota Vikings" },
                        new() { CardNumber = "100", PlayerName = "Travis Kelce", Team = "Kansas City Chiefs" },
                        new() { CardNumber = "150", PlayerName = "Tyreek Hill", Team = "Miami Dolphins" },
                        new() { CardNumber = "200", PlayerName = "Joe Burrow", Team = "Cincinnati Bengals" },
                        new() { CardNumber = "301", PlayerName = "CJ Stroud", Team = "Houston Texans", IsRookie = true, Subset = "Rookie" },
                        new() { CardNumber = "330", PlayerName = "Bryce Young", Team = "Carolina Panthers", IsRookie = true, Subset = "Rookie" }
                    },
                    KnownVariations = new List<string>
                    {
                        "Base", "Silver", "Red White Blue", "Blue /199", "Blue Shimmer /199",
                        "Red /299", "Green /75", "Orange /49", "Purple /25", "Gold /10",
                        "Gold Vinyl 1/1", "Black Finite 1/1", "Neon Green", "Pink",
                        "Tiger Stripe", "Camo", "Disco", "Snakeskin",
                        "No Huddle Silver", "No Huddle Gold /10", "Choice Blue /249"
                    }
                },

                // 2023 Panini Prizm Basketball
                new SetChecklist
                {
                    Manufacturer = "Panini",
                    Brand = "Prizm",
                    Year = 2023,
                    Sport = "Basketball",
                    TotalBaseCards = 300,
                    CachedAt = now,
                    Cards = new List<ChecklistCard>
                    {
                        new() { CardNumber = "1", PlayerName = "LeBron James", Team = "Los Angeles Lakers" },
                        new() { CardNumber = "25", PlayerName = "Stephen Curry", Team = "Golden State Warriors" },
                        new() { CardNumber = "50", PlayerName = "Giannis Antetokounmpo", Team = "Milwaukee Bucks" },
                        new() { CardNumber = "75", PlayerName = "Luka Doncic", Team = "Dallas Mavericks" },
                        new() { CardNumber = "100", PlayerName = "Kevin Durant", Team = "Phoenix Suns" },
                        new() { CardNumber = "125", PlayerName = "Jayson Tatum", Team = "Boston Celtics" },
                        new() { CardNumber = "150", PlayerName = "Nikola Jokic", Team = "Denver Nuggets" },
                        new() { CardNumber = "200", PlayerName = "Joel Embiid", Team = "Philadelphia 76ers" },
                        new() { CardNumber = "280", PlayerName = "Victor Wembanyama", Team = "San Antonio Spurs", IsRookie = true, Subset = "Rookie" },
                        new() { CardNumber = "290", PlayerName = "Brandon Miller", Team = "Charlotte Hornets", IsRookie = true, Subset = "Rookie" }
                    },
                    KnownVariations = new List<string>
                    {
                        "Base", "Silver", "Red White Blue", "Blue /199", "Blue Shimmer /199",
                        "Red /299", "Green /75", "Orange /49", "Purple /25", "Gold /10",
                        "Gold Vinyl 1/1", "Black Finite 1/1", "Neon Green", "Pink",
                        "Tiger Stripe", "Camo", "Disco", "Snakeskin",
                        "Choice Blue /249", "Choice Green /75", "Choice Gold /10"
                    }
                },

                // 2024 Topps Chrome Baseball
                new SetChecklist
                {
                    Manufacturer = "Topps",
                    Brand = "Chrome",
                    Year = 2024,
                    Sport = "Baseball",
                    TotalBaseCards = 250,
                    CachedAt = now,
                    Cards = new List<ChecklistCard>
                    {
                        new() { CardNumber = "1", PlayerName = "Shohei Ohtani", Team = "Los Angeles Dodgers" },
                        new() { CardNumber = "15", PlayerName = "Mike Trout", Team = "Los Angeles Angels" },
                        new() { CardNumber = "25", PlayerName = "Ronald Acuna Jr", Team = "Atlanta Braves" },
                        new() { CardNumber = "50", PlayerName = "Mookie Betts", Team = "Los Angeles Dodgers" },
                        new() { CardNumber = "75", PlayerName = "Aaron Judge", Team = "New York Yankees" },
                        new() { CardNumber = "100", PlayerName = "Freddie Freeman", Team = "Los Angeles Dodgers" },
                        new() { CardNumber = "125", PlayerName = "Bryce Harper", Team = "Philadelphia Phillies" },
                        new() { CardNumber = "150", PlayerName = "Juan Soto", Team = "New York Yankees" },
                        new() { CardNumber = "200", PlayerName = "Corey Seager", Team = "Texas Rangers" },
                        new() { CardNumber = "220", PlayerName = "Elly De La Cruz", Team = "Cincinnati Reds", IsRookie = true, Subset = "Rookie" }
                    },
                    KnownVariations = new List<string>
                    {
                        "Base", "Refractor", "X-Fractor", "Sepia Refractor",
                        "Pink Refractor", "Blue Refractor /150", "Green Refractor /99",
                        "Gold Refractor /50", "Orange Refractor /25",
                        "Red Refractor /5", "Superfractor 1/1",
                        "Aqua Refractor /199", "Purple Refractor /299",
                        "Prism Refractor"
                    }
                },

                // 2023 Panini Donruss Football
                new SetChecklist
                {
                    Manufacturer = "Panini",
                    Brand = "Donruss",
                    Year = 2023,
                    Sport = "Football",
                    TotalBaseCards = 350,
                    CachedAt = now,
                    Cards = new List<ChecklistCard>
                    {
                        new() { CardNumber = "1", PlayerName = "Patrick Mahomes", Team = "Kansas City Chiefs" },
                        new() { CardNumber = "25", PlayerName = "Josh Allen", Team = "Buffalo Bills" },
                        new() { CardNumber = "50", PlayerName = "Jalen Hurts", Team = "Philadelphia Eagles" },
                        new() { CardNumber = "88", PlayerName = "Justin Jefferson", Team = "Minnesota Vikings" },
                        new() { CardNumber = "100", PlayerName = "Travis Kelce", Team = "Kansas City Chiefs" },
                        new() { CardNumber = "150", PlayerName = "Tyreek Hill", Team = "Miami Dolphins" },
                        new() { CardNumber = "200", PlayerName = "Joe Burrow", Team = "Cincinnati Bengals" },
                        new() { CardNumber = "250", PlayerName = "Dak Prescott", Team = "Dallas Cowboys" },
                        new() { CardNumber = "301", PlayerName = "CJ Stroud", Team = "Houston Texans", IsRookie = true, Subset = "Rated Rookie" },
                        new() { CardNumber = "330", PlayerName = "Bryce Young", Team = "Carolina Panthers", IsRookie = true, Subset = "Rated Rookie" }
                    },
                    KnownVariations = new List<string>
                    {
                        "Base", "Rated Rookie", "Press Proof Silver", "Press Proof Blue /249",
                        "Press Proof Gold /50", "Press Proof Red /25", "Press Proof Black 1/1",
                        "Holo Red", "Holo Blue", "Holo Green", "Holo Purple"
                    }
                }
            };

            db.SetChecklists.AddRange(checklists);
            await db.SaveChangesAsync();
        }
    }
}
