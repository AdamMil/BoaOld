using System;

namespace Language
{

public sealed class Constants
{ private Constants() { }
  
  public static Type[] MakeTypeArray(Type type, int length)
  { Type[] arr = new Type[length];
    for(int i=0; i<length; i++) arr[i] = type;
    return arr;
  }
}

}
