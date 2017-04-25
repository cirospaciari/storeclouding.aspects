using PostSharp.Extensibility;
using StoreClouding.Aspects.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[assembly: DBProcedure("MariaDB", //ConnectionString da base de dados
                       "@className_@methodName", //Formato da Procedure baseada no nome da Classe e Metodo
                       AttributeTargetTypes= "StoreClouding.Aspects.Tests.DAL.*", //Aplica em todos os objetos do DAL
                       AttributeTargetMemberAttributes = MulticastAttributes.Static,//Somente em métodos estáticos
                       //Atribui que este aspecto não tem prioridade alta ou seja pode ser sobreescrito por outros 
                       //(como StaticMemoryCache ou Log)
                       AspectPriority = int.MaxValue)]
