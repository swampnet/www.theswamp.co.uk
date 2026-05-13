using Microsoft.AspNetCore.Mvc;
using TheSwamp.WWW.Models;
using TheSwamp.WWW.Services;

namespace TheSwamp.WWW.Api;

[ApiController]
[Route("api/[controller]")]
public class WineController : ControllerBase
{
    private readonly IWineService _wineService;

    public WineController(IWineService wineService)
    {
        _wineService = wineService;
    }

    /// <summary>
    /// Search LWIN wine
    /// </summary>
    /// <param name="term">Search term</param>
    /// <returns></returns>
    [HttpGet]
    public Task<IReadOnlyCollection<WineDto>> Get(string term)
    {
        return _wineService.Search(term);
    }
}
