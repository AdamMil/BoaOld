using System;

namespace Language.Runtime
{

public interface ICallable
{ object Call(params object[] parms);
}

public interface IFastCallable : ICallable
{ object Call();
	object Call(object arg0);
	object Call(object arg0, object arg1);
	object Call(object arg0, object arg1, object arg2);
	object Call(object arg0, object arg1, object arg2, object arg3);
}

} // namespace Language.Runtime
