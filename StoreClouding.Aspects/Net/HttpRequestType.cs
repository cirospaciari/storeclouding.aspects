using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Net
{
    /// <summary>
    /// Tipo de chamada HTTP
    /// </summary>
    public enum HttpRequestMethod
    {
        /// <summary>
        /// Busca dados
        /// </summary>
        GET,
        /// <summary>
        /// Insere dados
        /// </summary>
        POST,
        /// <summary>
        /// Atualiza dados
        /// </summary>
        PUT,
        /// <summary>
        /// Deleta dados
        /// </summary>
        DELETE,
        /// <summary>
        /// Atualiza dados parcialmente
        /// </summary>
        PATCH 

    }
}
