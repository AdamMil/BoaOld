using System;

namespace Boa.Runtime
{

public struct Integer : IConvertible, IRepresentable, IComparable, ICloneable
{ 
  #region Constructors
  public Integer(int i)
  { if(i>0)
    { sign   = 1;
      data   = i==1 ? One.data : new uint[1] { i };
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
        v = i;
      }
      else if(i<0)
      { sign = -1;
        v = (ulong)-v;
      }
      data = new uint[2] { (uint)v, (uint)(v>>32) };
      length = 2;
    }
  }

  public Integer(ulong i) { sign=1; data=new uint[2] { (uint)i, (uint)(i>>32) }; length=2; }

  Integer(int sign, params uint[] data)
  { int length = CalcLength(data);
    if(length>ushort.MaxValue) throw new NotImplementedException("Integer values larger than 2097120 bits");
    this.sign=sign; this.data=data; this.length=length;
  }
  #endregion

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
  // TODO: lots of optimizations can be done here
  public static Integer operator*(Integer a, Integer b)
  { int alen=a.length, blen=b.length, nlen=alen+blen;
    uint[] ad=a.data, bd=b.data, nd=new uint[nlen];
    
    for(int ai=0; ai<alen; ai++)
    { uint av = ad[ai];
      int  ni = ai;
      ulong carry = 0;
      
      for(int bi=0; bi<blen; bi++)
      { carry = carry + (ulong)av*bd[bi] + nd[ni];
        nd[ni++] = (uint)carry;
        carry >>= 32;
      }
      
      while(carry!=0)
      { carry += nd[ni];
        nd[ni++] = (uint)carry;
        carry >>= 32;
      }
    }
    
    return new Integer(a.sign*b.sign, nd);
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
  public static Integer operator~(Integer i) { return -(x+Integer.One); }
  #endregion
  
  #region Bitwise And
  public static Integer operator&(Integer a, Integer b);
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
  public static Integer operator|(Integer a, Integer b);
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
  public static Integer operator^(Integer a, Integer b);
  public static Integer operator^(Integer a, int b)   { return a ^ new Integer(b); }
  public static Integer operator^(Integer a, uint b)  { return a ^ new Integer(b); }
  public static Integer operator^(Integer a, long b)  { return a ^ new Integer(b); }
  public static Integer operator^(Integer a, ulong b) { return a ^ new Integer(b); }
  public static Integer operator^(int a, Integer b)   { return new Integer(a) ^ b; }
  public static Integer operator^(uint a, Integer b)  { return new Integer(a) ^ b; }
  public static Integer operator^(long a, Integer b)  { return new Integer(a) ^ b; }
  public static Integer operator^(ulong a, Integer b) { return new Integer(a) ^ b; }
  #endregion

  public static Integer operator<<(Integer a, int shift);
  public static Integer operator>>(Integer a, int shift);
  #endregion

  public Integer abs() { return sign<0 ? -this : this; }

  public override bool Equals(object obj) { return obj is Integer ? CompareTo((Integer)obj)==0 : false; }
  public override int GetHashCode()
  { return length>1 ? (int)(data[0]^data[length-1]) : length==1 ? (int)data[0] : 0;
  }

  #region ToString
  public override string ToString() { return ToString(10); }
  public override string ToString(int radix)
  { if(radix<2 || radix>36) throw new ArgumentOutOfRangeException("radix", radix, "radix must be from 2-36");
    if(length==0) return "0";
    
    System.Collections.ArrayList groups = new System.Collections.ArrayList();
    uint[] d = (uint[])data.Clone();
    int  len = length;
    uint  gr = groupRadixValues[radix];
    while(len>0) groups.Add(div(d, ref len, gr));

    System.Text.StringBuilder sb = new System.Text.StringBuilder();
    if(sign==-1) sb.Append('-');
    
    char[] digits = new char[maxCharsPerDigit[radix]];
    len = groups.Count-1;
    appendRadix((uint)groups[len--], radix, tmpDigits, sb, false);
    while(len>0) appendRadix((uint)groups[len--], radix, tmpDigits, sb, true);
    return sb.ToString();
  }
  #endregion

  public static readonly Integer MinusOne = new Integer(-1, new uint[1]{1});
  public static readonly Integer One  = new Integer(1, new uint[1]{1});
  public static readonly Integer Zero = new Integer(0, new uint[0]);

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
    return (data[0]&0x80000000)==0 ? (int)date[0]*sign-i : sign;
  }

  public int CompareTo(uint i)
  { int osign = i>0 ? 1 : 0;
    if(sign!=osign) return sign-osign;
    if(length>1) return 1;
    if(length==0) return 0;
    return date[0]-i;
  }
  
  public int CompareTo(long i)
  { int osign = i>0 ? 1 : i<0 ? -1 : 0;
    if(sign!=osign) return sign-osign;
    if(length>2) return sign;
    if(length==0) return 0;
    if(length==1) Math.Sign((int)date[0]*sign-i);
    return (data[1]&0x80000000)==0 ? (long)((ulong)date[1]<<32 | date[0])*sign-i : sign;
  }

  public int CompareTo(ulong i)
  { int osign = i>0 ? 1 : 0;
    if(sign!=osign) return sign-osign;
    if(length>2) return 1;
    if(length==1) return Math.Sign(i-data[0]);
    if(length==0) return 0;
    uint v = (uint)(i>>32);
    return (int)(data[1]==v ? data[0]-(uint)i : data[1]-v);
  }
  #endregion

