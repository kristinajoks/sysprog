using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _18203Proj1
{
    public class LocalCache
    {
        private Dictionary<string, Bitmap> cache;

        public LocalCache ()
        {
            this.cache = new Dictionary<string, Bitmap> ();
        }

        public bool containReq(string request)
        {
            if(this.cache.ContainsKey(request)) return true;
            return false;
        }

        public void addReq(string request, Bitmap bmp) { 
            this.cache.TryAdd(request, bmp);
        }

        public bool tryGetValue(string request, out Bitmap value)
        {
            bool status = this.cache.TryGetValue(request, out value);
            return status;
        }

    }
}
