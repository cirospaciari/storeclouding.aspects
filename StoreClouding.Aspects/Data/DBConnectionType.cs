using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Data
{
    /// <summary>
    /// Tipo de conexão da procedure (pode ser utilizado como parametro para o ConnectionManager)
    /// </summary>
    public enum DBConnectionType
    {
        /// <summary>
        /// Tipo de conexões padrão
        /// </summary>
        Default,
        /// <summary>
        /// Tipo de conexão compartilhada
        /// </summary>
        Shared,
        /// <summary>
        /// Tipo de conexão exclusivo somente para esta chamada
        /// </summary>
        Exclusive
    }
}
