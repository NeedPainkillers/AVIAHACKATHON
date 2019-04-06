using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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
        public string DepartureAirport;
        public string ArrivalAirport;
        public CDate Date;
        public string TypeOfFlight;
        public string FlightCompany;
        public string FlightNum;
        public string TypeOfPlane;
        public int NumOfASeats;

        public TourInfo()
        { }
    }
    class CParser
    {
        public static void ReadICS()
        {
            ChromeDr chromeDr = ChromeDr.getInstance();
            chromeDr.ChDr.Navigate().GoToUrl("https://www.icstrvl.ru/flights/index.html");


            List<string> href = (from item in chromeDr.ChDr.FindElementsByClassName("countries_list")
                                 from hr in item.FindElements(By.TagName("a"))
                                 where !string.IsNullOrEmpty(hr.GetAttribute("href")) 
                                        &&
                                        hr.GetAttribute("href").Length > 7
                                 let formated = hr.GetAttribute("href").Substring(5)
                                 select formated/*.Remove(formated.Length - 2)*/).Take(1).ToList();

            List<Task> tasks = new List<Task>();
            foreach (string item in href)
            {
                chromeDr.ChDr.Navigate().GoToUrl("https" + item);
                List<IWebElement> tables = chromeDr.ChDr.FindElementsByClassName("bordered").ToList();
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
            List<IWebElement> options = ChromeDr.getInstance().ChDr.FindElements(By.Name(name))[0].FindElements(By.TagName("option")).ToList();
            if (!options.Any())
            {
                return null;
            }
            return options.Find(x => x.Text == toFind);
        }
        public static int ReadPegasus(string deparCountry, string arrCountry, DateTime startDate, DateTime endDate, string dCity, string aCity)
        {
            if(deparCountry.Length == 0 || arrCountry.Length == 0)
            {
                return 2;
            }

            ChromeDr chromeDr = ChromeDr.getInstance();
            chromeDr.ChDr.Navigate().GoToUrl("https://pegast.ru/agency/pegasys/flights");

            //List<IWebElement> openCountryButton = chromeDr.ChDr.FindElements(By.ClassName("country_button")).ToList();
            //if(!openCountryButton.Any())
            //{
            //    return 1;
            //}
            //openCountryButton[0].Click();

            // List<IWebElement> countries = chromeDr.ChDr.FindElements(By.ClassName("pgs-country-menu__item")).ToList();
            //var countryButton = countries.Find(x => x.Text.Equals(deparCountry));
            //countryButton.FindElements(By.ClassName)
            // y.FindElements country-menu__sub-menu__item
            
            IWebElement option = findSelector("departureCity", dCity);
            if(option == null)
            {
                return 3;
            }
            option.Click();
            Thread.Sleep(2500);

            option = findSelector("destinationCountry", arrCountry);
            if (option == null)
            {
                return 4;
            }
            option.Click();
            Thread.Sleep(1000);

            if (aCity.Length > 0)
            {
                option = findSelector("destinationCity", aCity);
                if (option == null)
                {
                    return 5;
                }
                option.Click();
            }
            Thread.Sleep(1000);

            string fstartDate = startDate.ToString("dd.MM.yyyy");
            string fendDate = endDate.ToString("dd.MM.yyyy");

            chromeDr.ChDr.FindElement(By.Name("departureDateFrom")).SendKeys(fstartDate);
            chromeDr.ChDr.FindElement(By.Name("departureDateTo")).SendKeys(fendDate);
            chromeDr.ChDr.FindElement(By.Name("returnDateFrom")).SendKeys(fstartDate);
            chromeDr.ChDr.FindElement(By.Name("returnDateTo")).SendKeys(fendDate);
            //main-button
            chromeDr.ChDr.FindElementByClassName("main-button").Click();

            return 0;
        }

        public static void Write(List<TourInfo> data, string filename)
        { 
            File.WriteAllText(filename, JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }

    class ChromeDr
    {
        public ChromeDriver ChDr;
        private ChromeOptions options;

        private static ChromeDr instance;

        private ChromeDr()
        {
            options = new ChromeOptions();
            options.AddArguments("--disk-cache-size=1", "--incognito");
            ChDr = new ChromeDriver(options);
        }

        public static ChromeDr getInstance()
        {
            if (instance == null)
                instance = new ChromeDr();
            return instance;
        }

        public void Close()
        {
            ChDr.Dispose();
        }
        public void Open()
        {
            ChDr = new ChromeDriver(options);
        }
    }

    
}
