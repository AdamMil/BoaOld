using System;

namespace Boa.Runtime
{

public interface ICallable
{ object Call(params object[] parms);
}

public interface IContainer
{ int __len__();
  bool __contains__(object value);
}

public interface IDescriptor
{ object __get__(object obj);
}

public interface IDataDescriptor : IDescriptor
{ void __set__(object obj, object value);
  void __delete__(object obj);
}

public interface IDynamicObject
{ DynamicType GetDynamicType();
}

public interface IHasAttributes
{ List __attrs__();
  object __getattr__(string key);
  void __setattr__(string key, object value);
  void __delattr__(string key);
}

public interface ISequence : IContainer
{ object __add__(object obj);
  object __getitem__(int index);
}

public interface IMutableSequence : ISequence
{ void __delitem__(int index);
  void __setitem__(int index, object value);
}

public interface IMapping : IContainer
{ object get(object key);
  object get(object key, object defaultValue);

  object __delitem__(object key);
  object __getitem__(object key);
  object __setitem__(object key, object value);
}

} // namespace Boa.Runtime
