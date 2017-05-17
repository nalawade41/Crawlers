using SBCrawler.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SBCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            (new Processor("http://www.santabanta.com/wallpapers/categories/indian-celebrities(f)-categories/2/?order=name&page=1", "http://www.santabanta.com")).CreateCelebList();
        }
    }


}
