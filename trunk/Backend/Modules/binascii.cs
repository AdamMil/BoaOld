/*
Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targeting the Microsoft .NET Framework.

http://www.adammil.net/
Copyright (C) 2004 Adam Milazzo

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
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class binascii
{ binascii() { }

  public class Error : RuntimeException
  { public Error(string message) : base(message) { }
  }

  public class Incomplete : RuntimeException
  { public Incomplete(string message) : base(message) { }
  }

  public static string __repr__() { return "<module 'binascii' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  #region base32
  public static byte[] a2b_base32(string data) { throw new NotImplementedException(); }
  public static byte[] a2b_base32(byte[] data) { return a2b_base32(Encoding.ASCII.GetString(data)); }

  public static string b2a_base32(string data) { return b2a_base32(Encoding.ASCII.GetBytes(data)); }
  public static unsafe string b2a_base32(byte[] data)
  { char[] output = new char[(data.Length+4)/5*8 + data.Length/72];

    fixed(char* cp=b32e)
    fixed(char* op=output)
    fixed(byte* dp=data)
    { char* o=op;
      byte* p=dp, e=dp+data.Length-4;
      int i=0, left;
      uint a, b, c;
      while(p<e)
      { a = (uint)(*p++<<8) | *p++;
        b = (uint)((*p++<<8) | *p++) | ((a&1)<<16);
        c = *p++ | ((b&3)<<8);

        *o++ = cp[a>>11];
        *o++ = cp[(a>>6)&0x1f];
        *o++ = cp[(a>>1)&0x1f];
        *o++ = cp[b>>12];
        *o++ = cp[(b>>7)&0x1f];
        *o++ = cp[(b>>2)&0x1f];
        *o++ = cp[c>>5];
        *o++ = cp[c&0x1f];

        if(++i==9) { *o++='\n'; i=0; }
      }

      left = (int)(e-p)+4;
      
      if(left==1)
      { a = *p;
        *o++ = cp[a>>3];
        *o++ = cp[a&7];
        *(uint*)o     = 0x3d003d; // ======
        *(uint*)(o+2) = 0x3d003d;
        *(uint*)(o+4) = 0x3d003d;
      }
      else if(left==2)
      { a = (uint)(*p++<<8) | *p++;
        *o++ = cp[a>>11];
        *o++ = cp[(a>>6)&0x1f];
        *o++ = cp[(a>>1)&0x1f];
        *o++ = cp[(a&1)<<4];
        *(uint*)o     = 0x3d003d; // ====
        *(uint*)(o+2) = 0x3d003d;
      }
      else if(left==3)
      { a = (uint)(*p++<<8) | *p++;
        b = *p;
        *o++ = cp[a>>11];
        *o++ = cp[(a>>6)&0x1f];
        *o++ = cp[(a>>1)&0x1f];
        *o++ = cp[((a&1)<<4) | (b>>4)];
        *o++ = cp[(b&0xF)<<1];
        *(uint*)o = 0x3d003d; // ==
        *(o+2)    = '=';
      }
      else if(left==4)
      { a = (uint)(*p++<<8) | *p++;
        b = (uint)((*p++<<8) | *p++) | ((a&1)<<16);
        *o++ = cp[a>>11];
        *o++ = cp[(a>>6)&0x1f];
        *o++ = cp[(a>>1)&0x1f];
        *o++ = cp[b>>12];
        *o++ = cp[(b>>7)&0x1f];
        *o++ = cp[(b>>2)&0x1f];
        *o++ = cp[b&3];
        *o   = '=';
      }
    }

    return new string(output);
  }

  const string b32e = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  #endregion

  #region base64
  public static byte[] a2b_base64(byte[] data) { return a2b_base64(Encoding.ASCII.GetString(data)); }
  public static unsafe byte[] a2b_base64(string data)
  { byte[] output = new byte[(data.Length*3+3)/4];
    
    fixed(byte* cp=b64d)
    fixed(byte* op=output)
    fixed(char* dp=data)
    { byte* o=op;
      char* p=dp, e=dp+data.Length;
      byte a, b, c, d;

      while(p<e)
      { do
        { a=cp[(byte)*p++]; 
          if(p==e) goto breakout;
        } while(a==255);
        
        do
        { b=cp[(byte)*p++]; 
          if(p==e) goto breakout;
        } while(b==255);

        do c=cp[(byte)*p++]; while(c==255 && p<e);
        if(p==e || c==64)
        { *o++ = (byte)((a<<2) | ((b>>4)&3));
          break;
        }

        do d=cp[(byte)*p++]; while(d==255 && p<e);
        if(p==e || d==64)
        { *o++ = (byte)((a<<2) | ((b>>4)&3));
          *o++ = (byte)((b<<4) | ((c>>2)&0xF));
          break;
        }

        *o++ = (byte)((a<<2) | ((b>>4)&3));
        *o++ = (byte)((b<<4) | ((c>>2)&0xF));
        *o++ = (byte)((c<<6) | d);
      }

      breakout:
      int len = (int)(o-op);
      if(len==output.Length) return output;
      byte[] narr = new byte[len];
      Array.Copy(output, narr, len);
      return narr;
    }
  }

  public static string b2a_base64(string data) { return b2a_base64(Encoding.ASCII.GetBytes(data)); }
  public static unsafe string b2a_base64(byte[] data)
  { char[] output = new char[(data.Length+2)/3*4 + (data.Length+75)/76];

    fixed(char* cp=b64e)
    fixed(char* op=output)
    fixed(byte* dp=data)
    { char* o=op;
      byte* p=dp, e=dp+data.Length-2;
      int i=0, left;
      byte a, b, c;

      while(p<e)
      { a=*p++; b=*p++; c=*p++;
        *o++ = cp[a>>2];
        *o++ = cp[((a&3)<<4) | (b>>4)];
        *o++ = cp[((b&0xF)<<2) | (c>>6)];
        *o++ = cp[c&0x3F];
        if(++i==19) { *o++='\n'; i=0; }
      }

      left = (int)(e-p)+2;
      
      if(left==1)
      { a = *p;
        *o++ = cp[a>>2];
        *o++ = cp[(a&3)<<4];
        *o++ = '=';
        *o++ = '=';
      }
      else if(left==2)
      { a=*p++; b=*p;
        *o++ = cp[a>>2];
        *o++ = cp[((a&3)<<4) | (b>>4)];
        *o++ = cp[(b&0xF)<<2];
        *o++ = '=';
      }
      if(left!=0 || i!=0) *o = '\n';
    }

    return new string(output);
  }
  
  const string b64e = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";

  static readonly byte[] b64d = new byte[256]
  { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 62,  255, 255, 255, 63,
    52,  53,  54,  55,  56,  57,  58,  59,  60,  61,  255, 255, 255, 64,  255, 255,
    255, 0,   1,   2,   3,   4,   5,   6,   7,   8,   9,   10,  11,  12,  13,  14,
    15,  16,  17,  18,  19,  20,  21,  22,  23,  24,  25,  255, 255, 255, 255, 255,
    255, 26,  27,  28,  29,  30,  31,  32,  33,  34,  35,  36,  37,  38,  39,  40,
    41,  42,  43,  44,  45,  46,  47,  48,  49,  50,  51,  255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255
  };  
  #endregion

  #region hex (base16)
  public static byte[] unhexlify(string data) { return a2b_hex(data); }
  public static byte[] unhexlify(byte[] data) { return a2b_hex(Encoding.ASCII.GetString(data)); }
  public static byte[] a2b_hex(byte[] data) { return a2b_hex(Encoding.ASCII.GetString(data)); }
  public static unsafe byte[] a2b_hex(string data)
  { if((data.Length&1)!=0) throw Ops.TypeError("a2b_hex(): input data is not a multiple of 2 in length");
    byte[] output = new byte[data.Length/2];
    data = data.ToUpper();
    
    fixed(char* dp=data)
    fixed(byte* op=output)
    { char* p=dp, e=p+data.Length;
      byte* o=op;
      byte  h, l;
      
      while(p<e)
      { h=(byte)(*p++ - '0'); if(h>9) { h-=7; if(h>15) goto baddata; }
        l=(byte)(*p++ - '0'); if(l>9) { l-=7; if(l>15) goto baddata; }
        *o++ = (byte)((h<<4) | l);
      }
    }
    return output;
    
    baddata: throw Ops.TypeError("a2b_hex(): input data is not a valid hex string");
  }

  public static string hexlify(string data) { return b2a_hex(Encoding.ASCII.GetBytes(data)); }
  public static string hexlify(byte[] data) { return b2a_hex(data); }
  public static string b2a_hex(string data) { return b2a_hex(Encoding.ASCII.GetBytes(data)); }
  public static unsafe string b2a_hex(byte[] data)
  { char[] output = new char[data.Length*2];
    
    fixed(byte* dp=data)
    fixed(char* op=output)
    fixed(char* cp=hexe)
    { byte* p=dp, e=p+data.Length;
      char* o=op;
      byte  b;
      
      while(p<e)
      { b=*p++;
        *o++ = hexe[b>>4];
        *o++ = hexe[b&15];
      }
    }
    
    return new string(output);
  }
  
  const string hexe = "0123456789ABCDEF";
  #endregion

  #region hqx
  public static string b2a_hqx(string data) { return b2a_hqx(Encoding.ASCII.GetBytes(data)); }
  public static string b2a_hqx(byte[] data) { throw new NotImplementedException(); }

  public static int crc_hqx(string data, int crc) { return crc_hqx(Encoding.ASCII.GetBytes(data), crc); }
  public static int crc_hqx(byte[] data, int crc) { throw new NotImplementedException(); }

  public static byte[] rlecode_hqx(string data) { return rlecode_hqx(Encoding.ASCII.GetBytes(data)); }
  public static byte[] rlecode_hqx(byte[] data) { throw new NotImplementedException(); }

  public static byte[] rledecode_hqx(string data) { return rledecode_hqx(Encoding.ASCII.GetBytes(data)); }
  public static byte[] rledecode_hqx(byte[] data) { throw new NotImplementedException(); }
  #endregion

  #region qp (quoted printable)
  public static byte[] a2b_qp(string data) { return a2b_qp(data, false); }
  public static byte[] a2b_qp(string data, bool header){ throw new NotImplementedException(); }
  public static byte[] a2b_qp(byte[] data) { return a2b_qp(Encoding.ASCII.GetString(data), false); }
  public static byte[] a2b_qp(byte[] data, bool header) { return a2b_qp(Encoding.ASCII.GetString(data), header); }

  public static string b2a_qp(string data) { return b2a_qp(Encoding.ASCII.GetBytes(data), false, false, false); }
  public static string b2a_qp(string data, bool quotetabs)
  { return b2a_qp(Encoding.ASCII.GetBytes(data), quotetabs, false, false);
  }
  public static string b2a_qp(string data, bool quotetabs, bool istext)
  { return b2a_qp(Encoding.ASCII.GetBytes(data), quotetabs, istext, false);
  }
  public static string b2a_qp(string data, bool quotetabs, bool istext, bool header)
  { return b2a_qp(Encoding.ASCII.GetBytes(data), quotetabs, istext, header);
  }

  public static string b2a_qp(byte[] data) { return b2a_qp(data, false, false, false); }
  public static string b2a_qp(byte[] data, bool quotetabs) { return b2a_qp(data, quotetabs, false, false); }
  public static string b2a_qp(byte[] data, bool quotetabs, bool istext)
  { return b2a_qp(data, quotetabs, istext, false);
  }
  public static string b2a_qp(byte[] data, bool quotetabs, bool istext, bool header) { throw new NotImplementedException(); }
  #endregion

  #region uu (unix to unix)
  public static byte[] a2b_uu(string data) { throw new NotImplementedException(); }
  public static byte[] a2b_uu(byte[] data) { return a2b_uu(Encoding.ASCII.GetString(data)); }

  public static string b2a_uu(string data) { return b2a_uu(Encoding.ASCII.GetBytes(data)); }
  public static string b2a_uu(byte[] data) { throw new NotImplementedException(); }
  #endregion
  
  #region crc32
  public static int crc32(string data) { return crc32(Encoding.ASCII.GetBytes(data), 0); }
  public static int crc32(byte[] data) { return crc32(data, 0); }
  public static int crc32(string data, int crc) { return crc32(Encoding.ASCII.GetBytes(data), crc); }
  public static int crc32(byte[] data, int crc) { throw new NotImplementedException(); }
  #endregion
}

} // namespace Boa.Modules
