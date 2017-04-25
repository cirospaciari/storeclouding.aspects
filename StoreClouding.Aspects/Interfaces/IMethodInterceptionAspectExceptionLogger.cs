using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Interfaces
{
    /// <summary>
    /// Interface para Logger de Exception para Aspectos do tipo MethodInterceptionAspect
    /// </summary>
    public interface IMethodInterceptionAspectExceptionLogger
    {
        /// <summary>
        /// Realiza log de exception disparada
        /// </summary>
        /// <param name="ex">Exception disparada no Aspecto</param>
        /// <param name="sender">Aspecto que disparou a Exception</param>
        /// <param name="args">Argumentos passados na chamada do aspecto</param>
        void OnException(Exception ex, PostSharp.Aspects.MethodInterceptionAspect sender, PostSharp.Aspects.MethodInterceptionArgs args);
    }
}
