/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004-2005 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/

using System;
using System.Text;
using System.Security.Cryptography;
using Boa.AST;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class _md5
{ _md5() { }

  public static string __repr__() { return "<module 'md5' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  public const int digest_size = 16;

  public byte[] digest(string str) { return Encoding.Default.GetBytes(str); }
  public byte[] digest(byte[] bytes)
  { MD5CryptoServiceProvider hash = new MD5CryptoServiceProvider();
    hash.Initialize();
    return hash.ComputeHash(bytes);
  }
  public string hexdigest(string str) { return Misc.ArrayToHex(digest(str)); }
  public string hexdigest(byte[] bytes) { return Misc.ArrayToHex(digest(bytes)); }

  public static md5 @new() { return new md5(); }
  public static md5 @new(byte[] bytes) { return new md5(bytes); }
  public static md5 @new(string str) { return new md5(str); }

  public class md5
  { public md5() { }
    public md5(byte[] bytes) { update(bytes); }
    public md5(string str) { update(str); }
    md5(uint a, uint b, uint c, uint d, uint l0, uint l1, byte[] buffer)
    { A=a; B=b; C=c; D=d; L0=l0; L1=l1; Array.Copy(buffer, this.buffer, 64);
    }
    static md5() { padding[0] = 0x80; }

    public md5 copy() { return new md5(A, B, C, D, L0, L1, buffer); }

    public unsafe byte[] digest()
    { md5 m = copy();
      byte[] hash = new byte[16];
      m.Finish(hash);
      return hash;
    }

    public string hexdigest() { return Misc.ArrayToHex(digest()); }

    public void update(string str) { update(Encoding.Default.GetBytes(str)); }
    public void update(byte[] bytes) { Update(bytes, (uint)bytes.Length); }

    unsafe void Finish(byte[] hash)
    { byte[] bits = new byte[8];
      fixed(byte* bp=bits) { *(uint*)bp = L0; *(uint*)(bp+4) = L1; }
    
      uint index=(L0>>3)&0x3f, padLen=index<56 ? 56-index : 120-index;
      Update(padding, padLen);
      Update(bits, 8);

      fixed(byte* bp=hash)
      { *(uint*)bp      = A;
        *(uint*)(bp+4)  = B;
        *(uint*)(bp+8)  = C;
        *(uint*)(bp+12) = D;
      }
    }
    
    unsafe void Transform(uint* x)
    { uint a=A, b=B, c=C, d=D;

      FF(ref a, b, c, d, x[ 0], S11, 0xd76aa478);
      FF(ref d, a, b, c, x[ 1], S12, 0xe8c7b756);
      FF(ref c, d, a, b, x[ 2], S13, 0x242070db);
      FF(ref b, c, d, a, x[ 3], S14, 0xc1bdceee);
      FF(ref a, b, c, d, x[ 4], S11, 0xf57c0faf);
      FF(ref d, a, b, c, x[ 5], S12, 0x4787c62a);
      FF(ref c, d, a, b, x[ 6], S13, 0xa8304613);
      FF(ref b, c, d, a, x[ 7], S14, 0xfd469501);
      FF(ref a, b, c, d, x[ 8], S11, 0x698098d8);
      FF(ref d, a, b, c, x[ 9], S12, 0x8b44f7af);
      FF(ref c, d, a, b, x[10], S13, 0xffff5bb1);
      FF(ref b, c, d, a, x[11], S14, 0x895cd7be);
      FF(ref a, b, c, d, x[12], S11, 0x6b901122);
      FF(ref d, a, b, c, x[13], S12, 0xfd987193);
      FF(ref c, d, a, b, x[14], S13, 0xa679438e);
      FF(ref b, c, d, a, x[15], S14, 0x49b40821);

      GG(ref a, b, c, d, x[ 1], S21, 0xf61e2562);
      GG(ref d, a, b, c, x[ 6], S22, 0xc040b340);
      GG(ref c, d, a, b, x[11], S23, 0x265e5a51);
      GG(ref b, c, d, a, x[ 0], S24, 0xe9b6c7aa);
      GG(ref a, b, c, d, x[ 5], S21, 0xd62f105d);
      GG(ref d, a, b, c, x[10], S22,  0x2441453);
      GG(ref c, d, a, b, x[15], S23, 0xd8a1e681);
      GG(ref b, c, d, a, x[ 4], S24, 0xe7d3fbc8);
      GG(ref a, b, c, d, x[ 9], S21, 0x21e1cde6);
      GG(ref d, a, b, c, x[14], S22, 0xc33707d6);
      GG(ref c, d, a, b, x[ 3], S23, 0xf4d50d87);
      GG(ref b, c, d, a, x[ 8], S24, 0x455a14ed);
      GG(ref a, b, c, d, x[13], S21, 0xa9e3e905);
      GG(ref d, a, b, c, x[ 2], S22, 0xfcefa3f8);
      GG(ref c, d, a, b, x[ 7], S23, 0x676f02d9);
      GG(ref b, c, d, a, x[12], S24, 0x8d2a4c8a);

      HH(ref a, b, c, d, x[ 5], S31, 0xfffa3942);
      HH(ref d, a, b, c, x[ 8], S32, 0x8771f681);
      HH(ref c, d, a, b, x[11], S33, 0x6d9d6122);
      HH(ref b, c, d, a, x[14], S34, 0xfde5380c);
      HH(ref a, b, c, d, x[ 1], S31, 0xa4beea44);
      HH(ref d, a, b, c, x[ 4], S32, 0x4bdecfa9);
      HH(ref c, d, a, b, x[ 7], S33, 0xf6bb4b60);
      HH(ref b, c, d, a, x[10], S34, 0xbebfbc70);
      HH(ref a, b, c, d, x[13], S31, 0x289b7ec6);
      HH(ref d, a, b, c, x[ 0], S32, 0xeaa127fa);
      HH(ref c, d, a, b, x[ 3], S33, 0xd4ef3085);
      HH(ref b, c, d, a, x[ 6], S34,  0x4881d05);
      HH(ref a, b, c, d, x[ 9], S31, 0xd9d4d039);
      HH(ref d, a, b, c, x[12], S32, 0xe6db99e5);
      HH(ref c, d, a, b, x[15], S33, 0x1fa27cf8);
      HH(ref b, c, d, a, x[ 2], S34, 0xc4ac5665);

      II(ref a, b, c, d, x[ 0], S41, 0xf4292244);
      II(ref d, a, b, c, x[ 7], S42, 0x432aff97);
      II(ref c, d, a, b, x[14], S43, 0xab9423a7);
      II(ref b, c, d, a, x[ 5], S44, 0xfc93a039);
      II(ref a, b, c, d, x[12], S41, 0x655b59c3);
      II(ref d, a, b, c, x[ 3], S42, 0x8f0ccc92);
      II(ref c, d, a, b, x[10], S43, 0xffeff47d);
      II(ref b, c, d, a, x[ 1], S44, 0x85845dd1);
      II(ref a, b, c, d, x[ 8], S41, 0x6fa87e4f);
      II(ref d, a, b, c, x[15], S42, 0xfe2ce6e0);
      II(ref c, d, a, b, x[ 6], S43, 0xa3014314);
      II(ref b, c, d, a, x[13], S44, 0x4e0811a1);
      II(ref a, b, c, d, x[ 4], S41, 0xf7537e82);
      II(ref d, a, b, c, x[11], S42, 0xbd3af235);
      II(ref c, d, a, b, x[ 2], S43, 0x2ad7d2bb);
      II(ref b, c, d, a, x[ 9], S44, 0xeb86d391);

      A += a; B += b; C += c; D += d;
    }

    unsafe void Update(byte[] bytes, uint inputLen)
    { uint index=(L0>>3)&0x3F, partLen=64-index, i=inputLen<<3;
      L0 += i;
      if(L0<i) L1++;
      L1 += inputLen>>29;
      
      if(inputLen >= partLen)
      { Array.Copy(bytes, 0, buffer, (int)index, (int)partLen);
        fixed(byte* bp=buffer) Transform((uint*)bp);
        fixed(byte* bp=bytes) for(i=partLen; i+63<inputLen; i+=64) Transform((uint*)(bp+i));
        index=0;
      }
      else i=0;
      Array.Copy(bytes, (int)i, buffer, (int)index, (int)(inputLen-i));
    }
    
    uint A=0x67452301, B=0xefcdab89, C=0x98badcfe, D=0x10325476, L0, L1;
    byte[] buffer = new byte[64];

    const int S11=7, S12=12, S13=17, S14=22, S21=5, S22=9,  S23=14, S24=20,
              S31=4, S32=11, S33=16, S34=23, S41=6, S42=10, S43=15, S44=21;

    static uint F(uint x, uint y, uint z) { return (x&y) | (~x & z); }
    static uint G(uint x, uint y, uint z) { return (x&z) | (y & ~z); }
    static uint H(uint x, uint y, uint z) { return x^y^z; }
    static uint I(uint x, uint y, uint z) { return y ^ (x | ~z); }
    static uint RL(uint x, uint n) { return (x<<(int)n) | (x>>(32-(int)n)); }
    static void FF(ref uint a, uint b, uint c, uint d, uint x, uint s, uint ac) { a = RL(a+F(b,c,d)+x+ac, s)+b; }
    static void GG(ref uint a, uint b, uint c, uint d, uint x, uint s, uint ac) { a = RL(a+G(b,c,d)+x+ac, s)+b; }
    static void HH(ref uint a, uint b, uint c, uint d, uint x, uint s, uint ac) { a = RL(a+H(b,c,d)+x+ac, s)+b; }
    static void II(ref uint a, uint b, uint c, uint d, uint x, uint s, uint ac) { a = RL(a+I(b,c,d)+x+ac, s)+b; }

    static byte[] padding = new byte[64];
  }
}

} // namespace Boa.Modules
