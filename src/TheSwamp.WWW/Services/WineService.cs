using System.Diagnostics;
using TheSwamp.WWW.Data;
using TheSwamp.WWW.Models;

namespace TheSwamp.WWW.Services
{
    public interface IWineService
    {
        Task<IReadOnlyCollection<WineDto>> Search(string term);
        Task<WineDto?> Details(long id);
        Task<long> RandomLWIN();
    }

    public class WineService : IWineService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<WineService> _logger;

        public WineService(ApplicationDbContext db, ILogger<WineService> logger)
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

        public async Task<WineDto?> Details(long id)
        {
            var sw = Stopwatch.StartNew();

            var rs = await _db.WineDetailsAsync(id);

            _logger.LogInformation("'{id}' Complete in {elapsed}", id, sw.Elapsed);

            return rs;
        }

        public async Task<long> RandomLWIN()
        {
            var sw = Stopwatch.StartNew();

            var rs = await _db.RandomLWINIdAsync();

            _logger.LogInformation("'{id}' Complete in {elapsed}", rs, sw.Elapsed);

            return rs;
        }
    }
}
