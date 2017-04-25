using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Performance
{
    /// <summary>
    /// Modelo de item de cache em memória
    /// </summary>
    public class StaticMemoryCacheItem
    {
        /// <summary>
        /// Data em que o cache foi criado
        /// </summary>
        public DateTime Date { get; set; }
        /// <summary>
        /// Valor salvo no cache
        /// </summary>
        public object Value { get; set; }
        /// <summary>
        /// Instancia de StaticMemoryCache que criou o cache
        /// </summary>
        public StaticMemoryCache Owner { get; set; }
    }
}
