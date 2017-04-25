using StoreClouding.Aspects.Performance;
using StoreClouding.Aspects.Tests.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Tests.DAL
{
    public static class User
    {
        [StaticMemoryCache(60 * 1000, IgnoreValue = null)]
        public static Model.User GetByID(int id)
        {
            return null;
        }

        public static Model.User Save(this Model.User user)
        {
            return null;
        }
    }
}