  #region IConvertible Members
  public ulong ToUInt64(IFormatProvider provider)
  { if(sign<0 || length>2) throw new OverflowException("Integer won't fit into a ulong");
    if(sign==0) return 0;
    return data.Length==1 ? data[0] : (ulong)data[1]<<32 | data[0];
  }

  public sbyte ToSByte(IFormatProvider provider)
  { if(length>1 || (sign>0 && this>sbyte.MaxValue) || (sign<0 && this<sbyte.MinValue))
      throw new OverflowException("Integer won't fit into an sbyte");
    return length==0 ? 0 : (sbyte)((int)data[0] * sign);
  }

  public double ToDouble(IFormatProvider provider)
  {
  }

  public DateTime ToDateTime(IFormatProvider provider) { throw new InvalidCastException(); }
  public float ToSingle(IFormatProvider provider) { return (float)ToDouble(provider); }
  public bool ToBoolean(IFormatProvider provider) { return sign!=0; }

  public int ToInt32(IFormatProvider provider)
  { if(length==0) return 0;
    if(length>1 || (sign>0 && data[0]>(uint)int.MaxValue) || (sign<0 && data[0]>0x80000000))
      throw new OverflowException("Integer won't fit into an int");
    return (int)data[0]*sign;
  }

  public ushort ToUInt16(IFormatProvider provider)
  { if(length==0) return 0;
    if(length>1 || data[0]>ushort.MaxValue) throw new OverflowException("Integer won't fit into a ushort");
    return (ushort)data[0];
  }

  public short ToInt16(IFormatProvider provider)
  { if(length==0) return 0;
    if(length>1 || (sign>0 && data[0]>(uint)short.MaxValue) || (sign<0 && data[0]>(uint)-short.MinValue))
      throw new OverflowException("Integer won't fit into an int");
    return (short)((int)data[0]*sign);
  }

  public string ToString(IFormatProvider provider) { return ToString(); }

  public byte ToByte(IFormatProvider provider)
  { if(sign<0 || length>1 || (sign>0 && this>byte.MaxValue))
      throw new OverflowException("Integer won't fit into a byte");
    return length==0 ? 0 : (byte)data[0];
  }

  public char ToChar(IFormatProvider provider)
  { if(length==0) return '\0';
    if(length>1 || data[0]>char.MaxValue) throw new OverflowException("Integer won't fit into a char");
    return (char)data[0];
  }

  public long ToInt64(IFormatProvider provider)
  { if(sign<0 || length>2) throw new OverflowException("Integer won't fit into a long");
    if(sign==0) return 0;
    if(data.Length==1) return sign*(int)data[0];
    if((data[1]&0x80000000)!=0) throw new OverflowException("Integer won't fit into a long");
    return (long)((ulong)data[1]<<32 | data[0]);
  }

  public System.TypeCode GetTypeCode() { return TypeCode.Object; }

  public decimal ToDecimal(IFormatProvider provider)
  { 
  }

  public object ToType(Type conversionType, IFormatProvider provider)
  { if(conversionType==typeof(int)) return ToInt32(provider);
    if(conversionType==typeof(double)) return ToDouble(provider);
    if(conversionType==typeof(long)) return ToInt64(provider);
    if(conversionType==typeof(bool)) return ToBoolean(provider);
    if(conversionType==typeof(string)) return ToString(null, provider);
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

  public uint ToUInt32(IFormatProvider provider)
  { if(length>1) throw new OverflowException("Integer won't fit into a uint");
    return length==0 ? 0 : data[0];
  }
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
  internal Integer Pow(int power)
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

  internal Integer Pow(int power, object mod)
  { throw new NotImplementedException();
  }

  internal Integer Pow(Integer power) { return Pow(power.ToInt32(null)); }
  internal Integer Pow(Integer power, object mod) { return Pow(power.ToInt32(null), mod); }
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
    else for(; i<alen; i++) n[i]=x[i];
    return n;
  }

  private static void appendRadix(uint rem, uint radix, char[] tmp, System.Text.StringBuilder sb, bool leadingZeros)
  { string symbols = "0123456789abcdefghijklmnopqrstuvwxyz";
    int digits = tmp.Length;
    int i = digits;
    while(i>0 && (leadingZeros || rem!=0))
    { uint digit = rem % radix;
      rem /= radix;
      tmp[i--] = symbols[(int)digit];
    }
    if(leadingZeros) buf.Append(tmp);
    else buf.Append(tmp, i, digits-i);
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

  // stolen from elsewhere
  static uint[] maxCharsPerDigit = {0, 0, 31, 20, 15, 13, 12, 11, 10, 10, 9, 9, 8, 8, 8, 8, 7, 7, 7, 7, 7, 7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6};
  static uint[] groupRadixValues = {0, 0, 2147483648, 3486784401, 1073741824, 1220703125, 2176782336, 1977326743, 1073741824, 3486784401, 1000000000, 2357947691, 429981696, 815730721, 1475789056, 2562890625, 268435456, 410338673, 612220032, 893871739, 1280000000, 1801088541, 2494357888, 3404825447, 191102976, 244140625, 308915776, 387420489, 481890304, 594823321, 729000000, 887503681, 1073741824, 1291467969, 1544804416, 1838265625, 2176782336};
}

} // namespace Boa.Runtime
