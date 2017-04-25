using PostSharp.Aspects.Dependencies;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace StoreClouding.Aspects.Performance
{
    /// <summary>
    /// Realiza cache de curto prazo mantendo o retorno em uma variavel Static em memória, usando os
    /// parametros como chave do cache (para objetos implemente override ToString()). Não realiza cache de output params.
    /// </summary>
    [Serializable]
    public class StaticMemoryCache : PostSharp.Aspects.MethodInterceptionAspect
    {
        /// <summary>
        /// Indica se foi passado o valor do IgnoreValue caso ele não seja marcado 
        /// todos os valores vão para o cache
        /// </summary>
        private bool SettedIgnoreValue;

        /// <summary>
        /// Não salva/atualiza o cache caso o valor seja igual a esse objeto porem 
        /// somente se settedIgnoreValue == true
        /// </summary>
        private object ignoreValue = null;

        /// <summary>
        /// Não salva/atualiza o cache caso o valor seja igual a esse objeto
        /// </summary>
        public object IgnoreValue
        {
            get
            {
                if (!SettedIgnoreValue)
                    throw new InvalidOperationException();

                return ignoreValue;
            }
            set
            {
                ignoreValue = value;
                SettedIgnoreValue = true;
            }
        }

        /// <summary>
        /// Duração do cache em milisegundos (-1 indica que não expira)
        /// </summary>
        public long Duration { get; set; }

        /// <summary>
        /// Sincroniza chamadas de varias Threads
        /// </summary>
        public bool Synchronize { get; set; }

        /// <summary>
        /// Nome dos parametros que serão ignorados na criação da chave do cache
        /// </summary>
        private string[] IgnoredParameters { get; set; }

        /// <summary>
        /// Dicionario usado para Cache
        /// </summary>
        private static ConcurrentDictionary<string, StaticMemoryCacheItem> CacheDictionary = new ConcurrentDictionary<string, StaticMemoryCacheItem>();
        /// <summary>
        /// Objeto usado para sincronizar chamadas
        /// </summary>
        private object Locker = new object();

        /// <summary>
        /// Realiza cache de curto prazo mantendo o resultado em uma variavel Static em memória, usando os
        /// parametros como chave do cache (para objetos implemente override ToString()). Não realiza cache de output params.
        /// </summary>
        /// <param name="duration">Tempo de cache em milisegundos (-1 indica que não expira)</param>
        /// <param name="synchronize">Sincroniza chamadas de varias Threads</param>
        /// <param name="ignoredParameters">Nome dos parametros que serão ignorados na criação da chave do cache</param>
        public StaticMemoryCache(long duration, bool synchronize, params string[] ignoredParameters)
        {
            Duration = duration;
            IgnoredParameters = ignoredParameters;
            Synchronize = synchronize;
        }

        /// <summary>
        /// Realiza cache de curto prazo mantendo o resultado em uma variavel Static em memória, usando os
        ///  parametros como chave do cache (para objetos implemente override ToString()). Não realiza cache de output params.
        /// </summary>
        /// <param name="duration">Tempo de cache em milisegundos</param>
        /// <param name="ignoredParameters">Nome dos parametros que serão ignorados na criação da chave do cache</param>
        public StaticMemoryCache(long duration, params string[] ignoredParameters)
            : this(duration, true, ignoredParameters)
        {
        }

        /// <summary>
        /// Intercepta e trata cache em memória
        /// </summary>
        /// <param name="args">Argumentos do método chamado</param>
        public override void OnInvoke(PostSharp.Aspects.MethodInterceptionArgs args)
        {
            string cacheKey = CreateCacheKey(args);
            StaticMemoryCacheItem cache;
            //Verifica se o cache existe e se o mesmo esta expirado
            if (!CacheDictionary.TryGetValue(cacheKey, out cache) || (Duration > -1 && (DateTime.UtcNow.Subtract(cache.Date).TotalMilliseconds >= Duration)))
            {
                //limpa cache antes da sincronia para buscar o cache atualizado
                cache = null;

                if (Synchronize)
                {
                    //sincroniza chamada
                    lock (Locker)
                    {
                        //somente chama caso o cache seja null (ou seja não tenha sido pre carregado durante o lock)
                        if (cache == null)
                            cache = CreateCache(args, cacheKey);
                    }
                }
                else
                {
                    //Executa sem sincronismo de chamada
                    cache = CreateCache(args, cacheKey);
                }
            }
            //Retorna valor
            args.ReturnValue = cache.Value;
        }

        /// <summary>
        /// Executa procedimento padrão e atualiza/salva cache se necessário
        /// </summary>
        /// <param name="args">Argumentos do método chamado</param>
        /// <param name="cacheKey">Chave de cache</param>
        /// <returns>Retorna item de cache</returns>
        private StaticMemoryCacheItem CreateCache(PostSharp.Aspects.MethodInterceptionArgs args, string cacheKey)
        {
            //executa procedimento padrão
            args.Proceed();

            //salva cache
            StaticMemoryCacheItem cache = new StaticMemoryCacheItem()
            {
                Date = DateTime.UtcNow,
                Value = args.ReturnValue,
                Owner = this
            };
            if (!SettedIgnoreValue || args.ReturnValue != ignoreValue)
            {
                //armazena/atualiza cache
                CacheDictionary.AddOrUpdate(cacheKey, cache, (key, oldValue) => cache);
            }
            return cache;
        }

        /// <summary>
        /// Cria chave de cache com base nos argumentos do método chamado
        /// </summary>
        /// <param name="args">Argumentos do método chamado</param>
        /// <returns></returns>
        private string CreateCacheKey(PostSharp.Aspects.MethodInterceptionArgs args)
        {
            StringBuilder keyBuilder = new StringBuilder(args.Method.ReflectedType.FullName);
            keyBuilder.Append(".");
            keyBuilder.Append(args.Method.Name);
            keyBuilder.Append("(");

            var parameters = args.Method.GetParameters();

            for (var i = 0; i < parameters.Length; i++)
            {
                if (IgnoredParameters != null && IgnoredParameters.Contains(parameters[i].Name))
                    continue;
                keyBuilder.Append(parameters[i].Name);
                if (args.Arguments[i] != null)
                {
                    keyBuilder.Append("{");
                    keyBuilder.Append(args.Arguments[i]);
                    keyBuilder.Append("}");
                }
                else
                {
                    keyBuilder.Append("null");
                }
                keyBuilder.Append(";");

            }

            keyBuilder.Append(")");
            return keyBuilder.ToString();
        }

        /// <summary>
        /// Dicionário usado para Cache, abrindo possibilidades de tratamentos externos
        /// </summary>
        /// <returns>Retorna Dicionário usado para Cache, abrindo possibilidades de tratamentos externos</returns>
        public static ConcurrentDictionary<string, StaticMemoryCacheItem> GetCacheDictionary()
        {
            return CacheDictionary;
        }

        /// <summary>
        /// Limpa cache expirado da memória
        /// </summary>
        public static void ClearExpiredCache()
        {
            lock (CacheDictionary)
            {
                List<string> keysToClean = new List<string>();
                foreach (var item in CacheDictionary)
                {
                    var cache = item.Value;
                    if ((cache.Owner.Duration > -1 && (DateTime.UtcNow.Subtract(cache.Date).TotalMilliseconds >= cache.Owner.Duration)))
                        keysToClean.Add(item.Key);
                }
                StaticMemoryCacheItem temp;
                foreach (var key in keysToClean)
                {
                    CacheDictionary.TryRemove(key, out temp);
                }
                GC.Collect();
            }
        }

        /// <summary>
        /// Limpa todos os caches da memória
        /// </summary>
        public static void ClearCache()
        {
            lock (CacheDictionary)
            {
                CacheDictionary.Clear();
                CacheDictionary = new ConcurrentDictionary<string, StaticMemoryCacheItem>();
                GC.Collect();
            }
        }
    }
}
