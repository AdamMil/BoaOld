using System;

namespace Boa.Runtime
{

public struct Integer : IConvertible, IRepresentable, IComparable, ICloneable
{ 
  #region Constructors
  public Integer(int i)
  { if(i>0)
    { sign   = 1;
      data   = i==1 ? One.data : new uint[1] { (uint)i };
      length = 1;
    }
    else if(i<0)
    { sign   = -1;
      data   = i==-1 ? MinusOne.data : new uint[1] { (uint)-i };
      length = 1;
    }
    else { sign=0; data=Zero.data; length=0; }
  }

  public Integer(uint i)
  { if(i==0) { sign=0; data=Zero.data; length=0; }
    else
    { sign=1;
      data = i==1 ? One.data : new uint[1] { i };
      length = 1;
    }
  }

  public Integer(long i)
  { if(i==0) { sign=0; data=Zero.data; length=0; }
    else
    { ulong v;
      if(i>0)
      { sign = 1;
        v = (ulong)i;
      }
      else
      { sign = -1;
        v = (ulong)-i;
      }
      data = new uint[2] { (uint)v, (uint)(v>>32) };
      length = 2;
    }
  }

  public Integer(ulong i) { sign=1; data=new uint[2] { (uint)i, (uint)(i>>32) }; length=2; }

  Integer(int sign, params uint[] data)
  { int length = calcLength(data);
    if(length>ushort.MaxValue) throw new NotImplementedException("Integer values larger than 2097120 bits");
    this.sign=(short)sign; this.data=data; this.length=(ushort)length;
  }
  #endregion

  public int Sign { get { return sign; } }

  public Integer abs() { return sign<0 ? -this : this; }

  public bool DivisibleBy(Integer o) { throw new NotImplementedException(); }

  public override bool Equals(object obj) { return obj is Integer ? CompareTo((Integer)obj)==0 : false; }

  public override int GetHashCode()
  { uint hash=0;
    for(int i=0; i<length; i++) hash ^= data[i];
    return (int)hash;
  }
  
  #region ToString
  public override string ToString() { return ToString(10); }
  public string ToString(int radix)
  { throw new NotImplementedException();
  }
  #endregion

  public static Integer Parse(string s) { return Parse(s, 10); }
  public static Integer Parse(string s, int radix) { throw new NotImplementedException(); }

  public static readonly Integer MinusOne = new Integer(-1, new uint[1]{1});
  public static readonly Integer One  = new Integer(1, new uint[1]{1});
  public static readonly Integer Zero = new Integer(0, new uint[0]);
  
  #region Comparison operators
  public static bool operator==(Integer a, Integer b) { return a.CompareTo(b)==0; }
  public static bool operator==(Integer a, int b)     { return a.CompareTo(b)==0; }
  public static bool operator==(Integer a, long b)    { return a.CompareTo(b)==0; }
  public static bool operator==(Integer a, uint b)    { return a.CompareTo(b)==0; }
  public static bool operator==(Integer a, ulong b)   { return a.CompareTo(b)==0; }
  public static bool operator==(int a, Integer b)     { return b.CompareTo(a)==0; }
  public static bool operator==(long a, Integer b)    { return b.CompareTo(a)==0; }
  public static bool operator==(uint a, Integer b)    { return b.CompareTo(a)==0; }
  public static bool operator==(ulong a, Integer b)   { return b.CompareTo(a)==0; }

  public static bool operator!=(Integer a, Integer b) { return a.CompareTo(b)!=0; }
  public static bool operator!=(Integer a, int b)     { return a.CompareTo(b)!=0; }
  public static bool operator!=(Integer a, long b)    { return a.CompareTo(b)!=0; }
  public static bool operator!=(Integer a, uint b)    { return a.CompareTo(b)!=0; }
  public static bool operator!=(Integer a, ulong b)   { return a.CompareTo(b)!=0; }
  public static bool operator!=(int a, Integer b)     { return b.CompareTo(a)!=0; }
  public static bool operator!=(long a, Integer b)    { return b.CompareTo(a)!=0; }
  public static bool operator!=(uint a, Integer b)    { return b.CompareTo(a)!=0; }
  public static bool operator!=(ulong a, Integer b)   { return b.CompareTo(a)!=0; }

