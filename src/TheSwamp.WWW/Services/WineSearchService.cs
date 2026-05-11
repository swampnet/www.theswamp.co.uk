using System.Diagnostics;
using TheSwamp.WWW.Data;
using TheSwamp.WWW.Models;

namespace TheSwamp.WWW.Services
{
    public interface IWineSearchService
    {
        Task<IReadOnlyCollection<WineDto>> Search(string term);
    }

    public class WineSearchService : IWineSearchService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WineSearchService> _logger;

        public WineSearchService(ApplicationDbContext db, ILogger<WineSearchService> logger)
        {
            _db = db;
            _logger = logger;
        }


        public async Task<IReadOnlyCollection<WineDto>> Search(string term)
        {
            var sw = Stopwatch.StartNew();

            var rs = await _db.SearchWineAsync(term);

            _logger.LogInformation("'{term}' Complete in {elapsed}", term, sw.Elapsed);

            return rs;
        }
    }
}
