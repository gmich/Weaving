using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Linq;
using System.Reflection;

namespace Gmich.AOP
{
    public class ILCodeWeaver : IHideObjectMembers
    {
        private readonly string assemblyPath;
        private readonly AssemblyDefinition assemblyDefinition;

        public ILCodeWeaver(string assemblyPath)
        {
            this.assemblyPath = assemblyPath;
            assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);

            EnumerateTypes();
            Reweave();
        }

        private void EnumerateMethodCustomAttributes(TypeDefinition type,
          string attributeFullName, Action<MethodDefinition, CustomAttribute> action)
        {
            Weave.LogInfo($"Looking for {attributeFullName} in type {type.FullName}.");

            foreach (var method in type.Methods)
            {
                Weave.LogInfo($"Checking for {attributeFullName} in type {type.FullName} in method {method.FullName}.");
                foreach (var attribute in method.CustomAttributes)
                {
                    if (attribute.AttributeType.FullName == attributeFullName)
                    {
                        Weave.LogInfo($"Found attribute {attributeFullName} in method {method.FullName} of {method.DeclaringType.FullName}");
                        action(method, attribute);
                    }
                }
            }
        }

        private void EnumerateTypes()
        {
            foreach (var type in assemblyDefinition.Modules.SelectMany(c => c.Types))
            {
                EnumerateMethodCustomAttributes(type, typeof(InjectAttribute).FullName, (method, attr) =>
                {
                    Setup(attr.ConstructorArguments.First().Value.ToString(), (InjectIn)attr.ConstructorArguments.Skip(1).First().Value, method, type);
                });
            }
        }

        private void Setup(string typeFullName,InjectIn location, MethodDefinition method, TypeDefinition type)
        {
            Weave.LogInfo($"Intercepting {method.FullName} of {type.FullName} with {typeFullName} in {location}");
            var interceptedType = Assembly.Load("Gmich.AOP.Interceptors").GetTypes().Where(c=>c.FullName== typeFullName).First();

            Weave.LogInfo($"Assembly Qualified Name: {interceptedType.AssemblyQualifiedName}");

            var factoryMethodRef = method.Module.Import(typeof(AopSettings).GetMethod("CreateInstance",new[] { typeof(string) }));

            var method1 = typeof(AopSettings).GetMethod("CreateInstance");
            var interceptedTypeReference = method.Module.Import(interceptedType);

            var ilProcessor = method.Body.GetILProcessor();

            var instruction = (location==InjectIn.Start)? ilProcessor.Body.Instructions.First() : ilProcessor.Body.Instructions.Last();

            Action<Instruction, Instruction> inserter = (location == InjectIn.Start) 
               ? new Action<Instruction, Instruction>((fst, sec) => ilProcessor.InsertBefore(fst, sec)) 
               : new Action<Instruction, Instruction>((fst, sec) => ilProcessor.InsertAfter(fst, sec));


            //var fieldDefinition = new FieldDefinition("Interceptor", Mono.Cecil.FieldAttributes.Private, interceptedTypeReference);
            //method.DeclaringType.Fields.Add(fieldDefinition);
            //var tempVar = new VariableDefinition("tempVar", interceptedTypeReference);
            //method.Body.Variables.Add(tempVar);

            //var field = new FieldReference("Interceptor", interceptedTypeReference, method.DeclaringType);
            //var importedField = method.Module.Import(field);

            //var variable = new VariableDefinition("interceptedType", interceptedTypeReference);
            //method.Body.Variables.Add(variable);
            //ilProcessor.Create(OpCodes.Stloc, variable);

            //ilProcessor.InsertBefore(
            //    firstInstruction,
            //    ilProcessor.Create(OpCodes.Ldarg_0));

            //ilProcessor.InsertBefore(
            // firstInstruction,
            // ilProcessor.Create(OpCodes.Ldfld, fieldDefinition));

            inserter(
             instruction,
             ilProcessor.Create(OpCodes.Ldstr, interceptedType.AssemblyQualifiedName));

            inserter(
                instruction,
                ilProcessor.Create(OpCodes.Call, factoryMethodRef));
        }

        private void Reweave()
        {
            Weave.LogInfo($"Rewritting assembly {assemblyPath}");
            assemblyDefinition.Write(assemblyPath, new WriterParameters { WriteSymbols = true });
        }
    }
}

