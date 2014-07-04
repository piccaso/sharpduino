using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Sharpduino.SerialProviders
{
    public sealed class SerialProviderFactory
    {
        public static ISerialProvider GetProvider()
        {
            // Default to SerialPortProvider for ease-of-use
            var providerName = ConfigurationManager.AppSettings["SerialProvider"] ?? "SerialPortProvider";

            string @namespace = "Sharpduino.SerialProviders";
            var provider  = (from t in Assembly.GetExecutingAssembly().GetTypes()
                              where t.IsClass && !t.IsAbstract &&                     //We are searching for a non-abstract class 
                                    t.Namespace == @namespace &&                        //in the namespace we provide
                                    t.GetInterfaces().Any(x => x == typeof(ISerialProvider)) && //that implements ISerialProvider
                                    t.Name == providerName
                              select t).FirstOrDefault();

            if (provider == null)
                throw new Exception("SerialProviderFactory did not find an implementation of " + providerName);

            return (ISerialProvider)Activator.CreateInstance(provider);

        }
    }
}
