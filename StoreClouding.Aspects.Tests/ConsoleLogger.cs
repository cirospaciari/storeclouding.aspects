using StoreClouding.Aspects.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Tests
{
    class ConsoleLogger : IMethodInterceptionAspectExceptionLogger
    {

        public void OnException(Exception ex, PostSharp.Aspects.MethodInterceptionAspect sender, PostSharp.Aspects.MethodInterceptionArgs args)
        {

            Console.WriteLine("Exception on {0}.{1}:\r\n{2}", args.Method.ReflectedType.FullName, args.Method.Name, ex.Message);
        }
    }
}
