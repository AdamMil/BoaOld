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
}

public sealed class Options
{ private Options() { }

  public static bool Interactive;
}

}