  public static bool operator<(Integer a, Integer b) { return a.CompareTo(b)<0; }
  public static bool operator<(Integer a, int b)     { return a.CompareTo(b)<0; }
  public static bool operator<(Integer a, long b)    { return a.CompareTo(b)<0; }
  public static bool operator<(Integer a, uint b)    { return a.CompareTo(b)<0; }
  public static bool operator<(Integer a, ulong b)   { return a.CompareTo(b)<0; }
  public static bool operator<(int a, Integer b)     { return b.CompareTo(a)>0; }
  public static bool operator<(long a, Integer b)    { return b.CompareTo(a)>0; }
  public static bool operator<(uint a, Integer b)    { return b.CompareTo(a)>0; }
  public static bool operator<(ulong a, Integer b)   { return b.CompareTo(a)>0; }

  public static bool operator<=(Integer a, Integer b) { return a.CompareTo(b)<=0; }
  public static bool operator<=(Integer a, int b)     { return a.CompareTo(b)<=0; }
  public static bool operator<=(Integer a, long b)    { return a.CompareTo(b)<=0; }
  public static bool operator<=(Integer a, uint b)    { return a.CompareTo(b)<=0; }
  public static bool operator<=(Integer a, ulong b)   { return a.CompareTo(b)<=0; }
  public static bool operator<=(int a, Integer b)     { return b.CompareTo(a)>=0; }
  public static bool operator<=(long a, Integer b)    { return b.CompareTo(a)>=0; }
  public static bool operator<=(uint a, Integer b)    { return b.CompareTo(a)>=0; }
  public static bool operator<=(ulong a, Integer b)   { return b.CompareTo(a)>=0; }

  public static bool operator>(Integer a, Integer b) { return a.CompareTo(b)>0; }
  public static bool operator>(Integer a, int b)     { return a.CompareTo(b)>0; }
  public static bool operator>(Integer a, long b)    { return a.CompareTo(b)>0; }
  public static bool operator>(Integer a, uint b)    { return a.CompareTo(b)>0; }
  public static bool operator>(Integer a, ulong b)   { return a.CompareTo(b)>0; }
  public static bool operator>(int a, Integer b)     { return b.CompareTo(a)<0; }
  public static bool operator>(long a, Integer b)    { return b.CompareTo(a)<0; }
  public static bool operator>(uint a, Integer b)    { return b.CompareTo(a)<0; }
  public static bool operator>(ulong a, Integer b)   { return b.CompareTo(a)<0; }

  public static bool operator>=(Integer a, Integer b) { return a.CompareTo(b)>=0; }
  public static bool operator>=(Integer a, int b)     { return a.CompareTo(b)>=0; }
  public static bool operator>=(Integer a, long b)    { return a.CompareTo(b)>=0; }
  public static bool operator>=(Integer a, uint b)    { return a.CompareTo(b)>=0; }
  public static bool operator>=(Integer a, ulong b)   { return a.CompareTo(b)>=0; }
  public static bool operator>=(int a, Integer b)     { return b.CompareTo(a)<=0; }
  public static bool operator>=(long a, Integer b)    { return b.CompareTo(a)<=0; }
  public static bool operator>=(uint a, Integer b)    { return b.CompareTo(a)<=0; }
  public static bool operator>=(ulong a, Integer b)   { return b.CompareTo(a)<=0; }
  #endregion
  
  #region Arithmetic operators
  #region Addition
  public static Integer operator+(Integer a, Integer b)
  { return a.sign==b.sign ? new Integer(a.sign, add(a.data, a.length, b.data, b.length))
                          : a - new Integer(-b.sign, b.data);
  }
  public static Integer operator+(Integer a, int b)   { return a + new Integer(b); }
  public static Integer operator+(Integer a, uint b)  { return a + new Integer(b); }
  public static Integer operator+(Integer a, long b)  { return a + new Integer(b); }
  public static Integer operator+(Integer a, ulong b) { return a + new Integer(b); }
  public static Integer operator+(int a, Integer b)   { return new Integer(a) + b; }
  public static Integer operator+(uint a, Integer b)  { return new Integer(a) + b; }
  public static Integer operator+(long a, Integer b)  { return new Integer(a) + b; }
  public static Integer operator+(ulong a, Integer b) { return new Integer(a) + b; }
  #endregion
  
