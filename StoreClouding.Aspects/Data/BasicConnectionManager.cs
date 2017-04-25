using StoreClouding.Aspects.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Data
{
    /// <summary>
    /// Gerenciador Básico usando DbProviderFactories (sempre abre e fecha conexões a não ser que esteja em transação)
    /// </summary>
    public sealed class BasicConnectionManager : IConnectionManager
    {
        /// <summary>
        /// Dicionario contendo as transações abertas
        /// </summary>
        private static ConcurrentDictionary<int, DbTransaction> Transactions = new ConcurrentDictionary<int, DbTransaction>();

        /// <summary>
        /// Inicia transação para essa thread
        /// </summary>
        public void BeginTransaction()
        {
            int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            //Adiciona ID da Thread para que a proxima conexão venha em Transaction
            if(!Transactions.TryAdd(threadID, null))
                throw new Exception("Failed to open a transaction in this thread");
        }

        /// <summary>
        /// Executa um Commit na transação atual
        /// </summary>
        public void Commit()
        {
            DbTransaction transaction;
            int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (!Transactions.TryRemove(threadID, out transaction))
                throw new InvalidOperationException("No open transaction in this thread");

            if (transaction != null)
            {
                transaction.Commit();
                CloseConnection(transaction.Connection);
            }
        }

        /// <summary>
        /// Executa um Rollback na transação atual
        /// </summary>
        public void Rollback()
        {

            DbTransaction transaction;
            int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (!Transactions.TryRemove(threadID, out transaction))
                throw new InvalidOperationException("No open transaction in this thread");
            
            if (transaction != null)
            {
                transaction.Rollback();
                CloseConnection(transaction.Connection);
            }
        }

        /// <summary>
        /// Deve retornar uma conexão aberta de base de dados
        /// </summary>
        /// <param name="callID">ID da chamada atual (para identiicação se é o mesmo método na entrada e saida)</param>
        /// <param name="connectionStringName">Nome da configuração de ConnectionString</param>
        /// <param name="connectionType">Tipo de conexão</param>
        /// <returns></returns>
        public System.Data.IDbConnection GetOpenedConnection(Guid callID, string connectionStringName, DBConnectionType connectionType)
        {
            //Pega configuração de connection String
            var connectionStringSetting = ConfigurationManager.ConnectionStrings[connectionStringName];
            if (connectionStringSetting == null)
            {
                if (ConfigurationManager.ConnectionStrings.Count == 0)
                    throw new ArgumentException("Invalid ConnectionString", "ConnectionString");

                connectionStringSetting = ConfigurationManager.ConnectionStrings[0];
            }


            //Fecha conexão apenas se não tiver transações abertas nessa thread
            DbTransaction transaction;
            int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (Transactions.TryGetValue(threadID, out transaction))
            {

                if (transaction == null)
                {
                    //caso a transação seja nula abre uma conexão e inicia a transação
                    var connection = OpenConnection(connectionStringSetting);
                    transaction = connection.BeginTransaction();
                    //caso não consiga atualizar a transação acusa erro
                    if (!Transactions.TryUpdate(threadID, transaction, null))
                        throw new Exception("Failed to update transaction");

                    return connection;
                }
                else
                {
                    //caso a chamada esteja em transação mas for chamado um método com uma connection string diferente
                    //acusa erro
                    if (transaction.Connection.ConnectionString != connectionStringSetting.ConnectionString)
                        throw new InvalidOperationException("When transaction calls all the procedures must have the same connection string");
                    //caso possua a mesma conection string retorna a conexão da transação
                    return transaction.Connection;
                }
            }
            else
            {
                //retorna uma nova conexão
                return OpenConnection(connectionStringSetting);
            }

        }

        /// <summary>
        /// Dispensa conexão com base de dados
        /// </summary>
        /// <param name="callID">ID da chamada atual (para identiicação se é o mesmo método na entrada e saida)</param>
        /// <param name="connection">Conexão a ser dispensada</param>
        public void DispenseConnection(Guid callID, System.Data.IDbConnection connection)
        {

            //Fecha conexão apenas se não tiver transações abertas nessa thread
            DbTransaction transaction;
            int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;
            if (Transactions.TryGetValue(threadID, out transaction))
                return;

            CloseConnection(connection);
        }

        /// <summary>
        /// Abre uma conexão usando a configuração de connectionString indicada
        /// </summary>
        /// <param name="connectionStringSetting">Configuração de connectioString</param>
        /// <returns>Conexão aberta</returns>
        private static DbConnection OpenConnection(ConnectionStringSettings connectionStringSetting)
        {
            DbProviderFactory factory = DbProviderFactories.GetFactory(connectionStringSetting.ProviderName);
            var connection = factory.CreateConnection();
            connection.ConnectionString = connectionStringSetting.ConnectionString;
            connection.Open();
            return connection;
        }

        /// <summary>
        /// Fecha conexão ignorando exceptions causados pelo fechamento da mesma
        /// </summary>
        /// <param name="connection">Conexão a ser fechada</param>
        private static void CloseConnection(System.Data.IDbConnection connection)
        {
            try
            {
                //caso a conexão exista e não esteja fechada fecha ela
                if (connection != null && connection.State != ConnectionState.Closed)
                    connection.Close();
            }
            catch (Exception) { }
        }
    }
}
