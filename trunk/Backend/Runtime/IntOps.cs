using System;

namespace Boa.Runtime
{

public sealed class IntOps
{ IntOps() { }

  public static object Add(int a, object b);
  public static object BitwiseAnd(int a, object b);
  public static object BitwiseOr(int a, object b);
  public static object BitwiseXor(int a, object b);
  public static int    Compare(int a, object b);  
  public static object Divide(int a, object b);
  public static object FloorDivide(int a, object b);
  public static object LeftShift(int a, object b);
  public static object Modulus(int a, object b);
  public static object Multiply(int a, object b);
  public static object Power(int a, object b);
  public static object PowerMod(int a, object b);
  public static object Subtract(int a, object b);
  public static object RightShift(int a, object b);
}

} // namespace Boa.Runtime