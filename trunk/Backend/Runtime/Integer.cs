using System;

namespace Boa.Runtime
{

public sealed class Integer
{ public object Add(object b);
  public object BitwiseAnd(object b);
  public Integer BitwiseOr(object b);
  public Integer BitwiseXor(object b);
  public Integer BitwiseNegate();
  public int    Compare(object b);  
  public object Divide(object b);
  public object FloorDivide(object b);
  public Integer LeftShift(object b);
  public object Modulus(object b);
  public object Multiply(object b);
  public Integer Negate();
  public bool   NonZero();
  public object Power(object b);
  public object PowerMod(object b);
  public object Subtract(object b);
  public object RightShift(object b);

  #region IConvertible Members
  public ulong ToUInt64(IFormatProvider provider)
  {
  }

  public sbyte ToSByte(IFormatProvider provider)
  {
  }

  public double ToDouble(IFormatProvider provider)
  {
  }

  public DateTime ToDateTime(IFormatProvider provider)
  {
  }

  public float ToSingle(IFormatProvider provider)
  {
  }

  public bool ToBoolean(IFormatProvider provider)
  {
  }

  public int ToInt32(IFormatProvider provider)
  {
  }

  public ushort ToUInt16(IFormatProvider provider)
  {
  }

  public short ToInt16(IFormatProvider provider)
  {
  }

  public string ToString(IFormatProvider provider)
  {
  }

  public byte ToByte(IFormatProvider provider)
  {
  }

  public char ToChar(IFormatProvider provider)
  {
  }

  public long ToInt64(IFormatProvider provider)
  {
  }

  public System.TypeCode GetTypeCode() { return TypeCode.Object; }

  public decimal ToDecimal(IFormatProvider provider)
  {
  }

  public object ToType(Type conversionType, IFormatProvider provider)
  {
  }

  public uint ToUInt32(IFormatProvider provider)
  {
  }
  #endregion
}

} // namespace Boa.Runtime