using Autofac;
using Gmich.AOP.Interceptors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gmich.AOP.Playground
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<Interceptor>().AsSelf();
            var container = builder.Build();
            AopSettings.InstanceFactory = type => container.Resolve(type);

            var prog = new Program();
            prog.Inject();
        }

        [Inject(typeof(TestInterceptor), InjectIn.Start)]
        public void Inject()
        {
            Console.ReadLine();
        }

    }


}
