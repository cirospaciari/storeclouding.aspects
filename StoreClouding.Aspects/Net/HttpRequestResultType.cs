using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Net
{
    /// <summary>
    /// Tipo de resposta de chamada HTTP
    /// </summary>
    public enum HttpRequestResponseType
    {
        /// <summary>
        /// Converte resposta utilizando formato JSON
        /// </summary>
        JSON,
        /// <summary>
        /// Converte resposta utilizando formato XML
        /// </summary>
        XML,
        /// <summary>
        /// Converte resposta para uma String UTF8
        /// </summary>
        String,
        /// <summary>
        /// Não converte resposta devolvendo os bytes da requisição
        /// </summary>
        Bytes
    }
}