  #region Subtraction
  public static Integer operator-(Integer a, Integer b)
  { int c = a.CompareTo(b);
    if(c==0) return Integer.Zero;
    c = Math.Sign(c);

    uint[] n;
    if(a.sign==b.sign)
      n = c==a.sign ? sub(a.data, a.length, b.data, b.length) : sub(b.data, b.length, a.data, a.length);
    else n = add(a.data, a.length, b.data, b.length);
    return new Integer(c, n);
  }
  public static Integer operator-(Integer a, int b)   { return a - new Integer(b); }
  public static Integer operator-(Integer a, uint b)  { return a - new Integer(b); }
  public static Integer operator-(Integer a, long b)  { return a - new Integer(b); }
  public static Integer operator-(Integer a, ulong b) { return a - new Integer(b); }
  public static Integer operator-(int a, Integer b)   { return new Integer(a) - b; }
  public static Integer operator-(uint a, Integer b)  { return new Integer(a) - b; }
  public static Integer operator-(long a, Integer b)  { return new Integer(a) - b; }
  public static Integer operator-(ulong a, Integer b) { return new Integer(a) - b; }
  #endregion
  
  #region Multiplication
  public static Integer operator*(Integer a, Integer b)
  { throw new NotImplementedException();
  }
  public static Integer operator*(Integer a, int b)   { return a * new Integer(b); }
  public static Integer operator*(Integer a, uint b)  { return a * new Integer(b); }
  public static Integer operator*(Integer a, long b)  { return a * new Integer(b); }
  public static Integer operator*(Integer a, ulong b) { return a * new Integer(b); }
  public static Integer operator*(int a, Integer b)   { return new Integer(a) * b; }
  public static Integer operator*(uint a, Integer b)  { return new Integer(a) * b; }
  public static Integer operator*(long a, Integer b)  { return new Integer(a) * b; }
  public static Integer operator*(ulong a, Integer b) { return new Integer(a) * b; }
  #endregion

  #region Division
  public static Integer operator/(Integer a, Integer b)
  { throw new NotImplementedException();
  }
  public static Integer operator/(Integer a, int b)   { return a / new Integer(b); }
  public static Integer operator/(Integer a, uint b)  { return a / new Integer(b); }
  public static Integer operator/(Integer a, long b)  { return a / new Integer(b); }
  public static Integer operator/(Integer a, ulong b) { return a / new Integer(b); }
  public static Integer operator/(int a, Integer b)   { return new Integer(a) / b; }
  public static Integer operator/(uint a, Integer b)  { return new Integer(a) / b; }
  public static Integer operator/(long a, Integer b)  { return new Integer(a) / b; }
  public static Integer operator/(ulong a, Integer b) { return new Integer(a) / b; }
  #endregion
  
  #region Modulus
  public static Integer operator%(Integer a, Integer b)
  { throw new NotImplementedException();
  }
  public static Integer operator%(Integer a, int b)   { return a % new Integer(b); }
  public static Integer operator%(Integer a, uint b)  { return a % new Integer(b); }
  public static Integer operator%(Integer a, long b)  { return a % new Integer(b); }
  public static Integer operator%(Integer a, ulong b) { return a % new Integer(b); }
  public static Integer operator%(int a, Integer b)   { return new Integer(a) % b; }
  public static Integer operator%(uint a, Integer b)  { return new Integer(a) % b; }
  public static Integer operator%(long a, Integer b)  { return new Integer(a) % b; }
  public static Integer operator%(ulong a, Integer b) { return new Integer(a) % b; }
  #endregion
  
  #region Unary
  public static Integer operator-(Integer i) { return new Integer(-i.sign, i.data); }
  public static Integer operator~(Integer i) { return -(i+Integer.One); }
  #endregion
  
