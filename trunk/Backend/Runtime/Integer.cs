using System;

namespace Boa.Runtime
{

public struct Integer : IConvertible, IRepresentable, IComparable
{ 
  #region IConvertible Members
  public static ulong ToUInt64(IFormatProvider provider)
  {
  }

  public static sbyte ToSByte(IFormatProvider provider)
  {
  }

  public static double ToDouble(IFormatProvider provider)
  {
  }

  public static DateTime ToDateTime(IFormatProvider provider)
  {
  }

  public static float ToSingle(IFormatProvider provider)
  {
  }

  public static bool ToBoolean(IFormatProvider provider)
  {
  }

  public static int ToInt32(IFormatProvider provider)
  {
  }

  public static ushort ToUInt16(IFormatProvider provider)
  {
  }

  public static short ToInt16(IFormatProvider provider)
  {
  }

  public static string ToString(IFormatProvider provider)
  {
  }

  public static byte ToByte(IFormatProvider provider)
  {
  }

  public static char ToChar(IFormatProvider provider)
  {
  }

  public static long ToInt64(IFormatProvider provider)
  {
  }

  public static System.TypeCode GetTypeCode() { return TypeCode.Object; }

  public static decimal ToDecimal(IFormatProvider provider)
  {
  }

  public static object ToType(Type conversionType, IFormatProvider provider)
  {
  }

  public static uint ToUInt32(IFormatProvider provider)
  {
  }
  #endregion

  #region IRepresentable Members

  public string __repr__()
  {
    // TODO:  Add Integer.__repr__ implementation
    return null;
  }

  #endregion

  #region IComparable Members

  public int CompareTo(object obj)
  {
    // TODO:  Add Integer.CompareTo implementation
    return 0;
  }

  #endregion
}

} // namespace Boa.Runtime
