using System;

namespace Boa.Runtime
{

public sealed class ArrayOps
{ ArrayOps() { }

  public static int Compare(object[] arr1, int len1, object[] arr2, int len2)
  { int len = Math.Min(len1, len2);
    for(int i=0; i<len; i++)
    { int c = Ops.Compare(arr1[i], arr2[i]);
      if(c!=0) return c;
    }
    return len1==len2 ? 0 : len1<len2 ? -1 : 1;
  }
  
  public static string Repr(Array arr)
  { System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.Append(arr.GetType().FullName);
    sb.Append('(');
    for(int i=0; i<arr.Length; i++)
    { if(i>0) sb.Append(", ");
      sb.Append(Ops.Repr(arr.GetValue(i)));
    }
    sb.Append(')');
    return sb.ToString();
  }
}

} // namespace Boa.Runtime