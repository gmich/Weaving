using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Gmich.AOP
{
    public static class AopSettings
    {
        public static Func<Type, object> InstanceFactory { get; set; } = type => Activator.CreateInstance(type);
        public static void CreateInstance(string type) => InstanceFactory(Type.GetType(type));
        internal static MethodReference Factory { get; }

        static AopSettings()
        {
            var path = new Uri(Assembly.GetExecutingAssembly().CodeBase).AbsolutePath;
            var fileName = Path.GetFileName(path);
            Weave.LogInfo($"Reading codebase {fileName} ");

            var assembly = AssemblyDefinition.ReadAssembly(fileName);
            Factory = assembly.MainModule.Import(typeof(AopSettings).GetMethod("CreateInstance"));
        }
    }
}
