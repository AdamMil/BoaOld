using System;
using System.Collections;
using System.Reflection;

namespace Boa.Runtime
{

public class ReflectedField : IDataDescriptor
{ public ReflectedField(FieldInfo fi) { info = fi; }

  public object __get__(object obj)
  { return obj==null && info.IsStatic ? this : Ops.ToBoa(info.GetValue(obj));
  }
  public void __set__(object obj, object value)
  { if(obj==null && info.IsStatic) throw Ops.TypeError("instance field");
    info.SetValue(obj, Ops.ConvertTo(value, info.FieldType));
  }
  public void __delete__(object obj) { throw Ops.AttributeError("can't delete field on built-in object"); }

  public override string ToString()
  { return string.Format("<field '{0}' on '{1}'>", info.Name, info.DeclaringType.Name);
  }

  FieldInfo info;
}

public class ReflectedType : BoaType
{ ReflectedType(Type type)
  { //foreach(ConstructorInfo ci in type.GetConstructors()) AddConstructor(ci);
    //foreach(EventInfo ei in type.GetEvents()) AddEvent(ei);
    foreach(FieldInfo fi in type.GetFields()) AddField(fi);
    //foreach(MethodInfo mi in type.GetMethods()) AddMethod(mi);
    //foreach(PropertyInfo pi in type.GetProperties()) AddProperty(pi);
  }

  void AddField(FieldInfo fi) { names[fi.Name] = new ReflectedField(fi); }

  public static ReflectedType FromType(Type type)
  { ReflectedType rt = (ReflectedType)types[type];
    if(rt==null) types[type] = rt = new ReflectedType(type);
    return rt;
  }

  static Hashtable types = new Hashtable();
}

} // namespace Boa.Runtime