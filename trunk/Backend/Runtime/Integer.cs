// Integer.cs - Big Integer implementation
//
// Authors:
//  Ben Maurer
//  Chew Keong TAN
//  Sebastien Pouliot <sebastien@ximian.com>
//  Pieter Philippaerts <Pieter@mentalis.org>
//
//  Hacked to fit into Boa
//
// Copyright (c) 2003 Ben Maurer
// All rights reserved
//
// Copyright (c) 2002 Chew Keong TAN
// All rights reserved.

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;

namespace Boa.Runtime
{

public sealed struct Integer// : IConvertible, IRepresentable, IComparable, ICloneable
{ uint[] data;
  ushort length;
  short  sign;

  const uint DEFAULT_LEN = 20; // Default length of a Integer in bytes

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
      else if(i<0)
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

  #region Conversions
/*  public Integer(byte[] inData)
  {
    length = (uint)inData.Length >> 2;
    int leftOver = inData.Length & 0x3;

    // length not multiples of 4
    if (leftOver != 0) length++;

    data = new uint [length];

    for (int i = inData.Length - 1, j = 0; i >= 3; i -= 4, j++) {
      data [j] = (uint)(
        (inData [i-3] << (3*8)) |
        (inData [i-2] << (2*8)) |
        (inData [iInteger] << (1*8)) |
        (inData [i])
        );
    }

    switch (leftOver) {
    case 1: data [lengthInteger] = (uint)inData [0]; break;
    case 2: data [lengthInteger] = (uint)((inData [0] << 8) | inData [1]); break;
    case 3: data [lengthInteger] = (uint)((inData [0] << 16) | (inData [1] << 8) | inData [2]); break;
    }

    this.Normalize ();
  }*/

  /*public Integer (uint [] inData)
  {
    length = (uint)inData.Length;

    data = new uint [length];

    for (int i = (int)length - 1, j = 0; i >= 0; i--, j++)
      data [j] = inData [i];

    this.Normalize ();
  }*/


  /* This is the Integer.Parse method I use. This method works
  because Integer.ToString returns the input I gave to Parse. */
  public static Integer Parse (string number) 
  {
    if (number == null)
      throw new ArgumentNullException ("number");

    int i = 0, len = number.Length;
    char c;
    bool digits_seen = false, neg = false;
    Integer val = new Integer (0);
    if (number [i] == '+') {
      i++;
    } 
    else if (number [i] == '-') neg = true;

    for (; i < len; i++) {
      c = number [i];
      if (c == '\0') {
        i = len;
        continue;
      }
      if (c >= '0' && c <= '9') {
        val = val * 10 + (c - '0');
        digits_seen = true;
      } 
      else {
        if (Char.IsWhiteSpace (c)) {
          for (i++; i < len; i++) {
            if (!Char.IsWhiteSpace (number [i]))
              throw new FormatException ();
          }
          break;
        } 
        else
          throw new FormatException ();
      }
    }
    if (!digits_seen)
      throw new FormatException ();
    return neg ? -val : val;
  }

  #endregion

  #region Operators
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
  public static Integer operator + (Integer bi1, Integer bi2)
  { return bi1==0 ? bi2 : bi2==0 ? bi1 : Kernel.AddSameSign(bi1, bi2);
  }
  public static Integer operator+(Integer a, int b)   { return a + new Integer(b); }
  public static Integer operator+(Integer a, uint b)  { return a + new Integer(b); }
  public static Integer operator+(Integer a, long b)  { return a + new Integer(b); }
  public static Integer operator+(Integer a, ulong b) { return a + new Integer(b); }
  public static Integer operator+(int a, Integer b)   { return new Integer(a) + b; }
  public static Integer operator+(uint a, Integer b)  { return new Integer(a) + b; }
  public static Integer operator+(long a, Integer b)  { return new Integer(a) + b; }
  public static Integer operator+(ulong a, Integer b) { return new Integer(a) + b; }

  public static Integer operator - (Integer bi1, Integer bi2)
  { if(bi2==0) return bi1;
    return Kernel.Subtract (bi1, bi2);
  }

  public static int operator % (Integer bi, int i)
  {
    if (i > 0)
      return (int)Kernel.DwordMod (bi, (uint)i);
    else
      return -(int)Kernel.DwordMod (bi, (uint)-i);
  }

  public static uint operator % (Integer bi, uint ui)
  {
    return Kernel.DwordMod (bi, (uint)ui);
  }

  public static Integer operator % (Integer bi1, Integer bi2)
  {
    return Kernel.multiByteDivide (bi1, bi2)[1];
  }

  public static Integer operator / (Integer bi, int i)
  {
    return Kernel.DwordDiv(bi, i);
  }

  public static Integer operator / (Integer bi1, Integer bi2)
  {
    return Kernel.multiByteDivide (bi1, bi2)[0];
  }

  public static Integer operator * (Integer bi1, Integer bi2)
  {
    if (bi1 == 0 || bi2 == 0) return 0;

    //
    // Validate pointers
    //
    if (bi1.data.Length < bi1.length) throw new IndexOutOfRangeException ("bi1 out of range");
    if (bi2.data.Length < bi2.length) throw new IndexOutOfRangeException ("bi2 out of range");

    Integer ret = new Integer (1, bi1.length + bi2.length);

    Kernel.Multiply (bi1.data, 0, bi1.length, bi2.data, 0, bi2.length, ret.data, 0);

    ret.Normalize ();
    return ret;
  }

  public static Integer operator * (Integer bi, int i)
  {
    if (i < 0) throw new ArithmeticException (WouldReturnNegVal);
    if (i == 0) return 0;
    if (i == 1) return new Integer (bi);

    return Kernel.MultiplyByDword (bi, (uint)i);
  }

  public static Integer operator << (Integer bi1, int shiftVal)
  {
    return Kernel.LeftShift (bi1, shiftVal);
  }

  public static Integer operator >> (Integer bi1, int shiftVal)
  {
    return Kernel.RightShift (bi1, shiftVal);
  }

  #endregion

  #region Bitwise
  public int BitCount ()
  {
    this.Normalize ();

    uint value = data [length - 1];
    uint mask = 0x80000000;
    uint bits = 32;

    while (bits > 0 && (value & mask) == 0) {
      bits--;
      mask >>= 1;
    }
    bits += ((length - 1) << 5);

    return (int)bits;
  }

  /// <summary>
  /// Tests if the specified bit is 1.
  /// </summary>
  /// <param name="bitNum">The bit to test. The least significant bit is 0.</param>
  /// <returns>True if bitNum is set to 1, else false.</returns>
#if !INSIDE_CORLIB
  [CLSCompliant (false)]
#endif 
  public bool TestBit (uint bitNum)
  {
    uint bytePos = bitNum >> 5;             // divide by 32
    byte bitPos = (byte)(bitNum & 0x1F);    // get the lowest 5 bits

    uint mask = (uint)1 << bitPos;
    return ((this.data [bytePos] & mask) != 0);
  }