  #region Bitwise And
  public static Integer operator&(Integer a, Integer b)
  { throw new NotImplementedException();
  }
  public static Integer operator&(Integer a, int b)   { return a & new Integer(b); }
  public static Integer operator&(Integer a, uint b)  { return a & new Integer(b); }
  public static Integer operator&(Integer a, long b)  { return a & new Integer(b); }
  public static Integer operator&(Integer a, ulong b) { return a & new Integer(b); }
  public static Integer operator&(int a, Integer b)   { return new Integer(a) & b; }
  public static Integer operator&(uint a, Integer b)  { return new Integer(a) & b; }
  public static Integer operator&(long a, Integer b)  { return new Integer(a) & b; }
  public static Integer operator&(ulong a, Integer b) { return new Integer(a) & b; }
  #endregion

  #region Bitwise Or
  public static Integer operator|(Integer a, Integer b)
  { throw new NotImplementedException();
  }
  public static Integer operator|(Integer a, int b)   { return a | new Integer(b); }
  public static Integer operator|(Integer a, uint b)  { return a | new Integer(b); }
  public static Integer operator|(Integer a, long b)  { return a | new Integer(b); }
  public static Integer operator|(Integer a, ulong b) { return a | new Integer(b); }
  public static Integer operator|(int a, Integer b)   { return new Integer(a) | b; }
  public static Integer operator|(uint a, Integer b)  { return new Integer(a) | b; }
  public static Integer operator|(long a, Integer b)  { return new Integer(a) | b; }
  public static Integer operator|(ulong a, Integer b) { return new Integer(a) | b; }
  #endregion

  #region Bitwise Xor
  public static Integer operator^(Integer a, Integer b)
  { throw new NotImplementedException();
  }
  public static Integer operator^(Integer a, int b)   { return a ^ new Integer(b); }
  public static Integer operator^(Integer a, uint b)  { return a ^ new Integer(b); }
  public static Integer operator^(Integer a, long b)  { return a ^ new Integer(b); }
  public static Integer operator^(Integer a, ulong b) { return a ^ new Integer(b); }
  public static Integer operator^(int a, Integer b)   { return new Integer(a) ^ b; }
  public static Integer operator^(uint a, Integer b)  { return new Integer(a) ^ b; }
  public static Integer operator^(long a, Integer b)  { return new Integer(a) ^ b; }
  public static Integer operator^(ulong a, Integer b) { return new Integer(a) ^ b; }
  #endregion

  public static Integer operator<<(Integer a, int shift)
  { throw new NotImplementedException();
  }
  public static Integer operator>>(Integer a, int shift)
  { throw new NotImplementedException();
  }
  #endregion

  #region CompareTo
  public int CompareTo(Integer o)
  { if(sign!=o.sign) return sign-o.sign;
    int len=length, olen=o.length;
    if(len!=olen) return len-olen;
    for(int i=len-1; i>=0; i--) if(data[i]!=o.data[i]) return (int)(data[i]-o.data[i]);
    return 0;
  }

  public int CompareTo(int i)
  { int osign = i>0 ? 1 : i<0 ? -1 : 0;
    if(sign!=osign) return sign-osign;
    if(length>1) return sign;
    if(length==0) return 0;
    return (data[0]&0x80000000)==0 ? (int)data[0]*sign-i : sign;
  }

  public int CompareTo(uint i)
  { int osign = i>0 ? 1 : 0;
    if(sign!=osign) return sign-osign;
    if(length>1) return 1;
    if(length==0) return 0;
    return (int)(data[0]-i);
  }
  
  public int CompareTo(long i)
  { int osign = i>0 ? 1 : i<0 ? -1 : 0;
    if(sign!=osign) return sign-osign;
    if(length>2) return sign;
    if(length==0) return 0;
    if(length==1) Math.Sign((int)data[0]*sign-i);
    return (data[1]&0x80000000)==0 ? (int)((long)((ulong)data[1]<<32 | data[0])*sign-i) : sign;
  }

  public int CompareTo(ulong i)
  { int osign = i>0 ? 1 : 0;
    if(sign!=osign) return sign-osign;
    if(length>2) return 1;
    if(length==1) return Math.Sign((long)(i-data[0]));
    if(length==0) return 0;
    uint v = (uint)(i>>32);
    return (int)(data[1]==v ? data[0]-(uint)i : data[1]-v);
  }
  #endregion

