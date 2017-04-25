using PostSharp.Aspects.Dependencies;
using StoreClouding.Aspects.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Data
{
    /// <summary>
    /// Substitui corpo do método chamada de Procedure, sendo o corpo do método chamado em caso de Exception
    /// A Exception pode ser objeta usando DBProcedure.LastError
    /// </summary>
    [Serializable]
    public class DBProcedure : PostSharp.Aspects.MethodInterceptionAspect
    {   /// <summary>
        /// Ultimo Exception disparado no Aspecto
        /// </summary>
        private static Exception lastError { get; set; }
        /// <summary>
        /// Ultimo Exception disparado no Aspecto
        /// </summary>
        public static Exception LastError
        {
            get
            {
                return lastError;
            }
        }
        /// <summary>
        /// Logger para disparos de Exceptions ocorridos
        /// </summary>
        public static IMethodInterceptionAspectExceptionLogger Logger { get; set; }
        /// <summary>
        /// Tipo de conexão
        /// </summary>
        public DBConnectionType ConnectionType { get; set; }
        /// <summary>
        /// Gerenciador padrão de conexões (BasicConnectionManager)
        /// </summary>
        private static IConnectionManager DefaultManager = new BasicConnectionManager();
        /// <summary>
        /// Gerenciador de Conexões para chamadas de procedure (Por padrão usa BasicConnectionManager)
        /// </summary>
        public static IConnectionManager ConnectionManager { get; set; }
        /// <summary>
        /// Nome da ConnectionString
        /// </summary>
        private string ConnectionStringName { get; set; }
        /// <summary>
        /// Nome da Procedure
        /// </summary>
        private string ProcedureName { get; set; }

        /// <summary>
        /// Mapa de DbTypes
        /// </summary>
        static Dictionary<Type, DbType> TypeMap = new Dictionary<Type, DbType>();
        /// <summary>
        /// Instancia Mapa de DbTypes
        /// </summary>
        static DBProcedure()
        {
            ConnectionManager = ConnectionManager ?? DefaultManager;

            TypeMap = new Dictionary<Type, DbType>();
            TypeMap[typeof(byte)] = DbType.Byte;
            TypeMap[typeof(sbyte)] = DbType.SByte;
            TypeMap[typeof(short)] = DbType.Int16;
            TypeMap[typeof(ushort)] = DbType.UInt16;
            TypeMap[typeof(int)] = DbType.Int32;
            TypeMap[typeof(uint)] = DbType.UInt32;
            TypeMap[typeof(long)] = DbType.Int64;
            TypeMap[typeof(ulong)] = DbType.UInt64;
            TypeMap[typeof(float)] = DbType.Single;
            TypeMap[typeof(double)] = DbType.Double;
            TypeMap[typeof(decimal)] = DbType.Decimal;
            TypeMap[typeof(bool)] = DbType.Boolean;
            TypeMap[typeof(string)] = DbType.String;
            TypeMap[typeof(char)] = DbType.StringFixedLength;
            TypeMap[typeof(Guid)] = DbType.Guid;
            TypeMap[typeof(DateTime)] = DbType.DateTime;
            TypeMap[typeof(DateTimeOffset)] = DbType.DateTimeOffset;
            TypeMap[typeof(byte[])] = DbType.Binary;
            TypeMap[typeof(byte?)] = DbType.Byte;
            TypeMap[typeof(sbyte?)] = DbType.SByte;
            TypeMap[typeof(short?)] = DbType.Int16;
            TypeMap[typeof(ushort?)] = DbType.UInt16;
            TypeMap[typeof(int?)] = DbType.Int32;
            TypeMap[typeof(uint?)] = DbType.UInt32;
            TypeMap[typeof(long?)] = DbType.Int64;
            TypeMap[typeof(ulong?)] = DbType.UInt64;
            TypeMap[typeof(float?)] = DbType.Single;
            TypeMap[typeof(double?)] = DbType.Double;
            TypeMap[typeof(decimal?)] = DbType.Decimal;
            TypeMap[typeof(bool?)] = DbType.Boolean;
            TypeMap[typeof(char?)] = DbType.StringFixedLength;
            TypeMap[typeof(Guid?)] = DbType.Guid;
            TypeMap[typeof(DateTime?)] = DbType.DateTime;
            TypeMap[typeof(DateTimeOffset?)] = DbType.DateTimeOffset;
            //TypeMap[typeof(System.Data.Linq.Binary)] = DbType.Binary;
        }

        /// <summary>
        /// Substiu corpo do método por chamada de procedure já persistindo o objeto
        /// Corpo do método é chamada em caso de Exception, que pode ser obtida em DBProcedure.LastError
        /// </summary>
        /// <param name="connectionStringName">Nome da configuração de conexão</param>
        /// <param name="procedureName">Nome da procedure (Default é o nome do método, pode ser utilizado @className e @methodName e estes serão substituidos pelo nome da Classe e do Método)</param>
        public DBProcedure(string connectionStringName = null, string procedureName = null)
        {
            ConnectionType = DBConnectionType.Default;
            ConnectionStringName = connectionStringName;
            ProcedureName = procedureName;
        }

        /// <summary>
        /// Realiza sobreescrita de chamada no método por chamada de Procedure
        /// </summary>
        /// <param name="args">Argumentos da chamada do método</param>
        public override void OnInvoke(PostSharp.Aspects.MethodInterceptionArgs args)
        {
            //caso não seja declarado o ConnectionManager usa o Default
            var connectionManager = ConnectionManager ?? DefaultManager;
            //cria ID da chamada
            var callID = Guid.NewGuid();
            try
            {
                //caso não seja informado o nome da procedure usa o nome do método como nome da procedure
                if (ProcedureName == null)
                    ProcedureName = args.Method.Name;
                else
                {
                    //realiza replace de variaveis no nome da procedure
                    ProcedureName = ProcedureName.Replace("@className", args.Method.ReflectedType.Name);
                    ProcedureName = ProcedureName.Replace("@methodName", args.Method.Name);
                }

                //pega conexão com o manager
                IDbConnection connection = connectionManager.GetOpenedConnection(callID, ConnectionStringName, ConnectionType);
                //cria comando
                IDbCommand cmd = connection.CreateCommand();

                try
                {
                    //marca o comando como procedure
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = ProcedureName;

                    var parameters = args.Method.GetParameters();


                    #region Trata parametros da procedure
                    if (parameters.Length >= 1 && !IsPrimitive(parameters[0].ParameterType))
                    {
                        //caso tenha mais parametros todos devem ser de saida
                        #region Monta parametros de saida
                        for (var i = 1; i < parameters.Length; i++)
                        {
                            if (!parameters[i].IsOut || !IsPrimitive(parameters[i].ParameterType.GetElementType()))
                                throw new ArgumentException("If the first parameter is an object other parameters to be output and primitives", parameters[1].Name);

                            var dbType = DbTypeFromType(parameters[i].ParameterType.GetElementType());
                            if (dbType == null)
                                throw new ArgumentException("Invalid Type", parameters[i].Name);

                            IDbDataParameter parameter = cmd.CreateParameter();
                            parameter.ParameterName = parameters[i].Name;
                            parameter.DbType = dbType.Value;

                            parameter.Direction = ParameterDirection.Output;

                            cmd.Parameters.Add(parameter);
                        }
                        #endregion
                        //parametro não primitivo não pode ser de saida
                        if (parameters[0].IsOut)
                            throw new ArgumentException("Objects can not be output parameters", parameters[0].Name);
                        //o objeto não pode ser nulo
                        var value = args.Arguments[0];
                        if (value == null)
                            throw new ArgumentException("Invalid Parameter (Null Value)", parameters[0].Name);
                        //somente properties publicas
                        var properties = value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                        #region Adiciona propriedades como se fossem parametros de entrada
                        foreach (var property in properties)
                        {
                            var dbType = DbTypeFromType(property.PropertyType);
                            if (dbType == null)
                                throw new ArgumentException("Invalid Type", property.Name);

                            IDbDataParameter parameter = cmd.CreateParameter();
                            parameter.ParameterName = property.Name;
                            parameter.DbType = dbType.Value;
                            parameter.Value = property.GetGetMethod().Invoke(value, null);
                            cmd.Parameters.Add(parameter);
                        }
                        #endregion
                    }
                    else
                    {
                        //adiciona parametros
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            IDbDataParameter parameter = cmd.CreateParameter();
                            parameter.ParameterName = parameters[i].Name;


                            DbType? dbType;

                            //caso seja de saida não passa valor apenas indica que é de saida
                            if (parameters[i].IsOut)
                            {
                                dbType = DbTypeFromType(parameters[i].ParameterType.GetElementType());
                                parameter.Direction = ParameterDirection.Output;
                            }
                            else
                            {
                                dbType = DbTypeFromType(parameters[i].ParameterType);
                                parameter.Value = args.Arguments[i];
                            }

                            if (dbType == null)
                                throw new ArgumentException("Invalid Type", parameters[i].Name);

                            parameter.DbType = dbType.Value;
                            cmd.Parameters.Add(parameter);
                        }
                    }
                    #endregion

                    var methodInfo = (args.Method as MethodInfo);
                    //caso não tenmha retorno executa como NonQuery
                    if (methodInfo.ReturnType == typeof(void))
                        cmd.ExecuteNonQuery();
                    else
                    {
                        #region Realiza tratamentos de retorno
                        //caso tenha retorno executa como reader

                        //verifica se o resultado é uma lista
                        bool isListResult = methodInfo.ReturnType.IsGenericType && (methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(List<>));
                        //verifica se o resultado é primitivo ou se o item da lista é primitivo
                        bool isPrimitiveResult = isListResult ? IsPrimitive(methodInfo.ReturnType.GetGenericArguments()[0]) : IsPrimitive(methodInfo.ReturnType);
                        using (var reader = cmd.ExecuteReader())
                        {
                            //instancia objeto de retorno
                            var instance = Activator.CreateInstance(methodInfo.ReturnType);
                            //lê todos os items do reader
                            while (reader.Read())
                            {
                                int fieldCount = reader.FieldCount;
                                //caso só tenha uma coluna e o item da lista ou resultado for primitivo 
                                //lê somente ele
                                if (fieldCount == 1 && isPrimitiveResult)
                                {
                                    //caso não seja lista retorna diretamente o primitivo
                                    if (!isListResult)
                                    {
                                        //pega só o primeiro pois não é lista
                                        args.ReturnValue = reader.GetValue(0) is DBNull ? null : reader.GetValue(0);
                                        break;
                                    }
                                    else//caso seja lista adiciona na lista
                                        ((IList)instance).Add(reader.GetValue(0) is DBNull ? null : reader.GetValue(0));

                                }
                                else
                                {
                                    //verifica o tipo do objeto
                                    var itemType = isListResult ? methodInfo.ReturnType.GetGenericArguments()[0] : methodInfo.ReturnType;
                                    //istancia o objeto
                                    var item = Activator.CreateInstance(itemType);

                                    //preenche o objeto com as colunas
                                    for (int i = 0; i < fieldCount; i++)
                                    {
                                        try
                                        {
                                            //caso não tenha uma propriedade com o nome da coluna
                                            //ignora o resultado
                                            string name = reader.GetName(i);
                                            var property = itemType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                                            if (property == null)
                                                continue;
                                            //caso achae a propriedade seta o valor
                                            property.SetValue(item, reader.GetValue(i) is DBNull ? null : reader.GetValue(i));

                                        }
                                        catch (Exception) { }

                                    }
                                    //caso não seja lista o resultado apenas retorna
                                    if (!isListResult)
                                    {
                                        //pega só o primeiro pois não é lista
                                        args.ReturnValue = item;
                                        break;
                                    }
                                    else//caso seja lista adiciona o item na lista
                                        ((IList)instance).Add(item);
                                }
                            }
                            //caso seja lista retorna o valor
                            if (isListResult)
                                args.ReturnValue = instance;
                        }
                        #endregion
                    }


                    //Recupera paametros de saida caso tenha
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        if (parameters[i].IsOut)
                        {
                            //passa argumento validando DBNull para null
                            args.Arguments[i] = (cmd.Parameters[parameters[i].Name] as IDbDataParameter).Value is DBNull ? null : (cmd.Parameters[parameters[i].Name] as IDbDataParameter).Value;
                        }
                    }


                }
                finally
                {
                    //dispensa conexão
                    connectionManager.DispenseConnection(callID, connection);
                }

            }
            catch (Exception ex)
            {
                //chama logger para o erro
                if (Logger != null)
                    Logger.OnException(ex, this, args);
                //marca ultimo erro
                lastError = ex;
                //executa o corpo do método
                args.Proceed();
            }
        }

        /// <summary>
        /// Verifica se o tipo é primitivo ou não
        /// </summary>
        /// <param name="type">Tipo a ser verificado</param>
        /// <returns></returns>
        private bool IsPrimitive(Type type)
        {
            return (type.IsPrimitive || type == typeof(Decimal) || type == typeof(String) || type == typeof(DateTime));
        }
        /// <summary>
        /// Busca DbType para tipo especificado
        /// </summary>
        /// <param name="type">Tipo especificado</param>
        /// <returns></returns>
        private static DbType? DbTypeFromType(Type type)
        {
            lock (TypeMap)
            {
                if (!TypeMap.ContainsKey(type))
                    return null;
                return TypeMap[type];
            }
        }


        /// <summary>
        /// Inicia transação para essa thread
        /// </summary>
        public static void BeginTransaction()
        {
            //caso não seja declarado o ConnectionManager usa o Default
            var connectionManager = ConnectionManager ?? DefaultManager;
            connectionManager.BeginTransaction();
        }

        /// <summary>
        /// Executa um Commit na transação atual
        /// </summary>
        public static void Commit()
        {
            //caso não seja declarado o ConnectionManager usa o Default
            var connectionManager = ConnectionManager ?? DefaultManager;
            connectionManager.Commit();
        }

        /// <summary>
        /// Executa um Rollback na transação atual
        /// </summary>
        public static void Rollback()
        {
            //caso não seja declarado o ConnectionManager usa o Default
            var connectionManager = ConnectionManager ?? DefaultManager;
            connectionManager.Rollback();
        }
    }
}
