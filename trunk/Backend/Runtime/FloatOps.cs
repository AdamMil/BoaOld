using System;

namespace Boa.Runtime
{

public sealed class FloatOps
{ FloatOps() { }

  public static object Add(double a, object b);
  public static object BitwiseAnd(double a, object b);
  public static object BitwiseOr(double a, object b);
  public static object BitwiseXor(double a, object b);
  public static int    Compare(double a, object b);  
  public static object Divide(double a, object b);
  public static object FloorDivide(double a, object b);
  public static object LeftShift(double a, object b);
  public static object Modulus(double a, object b);
  public static object Multiply(double a, object b);
  public static object Power(double a, object b);
  public static object PowerMod(double a, object b);
  public static object Subtract(double a, object b);
  public static object RightShift(double a, object b);
}

} // namespace Boa.Runtime