  #region IConvertible Members
  public ulong ToUInt64()
  { if(sign<0 || length>2) throw new OverflowException("Integer won't fit into a ulong");
    if(sign==0) return 0;
    return data.Length==1 ? data[0] : (ulong)data[1]<<32 | data[0];
  }
  public ulong ToUInt64(IFormatProvider provider) { return ToUInt64(); }

  public sbyte ToSByte()
  { if(length>1 || (sign>0 && this>sbyte.MaxValue) || (sign<0 && this<sbyte.MinValue))
      throw new OverflowException("Integer won't fit into an sbyte");
    return length==0 ? (sbyte)0 : (sbyte)((int)data[0] * sign);
  }
  public sbyte ToSByte(IFormatProvider provider) { return ToSByte(); }

  public double ToDouble()
  { throw new NotImplementedException();
  }
  public double ToDouble(IFormatProvider provider) { return ToDouble(); }

  public DateTime ToDateTime(IFormatProvider provider) { throw new InvalidCastException(); }
  public float ToSingle() { return (float)ToDouble(); }
  public float ToSingle(IFormatProvider provider) { return (float)ToDouble(); }
  public bool ToBoolean() { return sign!=0; }
  public bool ToBoolean(IFormatProvider provider) { return sign!=0; }

  public int ToInt32()
  { if(length==0) return 0;
    if(length>1 || (sign>0 && data[0]>(uint)int.MaxValue) || (sign<0 && data[0]>0x80000000))
      throw new OverflowException("Integer won't fit into an int");
    return (int)data[0]*sign;
  }
  public int ToInt32(IFormatProvider provider) { return ToInt32(); }

  public ushort ToUInt16()
  { if(length==0) return 0;
    if(length>1 || data[0]>ushort.MaxValue) throw new OverflowException("Integer won't fit into a ushort");
    return (ushort)data[0];
  }
  public ushort ToUInt16(IFormatProvider provider) { return ToUInt16(); }

  public short ToInt16()
  { if(length==0) return 0;
    if(length>1 || (sign>0 && data[0]>(uint)short.MaxValue) || (sign<0 && data[0]>(uint)-short.MinValue))
      throw new OverflowException("Integer won't fit into an int");
    return (short)((int)data[0]*sign);
  }
  public short ToInt16(IFormatProvider provider) { return ToInt16(); }

  public string ToString(IFormatProvider provider) { return ToString(); }

  public byte ToByte()
  { if(sign<0 || length>1 || (sign>0 && this>byte.MaxValue))
      throw new OverflowException("Integer won't fit into a byte");
    return length==0 ? (byte)0 : (byte)data[0];
  }
  public byte ToByte(IFormatProvider provider) { return ToByte(); }

  public char ToChar()
  { if(length==0) return '\0';
    if(length>1 || data[0]>char.MaxValue) throw new OverflowException("Integer won't fit into a char");
    return (char)data[0];
  }
  public char ToChar(IFormatProvider provider) { return ToChar(); }

  public long ToInt64()
  { if(sign<0 || length>2) throw new OverflowException("Integer won't fit into a long");
    if(sign==0) return 0;
    if(data.Length==1) return sign*(int)data[0];
    if((data[1]&0x80000000)!=0) throw new OverflowException("Integer won't fit into a long");
    return (long)((ulong)data[1]<<32 | data[0]);
  }
  public long ToInt64(IFormatProvider provider) { return ToInt64(); }

  public System.TypeCode GetTypeCode() { return TypeCode.Object; }

  public decimal ToDecimal()
  { throw new NotImplementedException();
  }
  public decimal ToDecimal(IFormatProvider provider) { return ToDecimal(); }

