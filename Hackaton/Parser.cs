using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using Newtonsoft.Json;

namespace Parser
{
    class CDate
    {
        public string date;
        public string departureTime;
        public string arrivalTime;
    }
    class TourInfo
    {
        // class for storing parsed data
        public string TourOperator;
        public string City;
        public string DestinationCity;
        public string DepartureAirport;
        public string ArrivalAirport;
        public CDate Date;
        public string TypeOfFlight;
        public string FlightCompany;
        public string FlightNum;
        public string TypeOfPlane;
        public int NumOfSeats;

        public TourInfo()
        { }
    }
    class CParser
    {
        public static void ReadICS()
        {
            ChromeDr chromeDr = ChromeDr.getInstance();
            chromeDr.Driver.Navigate().GoToUrl("https://www.icstrvl.ru/flights/index.html");


            List<string> href = (from item in chromeDr.Driver.FindElementsByClassName("countries_list")
                                 from hr in item.FindElements(By.TagName("a"))
                                 where !string.IsNullOrEmpty(hr.GetAttribute("href")) 
                                        &&
                                        hr.GetAttribute("href").Length > 7
                                 let formated = hr.GetAttribute("href").Substring(5)
                                 select formated/*.Remove(formated.Length - 2)*/).Take(1).ToList();

            List<Task> tasks = new List<Task>();
            foreach (string item in href)
            {
                chromeDr.Driver.Navigate().GoToUrl("https" + item);
                List<IWebElement> tables = chromeDr.Driver.FindElementsByClassName("bordered").ToList();
                if(!tables.Any())
                {
                    continue;
                }

                string Operator = "ICS Travel Group";

                List<TourInfo> data = (from table in tables
                                       from row in table.FindElements(By.TagName("tr"))
                                       let column = row.FindElements(By.TagName("td"))
                                       where column.Any() && column.Count() > 9
                                       select new TourInfo()
                                       {
                                           TourOperator = Operator,
                                           City = column[1].Text,
                                           Date = new CDate()
                                           {
                                               date = column[2].Text,
                                               departureTime = column[6].Text,
                                               arrivalTime = column[7].Text
                                           },
                                           FlightNum = column[3].Text,
                                           FlightCompany = column[4].Text,
                                           DepartureAirport = column[8].Text,
                                           ArrivalAirport = column[9].Text

                                       }).ToList();
                string filename = item.Split('/').Last() + ".json";
                Directory.CreateDirectory(Environment.CurrentDirectory + "\\json");
                File.WriteAllText(Environment.CurrentDirectory + "\\json\\" + filename, JsonConvert.SerializeObject(data, Formatting.Indented));
            }
            chromeDr.Close();
        }

        private static IWebElement findSelector(string name, string toFind)
        {
            IWebElement selector = ChromeDr.getInstance().Driver.FindElements(By.Name(name))[0];
            while(!selector.Enabled)
            {
                Thread.Sleep(100);
                continue;
            }
            
            List<IWebElement> options = selector.FindElements(By.TagName("option")).ToList();
            
            if (!options.Any())
            {
                return null;
            }
            return options.Find(x => x.Text == toFind);
        }

        public static int ParsePegasus(string deparCountry, string arrCountry, DateTime startDate, DateTime endDate, string dCity, string aCity)
        {
            if(dCity.Length > 0)
            {
                InitPegasus(deparCountry, arrCountry, startDate, endDate, dCity, aCity);
                ChromeDr.getInstance().Close();
                return 0;
            }
            ChromeDr chromeDr = ChromeDr.getInstance();
            chromeDr.dataInitialized = false;
            chromeDr.Driver.Navigate().GoToUrl("https://pegast.ru/agency/pegasys/flights");

            List<IWebElement> cities = chromeDr.Driver.FindElements(By.Name("departureCity"))[0].FindElements(By.TagName("option")).ToList();
            cities.RemoveAt(0);
            var citiesName = (from city in cities
                              select city.Text).ToList();
            foreach (var city in citiesName)
            {
                chromeDr.dataInitialized = false;

                chromeDr.Driver.Navigate().GoToUrl("https://pegast.ru/agency/pegasys/flights");

                InitPegasus(deparCountry, arrCountry, startDate, endDate, city, aCity);
            }
            chromeDr.Close();
            return 0;
        }

        public static int InitPegasus(string deparCountry, string arrCountry, DateTime startDate, DateTime endDate, string dCity, string aCity)
        {
            if(arrCountry.Length == 0)
            {
                return 2;
            }

            ChromeDr chromeDr = ChromeDr.getInstance();
            chromeDr.Driver.Navigate().GoToUrl("https://pegast.ru/agency/pegasys/flights");

            //List<IWebElement> openCountryButton = chromeDr.Driver.FindElements(By.ClassName("country_button")).ToList();
            //if(!openCountryButton.Any())
            //{
            //    return 1;
            //}
            //openCountryButton[0].Click();

            // List<IWebElement> countries = chromeDr.Driver.FindElements(By.ClassName("pgs-country-menu__item")).ToList();
            //var countryButton = countries.Find(x => x.Text.Equals(deparCountry));
            //countryButton.FindElements(By.ClassName)
            // y.FindElements country-menu__sub-menu__item
            
            IWebElement option = findSelector("departureCity", dCity);
            if(option == null)
            {
                return 3;
            }
            option.Click();

            option = findSelector("destinationCountry", arrCountry);
            if (option == null)
            {
                return 4;
            }
            option.Click();

            if (aCity.Length > 0)
            {
                option = findSelector("destinationCity", aCity);
                if (option == null)
                {
                    return 5;
                }
                option.Click();
            }

            string fstartDate = startDate.ToString("dd.MM.yyyy");
            string fendDate = endDate.ToString("dd.MM.yyyy");

            if (!chromeDr.dataInitialized)
            {
                chromeDr.Driver.FindElement(By.Name("departureDateFrom")).SendKeys(fstartDate);
                chromeDr.Driver.FindElement(By.Name("departureDateTo")).SendKeys(fendDate);
                chromeDr.Driver.FindElement(By.Name("returnDateFrom")).SendKeys(fstartDate);
                chromeDr.Driver.FindElement(By.Name("returnDateTo")).SendKeys(fendDate);
                chromeDr.dataInitialized = true;
            }
            

            IWebElement mainButton = chromeDr.Driver.FindElementByClassName("main-button");
            while(!mainButton.Enabled)
            {
                continue;
            }
            mainButton.Click();

            //Thread.Sleep(1000);
            waitForPageLoadComplete(chromeDr.Driver);


            List<IWebElement> days = chromeDr.Driver.FindElementsByClassName("day-wrapper").ToList();
            ReadPegasus(arrCountry, dCity, aCity, days);
            return 0;
        }

