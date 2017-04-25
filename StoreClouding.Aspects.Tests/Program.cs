using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StoreClouding.Aspects.Data;
using PostSharp.Extensibility;
using StoreClouding.Aspects.Performance;
using StoreClouding.Aspects.Tests.Model;
using StoreClouding.Aspects.Tests.DAL;
using StoreClouding.Aspects.Net;

namespace StoreClouding.Aspects.Tests
{
    class Program
    {
        static void Main(string[] args)
        {
            DBProcedure.Logger = new ConsoleLogger();
            //DBProcedure.ConnectionManager = new BasicConnectionManager();
            //var exception = DBProcedure.LastError;

            //HttpRequest.Logger = new ConsoleLogger();
            //var exception = HttpRequest.LastError;

            //StaticMemoryCache.ClearExpiredCache();
            //StaticMemoryCache.ClearCache();
            //StaticMemoryCache.GetCacheDictionary();

            //try
            //{

            //    //DBProcedure.BeginTransaction();

            var newUser = new Model.User()
            {
                Name = "Ciro",
                Surname = "Spaciari"
            }.Save();

            var userFromDB = DAL.User.GetByID(1);

            var userFromCache = DAL.User.GetByID(1);

            //    //DBProcedure.Commit();
            //}
            //catch (Exception)
            //{
            //    //DBProcedure.Rollback();
            //}
            Console.Read();
        }

    }

}
