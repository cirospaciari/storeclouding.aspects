using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Net
{
    /// <summary>
    /// Tipo de conteudo enviado na Chamada HTTP
    /// </summary>
    public enum HttpRequestContentType
    {
        /// <summary>
        /// Envia dados no formato JSON
        /// </summary>
        JSON,
        /// <summary>
        /// Envia dados no formato formulário
        /// </summary>
        FormData,
        /// <summary>
        /// Envia dados no formato XML (deve conter apenas 1 parametro e deve ser um objeto e este não pode ser nulo)
        /// </summary>
        XML,
        /// <summary>
        /// Envia diretamente bytes (deve conter apenas 1 parametro e ser do tipo byte[])
        /// </summary>
        Bytes,
        /// <summary>
        /// Envia diretamente uma String (deve conter apenas 1 parametro e ser do tipo String)
        /// </summary>
        String
    }
}
