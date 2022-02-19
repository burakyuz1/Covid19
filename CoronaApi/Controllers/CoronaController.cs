using CoronaApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CoronaApi.Controllers
{
    public class CoronaController : Controller
    {
        private readonly IMemoryCache _memoryCache;

        public CoronaController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }
        public async Task<IActionResult> AllCountries()
        {
            List<CoronaViewModel> cases = new List<CoronaViewModel>();


            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://covid-api.mmediagroup.fr");

                HttpResponseMessage request;

                if (!_memoryCache.TryGetValue("request", out request))
                {
                    request = await client.GetAsync("/v1/cases");
                    _memoryCache.Set("request", request, DateTimeOffset.Now.AddDays(1));
                }
                if (request.IsSuccessStatusCode)
                {
                    var jsonData = await request.Content.ReadAsStringAsync();

                    dynamic netDatas = JsonSerializer.Deserialize<ExpandoObject>(jsonData);
                    foreach (KeyValuePair<string, object> data in netDatas) //data= dictionary
                    {
                        string countryName = data.Key;
                        int @case = (int)(data.Value as dynamic).GetProperty("All").GetProperty("confirmed").GetDecimal();
                        int death = (int)(data.Value as dynamic).GetProperty("All").GetProperty("deaths").GetDecimal();

                        cases.Add(new CoronaViewModel()
                        {
                            CountryName = countryName,
                            TotalCases = @case,
                            TotalDeaths = death
                        });


                    }

                    return View(cases.OrderByDescending(x => x.TotalDeaths).ToList());
                }
                else
                {
                    return BadRequest();

                }
            }

        }

        public async Task<IActionResult> CountryCases()
        {
            List<SelectListItem> countries = new List<SelectListItem>();
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://covid-api.mmediagroup.fr");
                var request = await client.GetAsync("/v1/cases");
                if (request.IsSuccessStatusCode)
                {
                    var jsonData = await request.Content.ReadAsStringAsync();

                    dynamic netData = JsonSerializer.Deserialize<ExpandoObject>(jsonData);
                    foreach (var item in netData)
                    {
                        countries.Add(new SelectListItem() { Text = item.Key, Value = item.Key });
                    }
                }
            }
            return View(countries);
        }



        [HttpPost]
        public async Task<IActionResult> CountryCases(string cn, DateTime dt) //normal
        {
            string formettedDateTime = dt.ToString("yyyy-MM-dd");
            string previousDay = dt.AddDays(-1).ToString("yyyy-MM-dd");

            if (string.IsNullOrEmpty(cn))
            {
                return BadRequest();
            }
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://covid-api.mmediagroup.fr");
                HttpResponseMessage requestConfirmed;
                HttpResponseMessage requestDeath;
                bool isConfirmExist = _memoryCache.TryGetValue("confirmed", out requestConfirmed);
                bool isDeathExist = _memoryCache.TryGetValue("death", out requestDeath);
                if (!isConfirmExist && !isDeathExist)
                {
                    requestConfirmed = await client.GetAsync($"v1/history?country={cn}&status=confirmed");
                    requestDeath = await client.GetAsync($"v1/history?country={cn}&status=deaths");
                    _memoryCache.Set("confirmed", requestConfirmed, DateTimeOffset.Now.AddDays(1));
                    _memoryCache.Set("death", requestDeath, DateTimeOffset.Now.AddDays(1));
                }

                if (requestConfirmed.IsSuccessStatusCode && requestDeath.IsSuccessStatusCode)
                {
                    var jsonDataConfirmed = await requestConfirmed.Content.ReadAsStringAsync();
                    var jsonDataDeaths = await requestDeath.Content.ReadAsStringAsync();

                    dynamic netDataConfirmed = JsonSerializer.Deserialize<ExpandoObject>(jsonDataConfirmed);
                    dynamic netDataDeaths = JsonSerializer.Deserialize<ExpandoObject>(jsonDataDeaths);


                    int population = (int)netDataConfirmed.All.GetProperty("population").GetDecimal();

                    int currentDeath = (int)netDataDeaths.All.GetProperty("dates").GetProperty(formettedDateTime).GetDecimal();
                    int previousDeath = (int)netDataDeaths.All.GetProperty("dates").GetProperty(previousDay).GetDecimal();
                    int dailyDeath = currentDeath - previousDeath;

                    int currentCase = (int)netDataConfirmed.All.GetProperty("dates").GetProperty(formettedDateTime).GetDecimal();
                    int previousCase = (int)netDataConfirmed.All.GetProperty("dates").GetProperty(previousDay).GetDecimal();
                    int dailyCase = currentCase - previousCase;

                    CountryCaseViewModel vm = new CountryCaseViewModel()
                    {
                        CountryName = cn,
                        Death = dailyDeath,
                        Confirmed = dailyCase,
                        Population = population
                    };

                    return PartialView("_CountryCasesPartial", vm);
                }

            }


            return View();
        }
    }
}