  public object ToType(Type conversionType, IFormatProvider provider)
  { if(conversionType==typeof(int)) return ToInt32(provider);
    if(conversionType==typeof(double)) return ToDouble(provider);
    if(conversionType==typeof(long)) return ToInt64(provider);
    if(conversionType==typeof(bool)) return ToBoolean(provider);
    if(conversionType==typeof(string)) return ToString(provider);
    if(conversionType==typeof(uint)) return ToUInt32(provider);
    if(conversionType==typeof(ulong)) return ToUInt64(provider);
    if(conversionType==typeof(float)) return ToSingle(provider);
    if(conversionType==typeof(short)) return ToInt16(provider);
    if(conversionType==typeof(ushort)) return ToUInt16(provider);
    if(conversionType==typeof(byte)) return ToByte(provider);
    if(conversionType==typeof(sbyte)) return ToSByte(provider);
    if(conversionType==typeof(decimal)) return ToDecimal(provider);
    if(conversionType==typeof(char)) return ToChar(provider);
    throw new InvalidCastException();
  }

  public uint ToUInt32()
  { if(length>1) throw new OverflowException("Integer won't fit into a uint");
    return length==0 ? 0 : data[0];
  }
  public uint ToUInt32(IFormatProvider provider) { return ToUInt32(); }
  #endregion

  #region IRepresentable Members
  public string __repr__() { return ToString()+'L'; }
  #endregion

  #region IComparable Members
  public int CompareTo(object obj)
  { if(obj is Integer) return CompareTo((Integer)obj);
    throw new ArgumentException();
  }
  #endregion
  
  #region ICloneable Members
  public object Clone() { return new Integer(sign, (uint[])data.Clone()); }
  #endregion

  #region Pow
  internal Integer Pow(uint power)
  { if(power==0) return Integer.One;
    if(power==2) return squared();
    if(power<0) throw new ArgumentOutOfRangeException("power", power, "power must be >= 0");

    Integer factor = this;
    Integer result = Integer.One;
    while(power!=0)
    { if((power&1)!=0) result *= factor;
      factor = factor.squared();
      power >>= 1;
    }
    return result; 
  }

  internal Integer Pow(uint power, object mod)
  { throw new NotImplementedException();
  }

  internal Integer Pow(Integer power) { return Pow(power.ToUInt32()); }
  internal Integer Pow(Integer power, object mod) { return Pow(power.ToUInt32(), mod); }
  #endregion
  
  Integer squared() { return this*this; } // TODO: this can be optimized much better

  uint[] data;
  short sign;
  ushort length;

  static uint[] add(uint[] a, int alen, uint[] b, int blen)
  { return blen>alen ? addArrays(b, blen, a, alen) : addArrays(a, alen, b, blen);
  }

  static uint[] addArrays(uint[] a, int alen, uint[] b, int blen) // assumes alen >= blen
  { uint[] n = new uint[alen];
    ulong sum = 0;
    int i=0;
    for(; i<blen; i++)
    { sum = sum + a[i] + b[i];
      n[i] = (uint)sum;
      sum >>= 32;
    }
    for(; i<alen && sum!=0; i++)
    { sum = sum + a[i];
      n[i] = (uint)sum;
      sum >>= 32;
    }
    if(sum!=0)
    { n = resize(n, alen+1);
      n[i] = (uint)sum;
    }
    else for(; i<alen; i++) n[i]=a[i];
    return n;
  }

  static int calcLength(uint[] data)
  { int len = data.Length-1; 
    while(len>=0 && data[len]==0) len--;
    return len+1;
  }

  static uint[] resize(uint[] array, int length)
  { if(array.Length>=length) return array;
    uint[] narr = new uint[length];
    Array.Copy(array, narr, array.Length);
    return narr;
  }

  static uint[] sub(uint[] a, int alen, uint[] b, int blen)
  { uint[] n = new uint[alen];
    int  i=0;
    uint ai, bi;
    bool borrow=false;

    for(i=0; i<blen; i++)
    { ai=a[i]; bi=b[i];
      if(borrow)
      { if(ai==0) ai=0xffffffff;
        else borrow = bi > --ai;
      }
      n[i] = ai-bi;
    }

    if(borrow)
    { for(; i<alen; i++)
      { ai = a[i];
        n[i] = ai-1;
        if(ai!=0) { i++; break; }
      }
    }
    for(; i<alen; i++) n[i] = a[i];
    return n;
  }
}

} // namespace Boa.Runtime