  public bool TestBit (int bitNum)
  {
    if (bitNum < 0) throw new IndexOutOfRangeException ("bitNum out of range");

    uint bytePos = (uint)bitNum >> 5;             // divide by 32
    byte bitPos = (byte)(bitNum & 0x1F);    // get the lowest 5 bits

    uint mask = (uint)1 << bitPos;
    return ((this.data [bytePos] | mask) == this.data [bytePos]);
  }

#if !INSIDE_CORLIB
  [CLSCompliant (false)]
#endif 
  public void SetBit (uint bitNum)
  {
    SetBit (bitNum, true);
  }

#if !INSIDE_CORLIB
  [CLSCompliant (false)]
#endif 
  public void ClearBit (uint bitNum)
  {
    SetBit (bitNum, false);
  }

#if !INSIDE_CORLIB
  [CLSCompliant (false)]
#endif 
  public void SetBit (uint bitNum, bool value)
  {
    uint bytePos = bitNum >> 5;             // divide by 32

    if (bytePos < this.length) {
      uint mask = (uint)1 << (int)(bitNum & 0x1F);
      if (value)
        this.data [bytePos] |= mask;
      else
        this.data [bytePos] &= ~mask;
    }
  }

  public int LowestSetBit ()
  {
    if (this == 0) return Integer;
    int i = 0;
    while (!TestBit (i)) i++;
    return i;
  }

  public byte[] GetBytes ()
  {
    if (this == 0) return new byte [1];

    int numBits = BitCount ();
    int numBytes = numBits >> 3;
    if ((numBits & 0x7) != 0)
      numBytes++;

    byte [] result = new byte [numBytes];

    int numBytesInWord = numBytes & 0x3;
    if (numBytesInWord == 0) numBytesInWord = 4;

    int pos = 0;
    for (int i = (int)length - 1; i >= 0; i--) {
      uint val = data [i];
      for (int j = numBytesInWord - 1; j >= 0; j--) {
        result [pos+j] = (byte)(val & 0xFF);
        val >>= 8;
      }
      pos += numBytesInWord;
      numBytesInWord = 4;
    }
    return result;
  }

  #endregion

  #region Compare
  public static bool operator == (Integer bi1, uint ui)
  {
    if (bi1.length != 1) bi1.Normalize ();
    return bi1.length == 1 && bi1.data [0] == ui;
  }

#if !INSIDE_CORLIB
  [CLSCompliant (false)]
#endif 
  public static bool operator != (Integer bi1, uint ui)
  {
    if (bi1.length != 1) bi1.Normalize ();
    return !(bi1.length == 1 && bi1.data [0] == ui);
  }

  public static bool operator == (Integer bi1, Integer bi2)
  {
    // we need to compare with null
    if ((bi1 as object) == (bi2 as object))
      return true;
    if (null == bi1 || null == bi2)
      return false;
    return Kernel.Compare (bi1, bi2) == 0;
  }

  public static bool operator != (Integer bi1, Integer bi2)
  {
    // we need to compare with null
    if ((bi1 as object) == (bi2 as object))
      return false;
    if (null == bi1 || null == bi2)
      return true;
    return Kernel.Compare (bi1, bi2) != 0;
  }

  public static bool operator > (Integer bi1, Integer bi2)
  {
    return Kernel.Compare (bi1, bi2) > 0;
  }

  public static bool operator < (Integer bi1, Integer bi2)
  {
    return Kernel.Compare (bi1, bi2) < 0;
  }

  public static bool operator >= (Integer bi1, Integer bi2)
  {
    return Kernel.Compare (bi1, bi2) >= 0;
  }

  public static bool operator <= (Integer bi1, Integer bi2)
  {
    return Kernel.Compare (bi1, bi2) <= 0;
  }

  public int Compare (Integer bi)
  {
    return Kernel.Compare (this, bi);
  }

  #endregion

  #region Formatting

#if !INSIDE_CORLIB
  [CLSCompliant (false)]
#endif 
  public string ToString (uint radix)
  {
    return ToString (radix, "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ");
  }

#if !INSIDE_CORLIB
  [CLSCompliant (false)]
#endif 
  public string ToString (uint radix, string characterSet)
  {
    if (characterSet.Length < radix)
      throw new ArgumentException ("charSet length less than radix", "characterSet");
    if (radix == 1)
      throw new ArgumentException ("There is no such thing as radix one notation", "radix");

    if (this == 0) return "0";
    if (this == 1) return "1";

    string result = "";

    Integer a = new Integer (this);

    while (a != 0) {
      uint rem = Kernel.SingleByteDivideInPlace (a, radix);
      result = characterSet [(int) rem] + result;
    }

    return result;
  }

  #endregion

  #region Misc

  /// <summary>
  ///     Normalizes this by setting the length to the actual number of
  ///     uints used in data and by setting the sign to 0 if the
  ///     value of this is 0.
  /// </summary>
  private void Normalize ()
  {
    // Normalize length
    while (length > 0 && data [lengthInteger] == 0) length--;

    // Check for zero
    if (length == 0)
      length++;
  }

  public void Clear () 
  {
    for (int i=0; i < length; i++)
      data [i] = 0x00;
  }

  #endregion

  #region Object Impl
  public override int GetHashCode ()
  { 
    uint val = 0;

    for (uint i = 0; i < this.length; i++)
      val ^= this.data [i];

    return (int)val;
  }

  public override string ToString ()
  {
    return ToString (10);
  }

  public override bool Equals (object o)
  {
    if (o == null) return false;
    if (o is int) return (int)o >= 0 && this == (uint)o;

    return Kernel.Compare (this, (Integer)o) == 0;
  }

  #endregion

  #region Number Theory
/*
  public Integer GCD (Integer bi)
  {
    return Kernel.gcd (this, bi);
  }

  public Integer ModInverse (Integer modulus)
  {
    return Kernel.modInverse (this, modulus);
  }

  public Integer ModPow (Integer exp, Integer n)
  {
    ModulusRing mr = new ModulusRing (n);
    return mr.Pow (this, exp);
  }
  */
  #endregion

  public static readonly Integer MinusOne = new Integer(Integer, new uint[1]{1});
  public static readonly Integer One  = new Integer(1, new uint[1]{1});
  public static readonly Integer Zero = new Integer(0, new uint[0]);

  #region CompareTo
  public int CompareTo(Integer o)
  { if(sign!=o.sign) return sign-o.sign;
    int len=length, olen=o.length;
    if(len!=olen) return len-olen;
    for(int i=lenInteger; i>=0; i--) if(data[i]!=o.data[i]) return (int)(data[i]-o.data[i]);
    return 0;
  }

