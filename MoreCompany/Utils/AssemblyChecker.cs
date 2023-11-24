using System;
using System.Reflection;
using BepInEx.Logging;

namespace MoreCompany.Utils
{
    public class AssemblyChecker
    {
        public static bool HasAssemblyLoaded(string name)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var asm in assemblies)
            {
                if (asm.GetName().Name.ToLower().Equals(name))
                {
                    return true;
                }
            }

            return false;
        }
    }
}