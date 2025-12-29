using Microsoft.AspNetCore.Mvc;
using StackExchange.Redis;
using LinkShortener.Utils;

namespace LinkShortener.Controllers
{
    [ApiController]
    [Route("")]
    public class ShortenerController : ControllerBase
    {
        private readonly IDatabase _db;
        private const string CounterKey = "ids:link";

        public ShortenerController(IConnectionMultiplexer mux)
        {
            Console.WriteLine("[CONTROLLER] ShortenerController constructor called");
            if (mux == null)
            {
                Console.Error.WriteLine("[CONTROLLER] ERROR: IConnectionMultiplexer is null!");
                throw new ArgumentNullException(nameof(mux));
            }
            _db = mux.GetDatabase();
            Console.WriteLine("[CONTROLLER] Database connection obtained successfully");
        }

        [HttpGet("{code}", Name = "GetShortenedLink")]
        public IActionResult Get(string code)
        {
            Console.WriteLine($"[GET] Received request for code: {code}");
            if (string.IsNullOrWhiteSpace(code))
                return BadRequest("Code is required.");

            var originalLink = _db.StringGet(code);
            if (originalLink.HasValue)
            {
                Console.WriteLine($"[GET] Found link, redirecting to: {originalLink}");
                return Redirect(originalLink.ToString());
            }

            Console.WriteLine($"[GET] Code not found: {code}");
            return NotFound();
        }

        [HttpPost("Shorten", Name = "Shorten")]
        public IActionResult Post([FromBody] string originalLink)
        {
            Console.WriteLine($"[POST] Received link to shorten: {originalLink}");
            if (string.IsNullOrWhiteSpace(originalLink))
                return BadRequest("Original link is required in the request body.");

            if (!Uri.TryCreate(originalLink, UriKind.Absolute, out var uriResult) || (uriResult.Scheme != Uri.UriSchemeHttp && uriResult.Scheme != Uri.UriSchemeHttps))
            {
                return BadRequest("Invalid URL format. Include scheme (http or https).");
            }

            // Atomically increment counter and encode to Base62
            var id = _db.StringIncrement(CounterKey);
            var code = Base62.Encode((long)id);

            // In rare case of collision (shouldn't happen) ensure uniqueness
            var setOk = _db.StringSet(code, originalLink, when: When.NotExists);
            if (!setOk)
            {
                // If collision, retry generation a few times
                for (int i = 0; i < 5 && !setOk; i++)
                {
                    id = _db.StringIncrement(CounterKey);
                    code = Base62.Encode((long)id);
                    setOk = _db.StringSet(code, originalLink, when: When.NotExists);
                }

                if (!setOk)
                    return StatusCode(500, "Could not generate unique code, please try again.");
            }

            var shortenedLink = $"http://short.ly/{code}";
            Console.WriteLine($"[POST] Generated short code: {code}");
            return Created(shortenedLink, new { OriginalLink = originalLink, ShortenedLink = shortenedLink, Code = code });
        }
    }
}