        public static int InitPegasus(string deparCountry, string arrCountry, DateTime startDate, DateTime endDate, IWebElement dCity, string aCity)
        {
            if (arrCountry.Length == 0)
            {
                return 2;
            }

            ChromeDr chromeDr = ChromeDr.getInstance();

            dCity.Click();

            IWebElement option = findSelector("destinationCountry", arrCountry);
            if (option == null)
            {
                return 4;
            }
            option.Click();

            if (aCity.Length > 0)
            {
                option = findSelector("destinationCity", aCity);
                if (option == null)
                {
                    return 5;
                }
                option.Click();
            }

            string fstartDate = startDate.ToString("dd.MM.yyyy");
            string fendDate = endDate.ToString("dd.MM.yyyy");

            if (!chromeDr.dataInitialized)
            {
                chromeDr.Driver.FindElement(By.Name("departureDateFrom")).SendKeys(fstartDate);
                chromeDr.Driver.FindElement(By.Name("departureDateTo")).SendKeys(fendDate);
                chromeDr.Driver.FindElement(By.Name("returnDateFrom")).SendKeys(fstartDate);
                chromeDr.Driver.FindElement(By.Name("returnDateTo")).SendKeys(fendDate);
                chromeDr.dataInitialized = true;
            }


            IWebElement mainButton = chromeDr.Driver.FindElementByClassName("main-button");
            while (!mainButton.Enabled)
            {
                continue;
            }
            mainButton.Click();
            //Thread.Sleep(5000);
            waitForPageLoadComplete(chromeDr.Driver);

            List<IWebElement> days = chromeDr.Driver.FindElementsByClassName("day-wrapper").ToList();
            ReadPegasus(arrCountry, dCity.Text, aCity, days);
            return 0;
        }


        public static List<TourInfo> ReadPegasus(string aCountry, string dCity, string aCity, List<IWebElement> days)
        {
            ChromeDr chromeDr = ChromeDr.getInstance();

            
            string Operator = "Pegas Touristik";

            List<TourInfo> data = (from day in days
                                   let date = day.FindElement(By.ClassName("day-header")).Text
                                   where chromeDr.Driver.FindElements(By.ClassName("day-header")).ToList().Find(x => x.Text.Equals(date)) != null
                                   from flight in day.FindElements(By.ClassName("f-row"))
                                   let departureItem = flight.FindElement(By.ClassName("departure-item")).Text
                                   let returnItem = flight.FindElement(By.ClassName("return-item")).Text
                                   let avia = flight.FindElement(By.ClassName("avia")).Text
                                   select new TourInfo()
                                   {
                                       TourOperator = Operator,
                                       City = dCity,
                                       DestinationCity = aCity,
                                       Date = new CDate()
                                       {
                                           date = date,
                                           departureTime = departureItem.Remove(departureItem.LastIndexOf("\r")),
                                           arrivalTime = returnItem.Remove(returnItem.LastIndexOf("\r"))
                                       },
                                       FlightNum = flight.FindElement(By.ClassName("flight")).Text,
                                       FlightCompany = avia.Remove(avia.LastIndexOf("\r")),
                                       TypeOfPlane = avia.Substring(avia.LastIndexOf("\n") + 1),
                                       DepartureAirport = departureItem.Substring(departureItem.LastIndexOf("\n") + 1),
                                       ArrivalAirport = returnItem.Substring(returnItem.LastIndexOf("\n") + 1),
                                   }).ToList();
            days.Clear();

            string filename = Environment.CurrentDirectory +"\\json\\" + dCity + aCountry + aCity + ".json";
            File.WriteAllText(filename, JsonConvert.SerializeObject(data, Formatting.Indented));
            return data;
        }

        public static void Write(List<TourInfo> data, string filename)
        { 
            File.WriteAllText(filename, JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        private static void waitForPageLoadComplete(ChromeDriver driver)
        {
            WebDriverWait wait = new WebDriverWait(driver, new TimeSpan(0, 1, 0));

            if (driver.ExecuteScript("return document.readyState").Equals("complete"))
                return;
        }
    }

    class ChromeDr
    {
        public ChromeDriver Driver;
        public bool dataInitialized;
        private ChromeOptions options;

        private static ChromeDr instance;

        private ChromeDr()
        {
            dataInitialized = false;
            options = new ChromeOptions();
            options.AddArguments("--disk-cache-size=2000", "--incognito");
            Driver = new ChromeDriver(options);
        }

        public static ChromeDr getInstance()
        {
            if (instance == null)
                instance = new ChromeDr();
            return instance;
        }

        public void Close()
        {
            Driver.Dispose();
        }
        public void Open()
        {
            Driver = new ChromeDriver(options);
        }
    }

    


}
