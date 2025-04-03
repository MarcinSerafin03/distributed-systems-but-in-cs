using System.Text;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Lab2_Rest;

[ApiController]
[Route("api/holidays/fetch")]
public class HolidayController : ControllerBase
{
    private class SvatkyItem
    {
        public string name { get; set; }
        public string date { get; set; }
    }

    private class HolidayItem
    {
        public int month { get; set; }
        public int day { get; set; }
        public int year { get; set; }
        public string name { get; set; }
    }

    private readonly HttpClient _httpClient;
    private readonly string _year = DateTime.Now.Year.ToString();

    public HolidayController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    [HttpGet("name")]
    public async Task<IActionResult> FetchHolidaysByName([FromQuery] string name, [FromQuery] bool json)
    {
        if (string.IsNullOrEmpty(name))
        {
            return BadRequest("Invalid parameters");
        }
        
        var svatkyUrl = $"https://svatky.adresa.info/json?name={name}";
        var svatkyResponse = await _httpClient.GetStringAsync(svatkyUrl);
        var parsedSvatky = JsonSerializer.Deserialize<List<SvatkyItem>>(svatkyResponse) ?? new List<SvatkyItem>();

        var holidayResponses = new List<List<HolidayItem>>();

        foreach (var item in parsedSvatky)
        {
            if (item.date.Length != 4) continue;
            string day = item.date.Substring(0, 2);
            string month = item.date.Substring(2, 2);
            month = int.Parse(month).ToString();

            var abstractApiUrl = $"https://pniedzwiedzinski.github.io/kalendarz-swiat-nietypowych/{month}/{day}.json";
            try
            {
                var abstractApiResponse = await _httpClient.GetStringAsync(abstractApiUrl);
                var holidays = JsonSerializer.Deserialize<List<HolidayItem>>(abstractApiResponse) ?? new List<HolidayItem>();
                holidayResponses.Add(holidays);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching holidays: {ex.Message}");
                holidayResponses.Add(new List<HolidayItem> { new HolidayItem { name = "No holidays found for this date." } });
            }
        }

        if (json)
        {
            var combinedResponse = new
            {
                Svatky = parsedSvatky,
                AbstractApi = holidayResponses
            };
            return Ok(combinedResponse);
        } 
        
        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append("<html><head><title>Holidays</title></head><body>");
        htmlBuilder.Append("<h1>Results for Name Search</h1>");

        if (parsedSvatky.Count == 0)
        {
            htmlBuilder.Append("<p>No name days found.</p>");
        }
        else
        {
            htmlBuilder.Append("<h2>Imieniny & Dziwne Święta:</h2>");
            htmlBuilder.Append(
                "<table border='1'><tr><th>Name</th><th>Day</th><th>Month</th><th>Holiday</th></tr>");

            for (int i = 0; i < parsedSvatky.Count; i++)
            {
                string day = parsedSvatky[i].date.Substring(0, 2);
                string month = parsedSvatky[i].date.Substring(2, 2);

                if (holidayResponses[i].Count > 0)
                {
                    foreach (var holiday in holidayResponses[i])
                    {
                        htmlBuilder.Append(
                            $"<tr><td>{parsedSvatky[i].name}</td><td>{day}</td><td>{month}</td><td>{holiday.name}</td></tr>");
                    }
                }
                else
                {
                    htmlBuilder.Append(
                        $"<tr><td>{parsedSvatky[i].name}</td><td>{day}</td><td>{month}</td><td>No holidays found</td></tr>");
                }
            }

            htmlBuilder.Append("</table>");
        }

        htmlBuilder.Append("</body></html>");
        return Content(htmlBuilder.ToString(), "text/html");
        
    }

    [HttpGet("date")]
    public async Task<IActionResult> FetchHolidaysByDate(
        [FromQuery] int month, [FromQuery] int day, [FromQuery] bool json)
    {   
        if (month <= 0 || day <= 0)
        {
            return BadRequest("Invalid parameters");
        }
        string monthString = month.ToString();
        if (month < 10)
        {
            monthString = "0" + month;
        }

        string svatkyUrl = $"https://svatky.adresa.info/json?date={day}{monthString}";
        string abstractApiUrl = $"https://pniedzwiedzinski.github.io/kalendarz-swiat-nietypowych/{month}/{day}.json";

        var svatkyResponse = await _httpClient.GetStringAsync(svatkyUrl);
        var abstractApiResponse = await _httpClient.GetStringAsync(abstractApiUrl);

        var parsedSvatky = JsonSerializer.Deserialize<List<SvatkyItem>>(svatkyResponse) ?? new List<SvatkyItem>();
        var holidayResponses = new List<HolidayItem>();

        foreach (var item in parsedSvatky)
        {
            if (item.date.Length != 4)
            {
                continue;
            }

            string dayStr = item.date.Substring(0, 2);
            string monthStr = item.date.Substring(2, 2);
            monthStr = int.Parse(monthStr).ToString();

            var parsedAbstractApi = JsonSerializer.Deserialize<List<HolidayItem>>(abstractApiResponse) ??
                                    new List<HolidayItem>();

            foreach (var holiday in parsedAbstractApi)
            {
                holidayResponses.Add(new HolidayItem
                {
                    month = holiday.month,
                    day = holiday.day,
                    year = holiday.year,
                    name = holiday.name
                });
            }
        }

        if (json)
        {
            var combinedResponse = new
            {
                Svatky = parsedSvatky,
                AbstractApi = holidayResponses
            };
            Console.WriteLine(JsonSerializer.Serialize(combinedResponse));
            return Ok(combinedResponse);
        }

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append("<html><head><title>Holidays</title></head><body>");
        htmlBuilder.Append("<h1>Results for Date Search</h1>");

        if (parsedSvatky.Count == 0)
        {
            htmlBuilder.Append("<p>No name days found.</p>");
        }
        else
        {
            htmlBuilder.Append("<h2>Imieniny & Dziwne Święta:</h2>");
            htmlBuilder.Append("<table border='1'><tr><th>Name</th><th>Day</th><th>Month</th><th>Holiday</th></tr>");

            foreach (var item in parsedSvatky)
            {

                if (holidayResponses.Count > 0)
                {
                    foreach (var holiday in holidayResponses)
                    {
                        htmlBuilder.Append(
                            $"<tr><td>{item.name}</td><td>{day}</td><td>{month}</td><td>{holiday.name}</td></tr>");
                    }
                }
                else
                {
                    htmlBuilder.Append(
                        $"<tr><td>{item.name}</td><td>{day}</td><td>{month}</td><td>No holidays found</td></tr>");
                }
            }

            htmlBuilder.Append("</table>");
        }

        htmlBuilder.Append("</body></html>");
        return Content(htmlBuilder.ToString(), "text/html");
    }
}

