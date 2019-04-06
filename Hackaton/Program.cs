using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;


namespace Parser
{
    class WorkSpace
    {
        static void Main(string[] args)
        {
            CParser.ReadPegasus("Россия", "Азербайджан", DateTime.Now, DateTime.Now, "Москва", "Баку");
        }
    }
}
