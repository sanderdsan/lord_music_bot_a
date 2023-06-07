using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net;
using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Services;
public class Event_by_Genre : ControllerBase
    {
        [HttpGet("genre/{genreName}")]
        public async Task<IActionResult> GetEventsByGenre(string genreName)
        {
            string slug = genreName.Replace(" ", "-");

            string url = $"https://api.seatgeek.com/2/performers?genres.slug={slug}&client_id=MzM5OTE4NTF8MTY4NTQzODQ5OC41ODE1Nzgz";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Error retrieving events from SeatGeek API");
                }

                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
        }
    }
    [ApiController]
    [Route("api/[controller]")]
    public class Event_by_Location : ControllerBase
    {
        [HttpGet("city/{location}")]
        public async Task<IActionResult> GetEventsByGenre(string location)
        {
            string clientId = "MzM5OTE4NTF8MTY4NTQzODQ5OC41ODE1Nzgz";

            string slug = location.Replace(" ", "-");

            string url = $"https://api.seatgeek.com/2/events?venue.city={location}&taxonomies.name=concert&client_id={clientId}";

            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "Error retrieving events from SeatGeek API");
                }

                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }
        }
    }

[Route("api/superfinder/[controller]")]

public class Event_by_Artist : ControllerBase
{
    [HttpGet("artist/{artistName}")]

    public async Task<IActionResult> GetEventsByArtist(string artistName)
    {

        string url = $"https://rest.bandsintown.com/artists/{artistName}/events?app_id=4ecdf4565c2459804a07ced326512723";
        string response = await GetApiResponse(url);

        if (!string.IsNullOrEmpty(response))
        {
            return Content(response, "application/json");
        }
        return StatusCode((int)HttpStatusCode.NotFound, "No events found");
    }

    private async Task<string> GetApiResponse(string url)
    {
        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsStringAsync();
        }
    }
}
[ApiController]
[Route("api/[controller]")]
public class EventController : ControllerBase
{
    private const string GoogleClientId = "635943519656-2v3tguccpfp94m9e6i08p3dkm85gn8cs.apps.googleusercontent.com";
    private const string GoogleClientSecret = "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";
    private const string GoogleSpreadsheetId = "1OUpf4eeM3iF1Rkm7EhP5EyXKOBsbkXVR6kzdGAD3N-E";
    private const string Range = "Storegen!A:B";

    [HttpGet("genre/{genreId}")]
    public async Task<IActionResult> GetGenreById(int genreId)
    {
        UserCredential credential = await Login();
        GoogleSheetManager manager = new GoogleSheetManager(credential);

        List<IList<object>> data = manager.GetSheetData(GoogleSpreadsheetId, Range);

        foreach (var row in data)
        {
            if (row.Count >= 2 && row[0].ToString() == genreId.ToString())
            {
                return Ok(row[1].ToString());
            }
        }

        return NotFound("Genre not found");
    }

    [HttpPost("login")]
    private async Task<UserCredential> Login()
    {
        string[] scopes = new[] { SheetsService.Scope.Spreadsheets };
        UserCredential credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            new ClientSecrets
            {
                ClientId = GoogleClientId,
                ClientSecret = GoogleClientSecret
            },
            scopes,
            "user",
            CancellationToken.None);

        return credential;
    }

    [HttpPost("insert")]
    public async Task<IActionResult> InsertData([FromBody] InsertDataRequest request)
    {
        UserCredential credential = await Login();
        GoogleSheetManager manager = new GoogleSheetManager(credential);

        manager.InsertData(GoogleSpreadsheetId, request.ChatID, request.Genres);

        return Ok("Data inserted successfully.");
    }

    [HttpPost("update")]
    public IActionResult UpdateGenres([FromBody] UpdateGenresRequest request)
    {
        try
        {
            UserCredential credential = Login().Result;
            GoogleSheetManager manager = new GoogleSheetManager(credential);

            manager.UpdateData(GoogleSpreadsheetId, request.ChatId, request.NewGenres);

            return Ok("Genres updated successfully.");
        }
        catch (Exception ex)
        {
            return StatusCode((int)HttpStatusCode.InternalServerError, ex.Message);
        }
    }
}
[Route("api/base/[controller]")]
public class GenresController : ControllerBase
{
    private const string GoogleSpreadsheetId = "1OUpf4eeM3iF1Rkm7EhP5EyXKOBsbkXVR6kzdGAD3N-E";
    private const string Range = "Storegen!A:B";

