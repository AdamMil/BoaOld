using System;

namespace Boa.Runtime
{

public sealed class LongOps
{ LongOps() { }

  public static object Add(long a, object b);
  public static object BitwiseAnd(long a, object b);
  public static object BitwiseOr(long a, object b);
  public static object BitwiseXor(long a, object b);
  public static int    Compare(long a, object b);  
  public static object Divide(long a, object b);
  public static object FloorDivide(long a, object b);
  public static object LeftShift(long a, object b);
  public static object Modulus(long a, object b);
  public static object Multiply(long a, object b);
  public static object Power(long a, object b);
  public static object PowerMod(long a, object b);
  public static object Subtract(long a, object b);
  public static object RightShift(long a, object b);
}

} // namespace Boa.Runtime