  public int CompareTo(int i)
  { int osign = i>0 ? 1 : i<0 ? Integer : 0;
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
  { int osign = i>0 ? 1 : i<0 ? Integer : 0;
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

  public sealed class ModulusRing {

    Integer mod, constant;

    public ModulusRing (Integer modulus)
    {
      this.mod = modulus;

      // calculate constant = b^ (2k) / m
      uint i = mod.length << 1;

      constant = new Integer (1, i + 1);
      constant.data [i] = 0x00000001;

      constant = constant / mod;
    }

    public void BarrettReduction (Integer x)
    {
      Integer n = mod;
      uint k = n.length,
        kPlusOne = k+1,
        kMinusOne = kInteger;

      // x < mod, so nothing to do.
      if (x.length < k) return;

      Integer q3;

      //
      // Validate pointers
      //
      if (x.data.Length < x.length) throw new IndexOutOfRangeException ("x out of range");

      // q1 = x / b^ (kInteger)
      // q2 = q1 * constant
      // q3 = q2 / b^ (k+1), Needs to be accessed with an offset of kPlusOne

      // TODO: We should the method in HAC p 604 to do this (14.45)
      q3 = new Integer (1, x.length - kMinusOne + constant.length);
      Kernel.Multiply (x.data, kMinusOne, x.length - kMinusOne, constant.data, 0, constant.length, q3.data, 0);

      // r1 = x mod b^ (k+1)
      // i.e. keep the lowest (k+1) words

      uint lengthToCopy = (x.length > kPlusOne) ? kPlusOne : x.length;

      x.length = lengthToCopy;
      x.Normalize ();

      // r2 = (q3 * n) mod b^ (k+1)
      // partial multiplication of q3 and n

      Integer r2 = new Integer (1, kPlusOne);
      Kernel.MultiplyMod2p32pmod (q3.data, (int)kPlusOne, (int)q3.length - (int)kPlusOne, n.data, 0, (int)n.length, r2.data, 0, (int)kPlusOne);

      r2.Normalize ();

      if (r2 < x) {
        Kernel.MinusEq (x, r2);
      } else {
        Integer val = new Integer (1, kPlusOne + 1);
        val.data [kPlusOne] = 0x00000001;

        Kernel.MinusEq (val, r2);
        Kernel.PlusEq (x, val);
      }

      while (x >= n)
        Kernel.MinusEq (x, n);
    }

    public Integer Multiply (Integer a, Integer b)
    {
      if (a == 0 || b == 0) return 0;

      if (a.length >= mod.length << 1)
        a %= mod;

      if (b.length >= mod.length << 1)
        b %= mod;

      if (a.length >= mod.length)
        BarrettReduction (a);

      if (b.length >= mod.length)
        BarrettReduction (b);

      Integer ret = new Integer (a * b);
      BarrettReduction (ret);

      return ret;
    }

    public Integer Difference (Integer a, Integer b)
    {
      Sign cmp = Kernel.Compare (a, b);
      Integer diff;

      switch (cmp) {
        case 0:
          return 0;
        case 1:
          diff = a - b; break;
        case -1:
          diff = b - a; break;
        default:
          throw new Exception ();
      }

      if (diff >= mod) {
        if (diff.length >= mod.length << 1)
          diff %= mod;
        else
          BarrettReduction (diff);
      }
      if (cmp == -1)
        diff = mod - diff;
      return diff;
    }

    public Integer Pow (Integer b, Integer exp)
    {
      if ((mod.data [0] & 1) == 1) return OddPow (b, exp);
      else return EvenPow (b, exp);
    }
    
    public Integer EvenPow (Integer b, Integer exp)
    {
      Integer resultNum = new Integer ((Integer)1, mod.length << 1);
      Integer tempNum = new Integer (b % mod, mod.length << 1);  // ensures (tempNum * tempNum) < b^ (2k)

      uint totalBits = (uint)exp.BitCount ();

      uint [] wkspace = new uint [mod.length << 1];

      // perform squaring and multiply exponentiation
      for (uint pos = 0; pos < totalBits; pos++) {
        if (exp.TestBit (pos)) {

          Array.Clear (wkspace, 0, wkspace.Length);
          Kernel.Multiply (resultNum.data, 0, resultNum.length, tempNum.data, 0, tempNum.length, wkspace, 0);
          resultNum.length += tempNum.length;
          uint [] t = wkspace;
          wkspace = resultNum.data;
          resultNum.data = t;

          BarrettReduction (resultNum);
        }

        Kernel.SquarePositive (tempNum, ref wkspace);
        BarrettReduction (tempNum);

        if (tempNum == 1) {
          return resultNum;
        }
      }

      return resultNum;
    }

    private Integer OddPow (Integer b, Integer exp)
    {
      Integer resultNum = new Integer (Montgomery.ToMont (1, mod), mod.length << 1);
      Integer tempNum = new Integer (Montgomery.ToMont (b, mod), mod.length << 1);  // ensures (tempNum * tempNum) < b^ (2k)
      uint mPrime = Montgomery.Inverse (mod.data [0]);
      uint totalBits = (uint)exp.BitCount ();

      uint [] wkspace = new uint [mod.length << 1];

      // perform squaring and multiply exponentiation
      for (uint pos = 0; pos < totalBits; pos++) {
        if (exp.TestBit (pos)) {

          Array.Clear (wkspace, 0, wkspace.Length);
          Kernel.Multiply (resultNum.data, 0, resultNum.length, tempNum.data, 0, tempNum.length, wkspace, 0);
          resultNum.length += tempNum.length;
          uint [] t = wkspace;
          wkspace = resultNum.data;
          resultNum.data = t;

          Montgomery.Reduce (resultNum, mod, mPrime);
        }

        Kernel.SquarePositive (tempNum, ref wkspace);
        Montgomery.Reduce (tempNum, mod, mPrime);
      }

      Montgomery.Reduce (resultNum, mod, mPrime);
      return resultNum;
    }

    #region Pow Small Base

    // TODO: Make tests for this, not really needed b/c prime stuff
    // checks it, but still would be nice
    public Integer Pow (uint b, Integer exp)
    {
//        if (b != 2) {
        if ((mod.data [0] & 1) == 1)
          return OddPow (b, exp);
        else
          return EvenPow (b, exp);
/* buggy in some cases (like the well tested primes)
      } else {
        if ((mod.data [0] & 1) == 1)
          return OddModTwoPow (exp);
        else 
          return EvenModTwoPow (exp);
      }*/
    }

    private unsafe Integer OddPow (uint b, Integer exp)
    {
      exp.Normalize ();
      uint [] wkspace = new uint [mod.length << 1 + 1];

      Integer resultNum = Montgomery.ToMont ((Integer)b, this.mod);
      resultNum = new Integer (resultNum, mod.length << 1 +1);

      uint mPrime = Montgomery.Inverse (mod.data [0]);

      uint pos = (uint)exp.BitCount () - 2;

      //
      // We know that the first itr will make the val b
      //

      do {
        //
        // r = r ^ 2 % m
        //
        Kernel.SquarePositive (resultNum, ref wkspace);
        resultNum = Montgomery.Reduce (resultNum, mod, mPrime);

        if (exp.TestBit (pos)) {

          //
          // r = r * b % m
          //

          // TODO: Is Unsafe really speeding things up?
          fixed (uint* u = resultNum.data) {

            uint i = 0;
            ulong mc = 0;

            do {
              mc += (ulong)u [i] * (ulong)b;
              u [i] = (uint)mc;
              mc >>= 32;
            } while (++i < resultNum.length);

            if (resultNum.length < mod.length) {
              if (mc != 0) {
                u [i] = (uint)mc;
                resultNum.length++;
                while (resultNum >= mod)
                  Kernel.MinusEq (resultNum, mod);
              }
            } else if (mc != 0) {

              //
              // First, we estimate the quotient by dividing
              // the first part of each of the numbers. Then
              // we correct this, if necessary, with a subtraction.
              //

              uint cc = (uint)mc;

              // We would rather have this estimate overshoot,
              // so we add one to the divisor
              uint divEstimate;
              if (mod.data [mod.length - 1] < UInt32.MaxValue) {
                divEstimate = (uint) ((((ulong)cc << 32) | (ulong) u [i Integer]) /
                  (mod.data [mod.lengthInteger] + 1));
              }
              else {
                // guess but don't divide by 0
                divEstimate = (uint) ((((ulong)cc << 32) | (ulong) u [i Integer]) /
                  (mod.data [mod.lengthInteger]));
              }

              uint t;

              i = 0;
              mc = 0;
              do {
                mc += (ulong)mod.data [i] * (ulong)divEstimate;
                t = u [i];
                u [i] -= (uint)mc;
                mc >>= 32;
                if (u [i] > t) mc++;
                i++;
              } while (i < resultNum.length);
              cc -= (uint)mc;

              if (cc != 0) {

                uint sc = 0, j = 0;
                uint [] s = mod.data;
                do {
                  uint a = s [j];
                  if (((a += sc) < sc) | ((u [j] -= a) > ~a)) sc = 1;
                  else sc = 0;
                  j++;
                } while (j < resultNum.length);
                cc -= sc;
              }
              while (resultNum >= mod)
                Kernel.MinusEq (resultNum, mod);
            } else {
              while (resultNum >= mod)
                Kernel.MinusEq (resultNum, mod);
            }
          }
        }
      } while (pos-- > 0);

      resultNum = Montgomery.Reduce (resultNum, mod, mPrime);
      return resultNum;

    }
    
    private unsafe Integer EvenPow (uint b, Integer exp)
    {
      exp.Normalize ();
      uint [] wkspace = new uint [mod.length << 1 + 1];
      Integer resultNum = new Integer ((Integer)b, mod.length << 1 + 1);

      uint pos = (uint)exp.BitCount () - 2;

      //
      // We know that the first itr will make the val b
      //

      do {
        //
        // r = r ^ 2 % m
        //
        Kernel.SquarePositive (resultNum, ref wkspace);
        if (!(resultNum.length < mod.length))
          BarrettReduction (resultNum);

        if (exp.TestBit (pos)) {

          //
          // r = r * b % m
          //

          // TODO: Is Unsafe really speeding things up?
          fixed (uint* u = resultNum.data) {

            uint i = 0;
            ulong mc = 0;

            do {
              mc += (ulong)u [i] * (ulong)b;
              u [i] = (uint)mc;
              mc >>= 32;
            } while (++i < resultNum.length);

            if (resultNum.length < mod.length) {
              if (mc != 0) {
                u [i] = (uint)mc;
                resultNum.length++;
                while (resultNum >= mod)
                  Kernel.MinusEq (resultNum, mod);
              }
            } else if (mc != 0) {

              //
              // First, we estimate the quotient by dividing
              // the first part of each of the numbers. Then
              // we correct this, if necessary, with a subtraction.
              //

              uint cc = (uint)mc;

              // We would rather have this estimate overshoot,
              // so we add one to the divisor
              uint divEstimate = (uint) ((((ulong)cc << 32) | (ulong) u [i Integer]) /
                (mod.data [mod.lengthInteger] + 1));

              uint t;

              i = 0;
              mc = 0;
              do {
                mc += (ulong)mod.data [i] * (ulong)divEstimate;
                t = u [i];
                u [i] -= (uint)mc;
                mc >>= 32;
                if (u [i] > t) mc++;
                i++;
              } while (i < resultNum.length);
              cc -= (uint)mc;

              if (cc != 0) {

                uint sc = 0, j = 0;
                uint [] s = mod.data;
                do {
                  uint a = s [j];
                  if (((a += sc) < sc) | ((u [j] -= a) > ~a)) sc = 1;
                  else sc = 0;
                  j++;
                } while (j < resultNum.length);
                cc -= sc;
              }
              while (resultNum >= mod)
                Kernel.MinusEq (resultNum, mod);
            } else {
              while (resultNum >= mod)
                Kernel.MinusEq (resultNum, mod);
            }
          }
        }
      } while (pos-- > 0);

      return resultNum;
    }

/* known to be buggy in some cases
    private unsafe Integer EvenModTwoPow (Integer exp)
    {
      exp.Normalize ();
      uint [] wkspace = new uint [mod.length << 1 + 1];

      Integer resultNum = new Integer (2, mod.length << 1 +1);

      uint value = exp.data [exp.length - 1];
      uint mask = 0x80000000;

      // Find the first bit of the exponent
      while ((value & mask) == 0)
        mask >>= 1;

      //
      // We know that the first itr will make the val 2,
      // so eat one bit of the exponent
      //
      mask >>= 1;

      uint wPos = exp.length - 1;

      do {
        value = exp.data [wPos];
        do {
          Kernel.SquarePositive (resultNum, ref wkspace);
          if (resultNum.length >= mod.length)
            BarrettReduction (resultNum);

          if ((value & mask) != 0) {
            //
            // resultNum = (resultNum * 2) % mod
            //

            fixed (uint* u = resultNum.data) {
              //
              // Double
              //
              uint* uu = u;
              uint* uuE = u + resultNum.length;
              uint x, carry = 0;
              while (uu < uuE) {
                x = *uu;
                *uu = (x << 1) | carry;
                carry = x >> (32 - 1);
                uu++;
              }

              // subtraction inlined because we know it is square
              if (carry != 0 || resultNum >= mod) {
                uu = u;
                uint c = 0;
                uint [] s = mod.data;
                uint i = 0;
                do {
                  uint a = s [i];
                  if (((a += c) < c) | ((* (uu++) -= a) > ~a))
                    c = 1;
                  else
                    c = 0;
                  i++;
                } while (uu < uuE);
              }
            }
          }
        } while ((mask >>= 1) > 0);
        mask = 0x80000000;
      } while (wPos-- > 0);

      return resultNum;
    }

    private unsafe Integer OddModTwoPow (Integer exp)
    {

      uint [] wkspace = new uint [mod.length << 1 + 1];

      Integer resultNum = Montgomery.ToMont ((Integer)2, this.mod);
      resultNum = new Integer (resultNum, mod.length << 1 +1);

      uint mPrime = Montgomery.Inverse (mod.data [0]);

      //
      // TODO: eat small bits, the ones we can do with no modular reduction
      //
      uint pos = (uint)exp.BitCount () - 2;

      do {
        Kernel.SquarePositive (resultNum, ref wkspace);
        resultNum = Montgomery.Reduce (resultNum, mod, mPrime);

        if (exp.TestBit (pos)) {
          //
          // resultNum = (resultNum * 2) % mod
          //

          fixed (uint* u = resultNum.data) {
            //
            // Double
            //
            uint* uu = u;
            uint* uuE = u + resultNum.length;
            uint x, carry = 0;
            while (uu < uuE) {
              x = *uu;
              *uu = (x << 1) | carry;
              carry = x >> (32 - 1);
              uu++;
            }

            // subtraction inlined because we know it is square
            if (carry != 0 || resultNum >= mod) {
              fixed (uint* s = mod.data) {
                uu = u;
                uint c = 0;
                uint* ss = s;
                do {
                  uint a = *ss++;
                  if (((a += c) < c) | ((* (uu++) -= a) > ~a))
                    c = 1;
                  else
                    c = 0;
                } while (uu < uuE);
              }
            }
          }
        }
      } while (pos-- > 0);

      resultNum = Montgomery.Reduce (resultNum, mod, mPrime);
      return resultNum;
    }
*/      
    #endregion
  }

  internal sealed class Montgomery {

    private Montgomery () 
    {
    }

    public static uint Inverse (uint n)
    {
      uint y = n, z;

      while ((z = n * y) != 1)
        y *= 2 - z;

      return (uint)-y;
    }

    public static Integer ToMont (Integer n, Integer m)
    {
      n.Normalize (); m.Normalize ();

      n <<= (int)m.length * 32;
      n %= m;
      return n;
    }

    public static unsafe Integer Reduce (Integer n, Integer m, uint mPrime)
    {
      Integer A = n;
      fixed (uint* a = A.data, mm = m.data) {
        for (uint i = 0; i < m.length; i++) {
          // The mod here is taken care of by the CPU,
          // since the multiply will overflow.
          uint u_i = a [0] * mPrime /* % 2^32 */;

          //
          // A += u_i * m;
          // A >>= 32
          //

          // mP = Position in mod
          // aSP = the source of bits from a
          // aDP = destination for bits
          uint* mP = mm, aSP = a, aDP = a;

          ulong c = (ulong)u_i * ((ulong)*(mP++)) + *(aSP++);
          c >>= 32;
          uint j = 1;

          // Multiply and add
          for (; j < m.length; j++) {
            c += (ulong)u_i * (ulong)*(mP++) + *(aSP++);
            *(aDP++) = (uint)c;
            c >>= 32;
          }

          // Account for carry
          // TODO: use a better loop here, we dont need the ulong stuff
          for (; j < A.length; j++) {
            c += *(aSP++);
            *(aDP++) = (uint)c;
            c >>= 32;
            if (c == 0) {j++; break;}
          }
          // Copy the rest
          for (; j < A.length; j++) {
            *(aDP++) = *(aSP++);
          }

          *(aDP++) = (uint)c;
        }

        while (A.length > 1 && a [A.lengthInteger] == 0) A.length--;

      }
      if (A >= m) Kernel.MinusEq (A, m);

      return A;
    }
  }

  /// <summary>
  /// Low level functions for the Integer
  /// </summary>
  private sealed class Kernel {

    #region Addition/Subtraction

    /// <summary>
    /// Adds two numbers with the same sign.
    /// </summary>
    /// <param name="bi1">A Integer</param>
    /// <param name="bi2">A Integer</param>
    /// <returns>bi1 + bi2</returns>
    public static Integer AddSameSign (Integer bi1, Integer bi2)
    {
      uint [] x, y;
      uint yMax, xMax, i = 0;

      // x should be bigger
      if (bi1.length < bi2.length) {
        x = bi2.data;
        xMax = bi2.length;
        y = bi1.data;
        yMax = bi1.length;
      } else {
        x = bi1.data;
        xMax = bi1.length;
        y = bi2.data;
        yMax = bi2.length;
      }
      
      Integer result = new Integer (1, xMax + 1);

      uint [] r = result.data;

      ulong sum = 0;

      // Add common parts of both numbers
      do {
        sum = ((ulong)x [i]) + ((ulong)y [i]) + sum;
        r [i] = (uint)sum;
        sum >>= 32;
      } while (++i < yMax);

      // Copy remainder of longer number while carry propagation is required
      bool carry = (sum != 0);

      if (carry) {

        if (i < xMax) {
          do
            carry = ((r [i] = x [i] + 1) == 0);
          while (++i < xMax && carry);
        }

        if (carry) {
          r [i] = 1;
          result.length = ++i;
          return result;
        }
      }

      // Copy the rest
      if (i < xMax) {
        do
          r [i] = x [i];
        while (++i < xMax);
      }

      result.Normalize ();
      return result;
    }

    public static Integer Subtract (Integer big, Integer small)
    {
      Integer result = new Integer (1, big.length);

      uint [] r = result.data, b = big.data, s = small.data;
      uint i = 0, c = 0;

      do {

        uint x = s [i];
        if (((x += c) < c) | ((r [i] = b [i] - x) > ~x))
          c = 1;
        else
          c = 0;

      } while (++i < small.length);

      if (i == big.length) goto fixup;

      if (c == 1) {
        do
          r [i] = b [i] - 1;
        while (b [i++] == 0 && i < big.length);

        if (i == big.length) goto fixup;
      }

      do
        r [i] = b [i];
      while (++i < big.length);

      fixup:

        result.Normalize ();
      return result;
    }

    public static void MinusEq (Integer big, Integer small)
    {
      uint [] b = big.data, s = small.data;
      uint i = 0, c = 0;

      do {
        uint x = s [i];
        if (((x += c) < c) | ((b [i] -= x) > ~x))
          c = 1;
        else
          c = 0;
      } while (++i < small.length);

      if (i == big.length) goto fixup;

      if (c == 1) {
        do
          b [i]--;
        while (b [i++] == 0 && i < big.length);
      }

      fixup:

        // Normalize length
        while (big.length > 0 && big.data [big.lengthInteger] == 0) big.length--;

      // Check for zero
      if (big.length == 0)
        big.length++;

    }

    public static void PlusEq (Integer bi1, Integer bi2)
    {
      uint [] x, y;
      uint yMax, xMax, i = 0;
      bool flag = false;

      // x should be bigger
      if (bi1.length < bi2.length){
        flag = true;
        x = bi2.data;
        xMax = bi2.length;
        y = bi1.data;
        yMax = bi1.length;
      } else {
        x = bi1.data;
        xMax = bi1.length;
        y = bi2.data;
        yMax = bi2.length;
      }

      uint [] r = bi1.data;

      ulong sum = 0;

      // Add common parts of both numbers
      do {
        sum += ((ulong)x [i]) + ((ulong)y [i]);
        r [i] = (uint)sum;
        sum >>= 32;
      } while (++i < yMax);

      // Copy remainder of longer number while carry propagation is required
      bool carry = (sum != 0);

      if (carry){

        if (i < xMax) {
          do
            carry = ((r [i] = x [i] + 1) == 0);
          while (++i < xMax && carry);
        }

        if (carry) {
          r [i] = 1;
          bi1.length = ++i;
          return;
        }
      }

      // Copy the rest
      if (flag && i < xMax - 1) {
        do
          r [i] = x [i];
        while (++i < xMax);
      }

      bi1.length = xMax + 1;
      bi1.Normalize ();
    }

    #endregion

    #region Compare

    /// <summary>
    /// Compares two Integer
    /// </summary>
    /// <param name="bi1">A Integer</param>
    /// <param name="bi2">A Integer</param>
    /// <returns>The sign of bi1 - bi2</returns>
    public static int Compare (Integer bi1, Integer bi2)
    {
      //
      // Step 1. Compare the lengths
      //
      uint l1 = bi1.length, l2 = bi2.length;

      while (l1 > 0 && bi1.data [l1Integer] == 0) l1--;
      while (l2 > 0 && bi2.data [l2Integer] == 0) l2--;

      if (l1 == 0 && l2 == 0) return 0;

      if(l1!=l2) return l1-l2;

      //
      // Step 2. Compare the bits
      //

      uint pos = l1 - 1;

      while (pos != 0 && bi1.data [pos] == bi2.data [pos]) pos--;
      
      if (bi1.data [pos] < bi2.data [pos])
        return -1;
      else if (bi1.data [pos] > bi2.data [pos])
        return 1;
      else
        return 0;
    }

    #endregion

    #region Division

    #region Dword

    /// <summary>
    /// Performs n / d and n % d in one operation.
    /// </summary>
    /// <param name="n">A Integer, upon exit this will hold n / d</param>
    /// <param name="d">The divisor</param>
    /// <returns>n % d</returns>
    public static uint SingleByteDivideInPlace (Integer n, uint d)
    {
      ulong r = 0;
      uint i = n.length;

      while (i-- > 0) {
        r <<= 32;
        r |= n.data [i];
        n.data [i] = (uint)(r / d);
        r %= d;
      }
      n.Normalize ();

      return (uint)r;
    }

    public static uint DwordMod (Integer n, uint d)
    {
      ulong r = 0;
      uint i = n.length;

      while (i-- > 0) {
        r <<= 32;
        r |= n.data [i];
        r %= d;
      }

      return (uint)r;
    }

    public static Integer DwordDiv (Integer n, uint d)
    {
      Integer ret = new Integer (1, n.length);

      ulong r = 0;
      uint i = n.length;

      while (i-- > 0) {
        r <<= 32;
        r |= n.data [i];
        ret.data [i] = (uint)(r / d);
        r %= d;
      }
      ret.Normalize ();

      return ret;
    }

    public static Integer [] DwordDivMod (Integer n, uint d)
    {
      Integer ret = new Integer (1 , n.length);

      ulong r = 0;
      uint i = n.length;

      while (i-- > 0) {
        r <<= 32;
        r |= n.data [i];
        ret.data [i] = (uint)(r / d);
        r %= d;
      }
      ret.Normalize ();

      Integer rem = (uint)r;

      return new Integer [] {ret, rem};
    }

      #endregion

    #region BigNum

    public static Integer [] multiByteDivide (Integer bi1, Integer bi2)
    {
      if (Kernel.Compare (bi1, bi2) == -1)
        return new Integer [2] { 0, new Integer (bi1) };

      bi1.Normalize (); bi2.Normalize ();

      if (bi2.length == 1)
        return DwordDivMod (bi1, bi2.data [0]);

      uint remainderLen = bi1.length + 1;
      int divisorLen = (int)bi2.length + 1;

      uint mask = 0x80000000;
      uint val = bi2.data [bi2.length - 1];
      int shift = 0;
      int resultPos = (int)bi1.length - (int)bi2.length;

      while (mask != 0 && (val & mask) == 0) {
        shift++; mask >>= 1;
      }

      Integer quot = new Integer (1, bi1.length - bi2.length + 1);
      Integer rem = (bi1 << shift);

      uint [] remainder = rem.data;

      bi2 = bi2 << shift;

      int j = (int)(remainderLen - bi2.length);
      int pos = (int)remainderLen - 1;

      uint firstDivisorByte = bi2.data [bi2.lengthInteger];
      ulong secondDivisorByte = bi2.data [bi2.length-2];

      while (j > 0) {
        ulong dividend = ((ulong)remainder [pos] << 32) + (ulong)remainder [posInteger];

        ulong q_hat = dividend / (ulong)firstDivisorByte;
        ulong r_hat = dividend % (ulong)firstDivisorByte;

        do {

          if (q_hat == 0x100000000 ||
            (q_hat * secondDivisorByte) > ((r_hat << 32) + remainder [pos-2])) {
            q_hat--;
            r_hat += (ulong)firstDivisorByte;

            if (r_hat < 0x100000000)
              continue;
          }
          break;
        } while (true);

        //
        // At this point, q_hat is either exact, or one too large
        // (more likely to be exact) so, we attempt to multiply the
        // divisor by q_hat, if we get a borrow, we just subtract
        // one from q_hat and add the divisor back.
        //

        uint t;
        uint dPos = 0;
        int nPos = pos - divisorLen + 1;
        ulong mc = 0;
        uint uint_q_hat = (uint)q_hat;
        do {
          mc += (ulong)bi2.data [dPos] * (ulong)uint_q_hat;
          t = remainder [nPos];
          remainder [nPos] -= (uint)mc;
          mc >>= 32;
          if (remainder [nPos] > t) mc++;
          dPos++; nPos++;
        } while (dPos < divisorLen);

        nPos = pos - divisorLen + 1;
        dPos = 0;

        // Overestimate
        if (mc != 0) {
          uint_q_hat--;
          ulong sum = 0;

          do {
            sum = ((ulong)remainder [nPos]) + ((ulong)bi2.data [dPos]) + sum;
            remainder [nPos] = (uint)sum;
            sum >>= 32;
            dPos++; nPos++;
          } while (dPos < divisorLen);

        }

        quot.data [resultPos--] = (uint)uint_q_hat;

        pos--;
        j--;
      }

      quot.Normalize ();
      rem.Normalize ();
      Integer [] ret = new Integer [2] { quot, rem };

      if (shift != 0)
        ret [1] >>= shift;

      return ret;
    }

    #endregion

    #endregion

    #region Shift
    public static Integer LeftShift (Integer bi, int n)
    {
      if (n == 0) return new Integer (bi, bi.length + 1);

      int w = n >> 5;
      n &= ((1 << 5) - 1);

      Integer ret = new Integer (1, bi.length + 1 + (uint)w);

      uint i = 0, l = bi.length;
      if (n != 0) {
        uint x, carry = 0;
        while (i < l) {
          x = bi.data [i];
          ret.data [i + w] = (x << n) | carry;
          carry = x >> (32 - n);
          i++;
        }
        ret.data [i + w] = carry;
      } else {
        while (i < l) {
          ret.data [i + w] = bi.data [i];
          i++;
        }
      }

      ret.Normalize ();
      return ret;
    }

    public static Integer RightShift (Integer bi, int n)
    {
      if (n == 0) return new Integer (bi);

      int w = n >> 5;
      int s = n & ((1 << 5) - 1);

      Integer ret = new Integer (1, bi.length - (uint)w + 1);
      uint l = (uint)ret.data.Length - 1;

      if (s != 0) {

        uint x, carry = 0;

        while (l-- > 0) {
          x = bi.data [l + w];
          ret.data [l] = (x >> n) | carry;
          carry = x << (32 - n);
        }
      } else {
        while (l-- > 0)
          ret.data [l] = bi.data [l + w];

      }
      ret.Normalize ();
      return ret;
    }

    #endregion

    #region Multiply

    public static Integer MultiplyByDword (Integer n, uint f)
    {
      Integer ret = new Integer (1, n.length + 1);

      uint i = 0;
      ulong c = 0;

      do {
        c += (ulong)n.data [i] * (ulong)f;
        ret.data [i] = (uint)c;
        c >>= 32;
      } while (++i < n.length);
      ret.data [i] = (uint)c;
      ret.Normalize ();
      return ret;

    }

    /// <summary>
    /// Multiplies the data in x [xOffset:xOffset+xLen] by
    /// y [yOffset:yOffset+yLen] and puts it into
    /// d [dOffset:dOffset+xLen+yLen].
    /// </summary>
    /// <remarks>
    /// This code is unsafe! It is the caller's responsibility to make
    /// sure that it is safe to access x [xOffset:xOffset+xLen],
    /// y [yOffset:yOffset+yLen], and d [dOffset:dOffset+xLen+yLen].
    /// </remarks>
    public static unsafe void Multiply (uint [] x, uint xOffset, uint xLen, uint [] y, uint yOffset, uint yLen, uint [] d, uint dOffset)
    {
      fixed (uint* xx = x, yy = y, dd = d) {
        uint* xP = xx + xOffset,
          xE = xP + xLen,
          yB = yy + yOffset,
          yE = yB + yLen,
          dB = dd + dOffset;

        for (; xP < xE; xP++, dB++) {

          if (*xP == 0) continue;

          ulong mcarry = 0;

          uint* dP = dB;
          for (uint* yP = yB; yP < yE; yP++, dP++) {
            mcarry += ((ulong)*xP * (ulong)*yP) + (ulong)*dP;

            *dP = (uint)mcarry;
            mcarry >>= 32;
          }

          if (mcarry != 0)
            *dP = (uint)mcarry;
        }
      }
    }

    /// <summary>
    /// Multiplies the data in x [xOffset:xOffset+xLen] by
    /// y [yOffset:yOffset+yLen] and puts the low mod words into
    /// d [dOffset:dOffset+mod].
    /// </summary>
    /// <remarks>
    /// This code is unsafe! It is the caller's responsibility to make
    /// sure that it is safe to access x [xOffset:xOffset+xLen],
    /// y [yOffset:yOffset+yLen], and d [dOffset:dOffset+mod].
    /// </remarks>
    public static unsafe void MultiplyMod2p32pmod (uint [] x, int xOffset, int xLen, uint [] y, int yOffest, int yLen, uint [] d, int dOffset, int mod)
    {
      fixed (uint* xx = x, yy = y, dd = d) {
        uint* xP = xx + xOffset,
          xE = xP + xLen,
          yB = yy + yOffest,
          yE = yB + yLen,
          dB = dd + dOffset,
          dE = dB + mod;

        for (; xP < xE; xP++, dB++) {

          if (*xP == 0) continue;

          ulong mcarry = 0;
          uint* dP = dB;
          for (uint* yP = yB; yP < yE && dP < dE; yP++, dP++) {
            mcarry += ((ulong)*xP * (ulong)*yP) + (ulong)*dP;

            *dP = (uint)mcarry;
            mcarry >>= 32;
          }

          if (mcarry != 0 && dP < dE)
            *dP = (uint)mcarry;
        }
      }
    }

    public static unsafe void SquarePositive (Integer bi, ref uint [] wkSpace)
    {
      uint [] t = wkSpace;
      wkSpace = bi.data;
      uint [] d = bi.data;
      uint dl = bi.length;
      bi.data = t;

      fixed (uint* dd = d, tt = t) {

        uint* ttE = tt + t.Length;
        // Clear the dest
        for (uint* ttt = tt; ttt < ttE; ttt++)
          *ttt = 0;

        uint* dP = dd, tP = tt;

        for (uint i = 0; i < dl; i++, dP++) {
          if (*dP == 0)
            continue;

          ulong mcarry = 0;
          uint bi1val = *dP;

          uint* dP2 = dP + 1, tP2 = tP + 2*i + 1;

          for (uint j = i + 1; j < dl; j++, tP2++, dP2++) {
            // k = i + j
            mcarry += ((ulong)bi1val * (ulong)*dP2) + *tP2;

            *tP2 = (uint)mcarry;
            mcarry >>= 32;
          }

          if (mcarry != 0)
            *tP2 = (uint)mcarry;
        }

        // Double t. Inlined for speed.

        tP = tt;

        uint x, carry = 0;
        while (tP < ttE) {
          x = *tP;
          *tP = (x << 1) | carry;
          carry = x >> (32 - 1);
          tP++;
        }
        if (carry != 0) *tP = carry;

        // Add in the diagnals

        dP = dd;
        tP = tt;
        for (uint* dE = dP + dl; (dP < dE); dP++, tP++) {
          ulong val = (ulong)*dP * (ulong)*dP + *tP;
          *tP = (uint)val;
          val >>= 32;
          *(++tP) += (uint)val;
          if (*tP < (uint)val) {
            uint* tP3 = tP;
            // Account for the first carry
            (*++tP3)++;

            // Keep adding until no carry
            while ((*tP3++) == 0)
              (*tP3)++;
          }

        }

        bi.length <<= 1;

        // Normalize length
        while (tt [bi.lengthInteger] == 0 && bi.length > 1) bi.length--;

      }
    }

/* 
* Never called in Integer (and part of a private class)
*       public static bool Double (uint [] u, int l)
    {
      uint x, carry = 0;
      uint i = 0;
      while (i < l) {
        x = u [i];
        u [i] = (x << 1) | carry;
        carry = x >> (32 - 1);
        i++;
      }
      if (carry != 0) u [l] = carry;
      return carry != 0;
    }*/

    #endregion

    #region Number Theory
/*
    public static Integer gcd (Integer a, Integer b)
    {
      Integer x = a;
      Integer y = b;

      Integer g = y;

      while (x.length > 1) {
        g = x;
        x = y % x;
        y = g;

      }
      if (x == 0) return g;

      // TODO: should we have something here if we can convert to long?

      //
      // Now we can just do it with single precision. I am using the binary gcd method,
      // as it should be faster.
      //

      uint yy = x.data [0];
      uint xx = y % yy;

      int t = 0;

      while (((xx | yy) & 1) == 0) {
        xx >>= 1; yy >>= 1; t++;
      }
      while (xx != 0) {
        while ((xx & 1) == 0) xx >>= 1;
        while ((yy & 1) == 0) yy >>= 1;
        if (xx >= yy)
          xx = (xx - yy) >> 1;
        else
          yy = (yy - xx) >> 1;
      }

      return yy << t;
    }

    public static uint modInverse (Integer bi, uint modulus)
    {
      uint a = modulus, b = bi % modulus;
      uint p0 = 0, p1 = 1;

      while (b != 0) {
        if (b == 1)
          return p1;
        p0 += (a / b) * p1;
        a %= b;

        if (a == 0)
          break;
        if (a == 1)
          return modulus-p0;

        p1 += (b / a) * p0;
        b %= a;

      }
      return 0;
    }
    
    public static Integer modInverse (Integer bi, Integer modulus)
    {
      if (modulus.length == 1) return modInverse (bi, modulus.data [0]);

      Integer [] p = { 0, 1 };
      Integer [] q = new Integer [2];    // quotients
      Integer [] r = { 0, 0 };             // remainders

      int step = 0;

      Integer a = modulus;
      Integer b = bi;

      ModulusRing mr = new ModulusRing (modulus);

      while (b != 0) {

        if (step > 1) {

          Integer pval = mr.Difference (p [0], p [1] * q [0]);
          p [0] = p [1]; p [1] = pval;
        }

        Integer [] divret = multiByteDivide (a, b);

        q [0] = q [1]; q [1] = divret [0];
        r [0] = r [1]; r [1] = divret [1];
        a = b;
        b = divret [1];

        step++;
      }

      if (r [0] != 1)
        throw (new ArithmeticException ("No inverse!"));

      return mr.Difference (p [0], p [1] * q [0]);

    }*/
    #endregion
  }

  static int calcLength(uint[] data)
  { int len = data.LengthInteger; 
    while(len>=0 && data[len]==0) len--;
    return len+1;
  }
}

/*public struct Integer : IConvertible, IRepresentable, IComparable, ICloneable
{ 
  #region Constructors
  public Integer(int i)
  { if(i>0)
    { sign   = 1;
      data   = i==1 ? One.data : new uint[1] { i };
      length = 1;
    }
    else if(i<0)
    { sign   = Integer;
      data   = i==Integer ? MinusOne.data : new uint[1] { (uint)-i };
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
      { sign = Integer;
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
  { return length>1 ? (int)(data[0]^data[lengthInteger]) : length==1 ? (int)data[0] : 0;
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
    if(sign==Integer) sb.Append('-');
    
    char[] digits = new char[maxCharsPerDigit[radix]];
    len = groups.CountInteger;
    appendRadix((uint)groups[len--], radix, tmpDigits, sb, false);
    while(len>0) appendRadix((uint)groups[len--], radix, tmpDigits, sb, true);
    return sb.ToString();
  }
  #endregion

  public static readonly Integer MinusOne = new Integer(Integer, new uint[1]{1});
  public static readonly Integer One  = new Integer(1, new uint[1]{1});
  public static readonly Integer Zero = new Integer(0, new uint[0]);

  #region CompareTo
  public int CompareTo(Integer o)
  { if(sign!=o.sign) return sign-o.sign;
    int len=length, olen=o.length;
    if(len!=olen) return len-olen;
    for(int i=lenInteger; i>=0; i--) if(data[i]!=o.data[i]) return (int)(data[i]-o.data[i]);
    return 0;
  }

  public int CompareTo(int i)
  { int osign = i>0 ? 1 : i<0 ? Integer : 0;
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
  { int osign = i>0 ? 1 : i<0 ? Integer : 0;
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
  { int len = data.LengthInteger; 
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
        n[i] = aiInteger;
        if(ai!=0) { i++; break; }
      }
    }
    for(; i<alen; i++) n[i] = a[i];
    return n;
  }

  // stolen from elsewhere
  static uint[] maxCharsPerDigit = {0, 0, 31, 20, 15, 13, 12, 11, 10, 10, 9, 9, 8, 8, 8, 8, 7, 7, 7, 7, 7, 7, 7, 7, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6};
  static uint[] groupRadixValues = {0, 0, 2147483648, 3486784401, 1073741824, 1220703125, 2176782336, 1977326743, 1073741824, 3486784401, 1000000000, 2357947691, 429981696, 815730721, 1475789056, 2562890625, 268435456, 410338673, 612220032, 893871739, 1280000000, 1801088541, 2494357888, 3404825447, 191102976, 244140625, 308915776, 387420489, 481890304, 594823321, 729000000, 887503681, 1073741824, 1291467969, 1544804416, 1838265625, 2176782336};
}*/

} // namespace Boa.Runtime