    [HttpGet]
    public async Task<IActionResult> GetGenres()
    {
        try
        {
            var service = await GetSheetsService();

            var request = service.Spreadsheets.Values.Get(GoogleSpreadsheetId, Range);
            var response = await request.ExecuteAsync();

            var values = response.Values;

            if (values != null && values.Count > 0)
            {
                var genresList = new List<Genre>();

                foreach (var row in values)
                {
                    var genre = new Genre
                    {
                        chat_id = int.Parse(row[0].ToString()),
                        genres = row[1].ToString()
                    };

                    genresList.Add(genre);
                }

                return Ok(genresList);
            }
            else
            {
                return NotFound("No genres found.");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    public class Genre
    {
        public int chat_id { get; set; }
        public string genres { get; set; }
    }

    private async Task<SheetsService> GetSheetsService()
    {
        var initializer = new BaseClientService.Initializer
        {
            ApiKey = "AIzaSyC1Xbb-xlYKJhsbJ3nYUqhNmTm-MFxaA5Y",
            ApplicationName = "Storegen"
        };

        return new SheetsService(initializer);
    }
}

[ApiController]
[Route("api/checker/[controller]")]
public class Genresfinder : ControllerBase
{
    private const string GoogleSpreadsheetId = "1OUpf4eeM3iF1Rkm7EhP5EyXKOBsbkXVR6kzdGAD3N-E";
    private const string Range = "Storegen!A:B";

    [HttpGet("{chatId}")]
    public async Task<IActionResult> GetGenresByChatId(int chatId)
    {
        try
        {
            var service = await GetSheetsService();

            var request = service.Spreadsheets.Values.Get(GoogleSpreadsheetId, Range);
            var response = await request.ExecuteAsync();

            var values = response.Values;

            if (values != null && values.Count > 0)
            {
                foreach (var row in values)
                {
                    var rowChatId = int.Parse(row[0].ToString());
                    if (rowChatId == chatId)
                    {
                        var genres = row[1].ToString();
                        return Ok(genres);
                    }
                }

                return NotFound($"No genres found for chat ID: {chatId}");
            }
            else
            {
                return NotFound($"No genres found for chat ID: {chatId}");
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    private async Task<SheetsService> GetSheetsService()
    {
        var initializer = new BaseClientService.Initializer
        {
            ApiKey = "AIzaSyC1Xbb-xlYKJhsbJ3nYUqhNmTm-MFxaA5Y",
            ApplicationName = "Storegen"
        };

        return new SheetsService(initializer);
    }
}

[ApiController]
[Route("api/superfinder/[controller]")]
public class RecommendationController : ControllerBase
{
    private const string SpotifyClientId = "48f9cc3719624a9da8cce71daf020f0d";
    private const string SpotifyClientSecret = "5b45f39aaa2b4bcd866ad1afa15cb6f3";
    private const string GoogleSpreadsheetId = "1OUpf4eeM3iF1Rkm7EhP5EyXKOBsbkXVR6kzdGAD3N-E";
    private const string Range = "Storegen!A:B";

    [HttpPost]
    public async Task<IActionResult> GetRecommendation([FromBody] RecommendationRequest request)
    {
        try
        {
            string selectedGenre = await GetRandomGenreByChatId(request.ChatId);
            if (selectedGenre == null)
            {
                return NotFound($"No genres found for chat ID: {request.ChatId}");
            }

            string recommendation = await GetMusicRecommendation(selectedGenre);

            var response = new RecommendationResponse
            {
                Recommendation = recommendation
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    private async Task<string> GetRandomGenreByChatId(int chatId)
    {
        var service = await GetSheetsService();

        var request = service.Spreadsheets.Values.Get(GoogleSpreadsheetId, Range);
        var response = await request.ExecuteAsync();

        var values = response.Values;

        if (values != null && values.Count > 0)
        {
            foreach (var row in values)
            {
                var rowChatId = int.Parse(row[0].ToString());
                if (rowChatId == chatId)
                {
                    string genres = row[1].ToString();
                    List<string> genreList = genres.Split(';').ToList();
                    if (genreList.Count > 0)
                    {
                        Random random = new Random();
                        int index = random.Next(genreList.Count);
                        return genreList[index];
                    }
                }
            }
        }

        return null;
    }

    private async Task<string> GetMusicRecommendation(string genre)
    {
        var config = SpotifyClientConfig.CreateDefault();
        var request = new ClientCredentialsRequest(SpotifyClientId, SpotifyClientSecret);
        var response = await new OAuthClient(config).RequestToken(request);

        var spotifyClient = new SpotifyClient(config.WithToken(response.AccessToken));

        var searchRequest = new SearchRequest(SearchRequest.Types.Playlist, genre);
        var searchResults = await spotifyClient.Search.Item(searchRequest);

        if (searchResults.Playlists.Items.Count > 0)
        {
            var playlistId = searchResults.Playlists.Items[0].Id;

            var playlist = await spotifyClient.Playlists.Get(playlistId);
            if (playlist.Tracks.Items.Count > 0)
            {
                Random random = new Random();
                int index = random.Next(playlist.Tracks.Items.Count);
                var track = playlist.Tracks.Items[index].Track as FullTrack;

                var songUrl = track.ExternalUrls["spotify"];

                return $"{track.Name} by {string.Join(", ", track.Artists.Select(a => a.Name))}\nSpotify link: {songUrl}";
            }
        }

        return "No recommendation found.";
    }


    private async Task<SheetsService> GetSheetsService()
    {
        var initializer = new BaseClientService.Initializer
        {
            ApiKey = "AIzaSyC1Xbb-xlYKJhsbJ3nYUqhNmTm-MFxaA5Y",
            ApplicationName = "Storegen"
        };

        return new SheetsService(initializer);
    }
}

public class RecommendationRequest
{
    public int ChatId { get; set; }
}

public class RecommendationResponse
{
    public string Recommendation { get; set; }
}
public class GoogleSheetManager
{
    private readonly SheetsService _sheetsService;

    public GoogleSheetManager(UserCredential credential)
    {
        _sheetsService = new SheetsService(new BaseClientService.Initializer()
        {
            HttpClientInitializer = credential
        });
    }

    public List<IList<object>> GetSheetData(string spreadsheetId, string range)
    {
        SpreadsheetsResource.ValuesResource.GetRequest request =
            _sheetsService.Spreadsheets.Values.Get(spreadsheetId, range);

        ValueRange response = request.Execute();
        return response.Values.ToList();
    }

    public void InsertData(string spreadsheetId, string chatId, List<string> genres)
    {
        List<object> row = new List<object> { chatId };
        row.AddRange(genres.Select(genre => (object)genre));

        ValueRange valueRange = new ValueRange
        {
            Values = new List<IList<object>> { row }
        };

        SpreadsheetsResource.ValuesResource.AppendRequest request =
            _sheetsService.Spreadsheets.Values.Append(valueRange, spreadsheetId, "Storegen");

        request.ValueInputOption = SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED;

        request.Execute();
    }

    public void UpdateData(string spreadsheetId, string chatId, List<string> newGenres)
    {
        List<IList<object>> sheetData = GetSheetData(spreadsheetId, "Storegen!A:B");

        for (int i = 0; i < sheetData.Count; i++)
        {
            var row = sheetData[i];

            if (row.Count > 0 && row[0].ToString() == chatId)
            {
                row[1] = newGenres[0]; // Update the value in column B

                var range = $"Storegen!B{i + 1}";

                var valueRange = new ValueRange
                {
                    Values = new List<IList<object>> { new List<object> { row[1] } }
                };

                var request = _sheetsService.Spreadsheets.Values.Update(valueRange, spreadsheetId, range);

                request.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;

                request.Execute();
                break;
            }
        }
    }
}

public class InsertDataRequest
{
    [JsonProperty("chat_id")]
    public string ChatID { get; set; }

    [JsonProperty("genres")]
    public List<string> Genres { get; set; }
}

public class UpdateGenresRequest
{
    [JsonProperty("chat_id")]
    public string ChatId { get; set; }

    [JsonProperty("new_genres")]
    public List<string> NewGenres { get; set; }
}
