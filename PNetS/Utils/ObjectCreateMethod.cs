using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;

namespace PNet
{
    /// <summary>
    /// Class which creates a dynamic constructor method for the given type
    /// T is root type, if you want to restrict it to a parent + child types. Otherwise just use object.
    /// </summary>
    internal class ObjectCreateMethod<T> where T : class, new()
    {
        delegate T MethodInvoker();
        MethodInvoker methodHandler = null;

        public ObjectCreateMethod(Type type)
        {
            CreateMethod(type.GetConstructor(Type.EmptyTypes));
        }

        public ObjectCreateMethod(ConstructorInfo target)
        {
            if (typeof(T) != typeof(object) && target.DeclaringType.BaseType != typeof(T))
                throw new ArgumentException("The specified type must a child class of the generic type");
            CreateMethod(target);
        }

        void CreateMethod(ConstructorInfo target)
        {
            DynamicMethod dynamic = new DynamicMethod(string.Empty,
                        typeof(T),
                        new Type[0],
                        target.DeclaringType);

            ILGenerator il = dynamic.GetILGenerator();
            il.DeclareLocal(target.DeclaringType);
            il.Emit(OpCodes.Newobj, target);
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Ret);

            methodHandler = (MethodInvoker)dynamic.CreateDelegate(typeof(MethodInvoker));
        }

        public T CreateInstance()
        {
            return methodHandler();
        }
    }
}
