using System;

// TODO: respect this: http://rgruet.free.fr/PQR2.3.html

namespace Boa.Runtime
{

public interface ICallable
{ object Call(params object[] args);
}

public interface IContainer
{ int __len__();
  bool __contains__(object value);
}

public interface IRepresentable
{ string __repr__();
}

public interface IDescriptor
{ object __get__(object instance);
}

public interface IDataDescriptor : IDescriptor
{ void __set__(object instance, object value);
  void __delete__(object instance);
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
{ object __add__(object o);
  object __getitem__(int index);
  object __getitem__(Slice slice);
}

public interface IMutableSequence : ISequence
{ void __delitem__(int index);
  void __delitem__(Slice slice);
  void __setitem__(int index, object value);
  void __setitem__(Slice slice, object value);
}

public interface IMapping : IContainer
{ object get(object key);
  object get(object key, object defaultValue);

  void __delitem__(object key);
  object __getitem__(object key);
  void __setitem__(object key, object value);
}

} // namespace Boa.Runtime
