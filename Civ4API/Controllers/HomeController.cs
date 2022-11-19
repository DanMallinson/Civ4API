using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Civ4API.Controllers
{
    public class HomeController : Controller
    {
        public string Index()
        {
            var value =System.Text.Json.JsonSerializer.Serialize(Worker.Scrape().Item1, typeof(Dictionary<string, string>));
            return value;
        }
    }
}
