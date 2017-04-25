using StoreClouding.Aspects.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Interfaces
{
    /// <summary>
    /// Interface para Gerenciador de Conexões
    /// </summary>
    public interface IConnectionManager
    {
        /// <summary>
        /// Inicia transação para essa thread
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Executa um Commit na transação atual
        /// </summary>
        void Commit();

        /// <summary>
        /// Executa um Rollback na transação atual
        /// </summary>
        void Rollback();

        /// <summary>
        /// Deve retornar uma conexão aberta de base de dados
        /// </summary>
        /// <param name="callID">ID da chamada atual (para identiicação se é o mesmo método na entrada e saida)</param>
        /// <param name="connectionStringName">Nome da configuração de ConnectionString</param>
        /// <param name="connectionType">Tipo de conexão</param>
        /// <returns></returns>
        System.Data.IDbConnection GetOpenedConnection(Guid callID, String connectionStringName, DBConnectionType connectionType);

        /// <summary>
        /// Dispensa conexão com base de dados
        /// </summary>
        /// <param name="callID">ID da chamada atual (para identiicação se é o mesmo método na entrada e saida)</param>
        /// <param name="connection">Conexão a ser dispensada</param>
        void DispenseConnection(Guid callID, System.Data.IDbConnection connection);
    }
}
