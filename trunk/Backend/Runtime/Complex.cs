using System;

namespace Boa.Runtime
{

public sealed class Complex : IConvertible
{ public Complex Add(object b);
  public int     Compare(object b);  
  public Complex Divide(object b);
  public Complex FloorDivide(object b);
  public Complex Modulus(object b);
  public Complex Multiply(object b);
  public Complex Negate();
  public bool    NonZero();
  public Complex Power(object b);
  public Complex PowerMod(object b);
  public Complex Subtract(object b);

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