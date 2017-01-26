using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gmich.AOP
{
    [AttributeUsage(AttributeTargets.Method)]
    public class InjectAttribute : Attribute
    {
        public InjectAttribute(Type interceptorType, InjectIn inject)
        {
            Interceptor = interceptorType;
        }

        public Type Interceptor { get; }
    }

    public enum InjectIn
    {
        Start,
        End
    }

    public class Interceptor
    {
        public Interceptor()
        {
            Console.WriteLine("works!");
        }
        public void Intercept() { }
    }
}

