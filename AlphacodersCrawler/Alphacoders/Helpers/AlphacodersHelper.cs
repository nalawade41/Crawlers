using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alphacoders.Helpers
{
    class AlphacodersHelper : IDisposable
    {
        public string AlphacodersBaseURL { get; set; }
        public string CategoryBaseURL { get; set; }
        public string RootFolderPath { get; set; }
        public Dictionary<string, List<Tuple<string, string>>> DataToProcess { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(disposing)
            {

            }
        }
    }
}
