using System;

namespace Boa.Runtime
{

public sealed class IntegerOps
{ IntegerOps() { }

  public static object Add(Integer a, object b);
  public static object BitwiseAnd(Integer a, object b);
  public static Integer BitwiseOr(Integer a, object b);
  public static Integer BitwiseXor(Integer a, object b);
  public static int    Compare(Integer a, object b);  
  public static object Divide(Integer a, object b);
  public static object FloorDivide(Integer a, object b);
  public static Integer LeftShift(Integer a, object b);
  public static object Modulus(Integer a, object b);
  public static object Multiply(Integer a, object b);
  public static bool   NonZero(Integer a);
  public static object Power(Integer a, object b);
  public static object PowerMod(Integer a, object b);
  public static object Subtract(Integer a, object b);
  public static object RightShift(Integer a, object b);
}

} // namespace Boa.Runtime