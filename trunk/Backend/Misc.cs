// TODO: make use of 'readonly' attribute
// TODO: make use of 'sealed' attribute
// TODO: unify variable names

using System;

namespace Boa
{

public sealed class Misc
{ private Misc() { }
  
  public static Type[] MakeTypeArray(Type type, int length)
  { Type[] arr = new Type[length];
    for(int i=0; i<length; i++) arr[i] = type;
    return arr;
  }
  
  public static readonly object[] EmptyArray = new object[0];
}

public sealed class Options
{ private Options() { }

  public static bool Debug, Interactive;
}

}

