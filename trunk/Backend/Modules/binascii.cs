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
  public static byte[] a2b_base32(string data) { return a2b_base32(Encoding.ASCII.GetBytes(data)); }
  public static byte[] a2b_base32(byte[] data) { throw new NotImplementedException(); }

  public static string b2a_base32(string data) { return b2a_base32(Encoding.ASCII.GetBytes(data)); }
  public static string b2a_base32(byte[] data) { throw new NotImplementedException(); }
  #endregion

  #region base64
  public static byte[] a2b_base64(string data) { return a2b_base64(Encoding.ASCII.GetBytes(data)); }
  public static byte[] a2b_base64(byte[] data) { throw new NotImplementedException(); }

  public static string b2a_base64(string data) { return b2a_base64(Encoding.ASCII.GetBytes(data)); }
  public unsafe static string b2a_base64(byte[] data)
  { int left = data.Length%3;
    char[] output = new char[(data.Length+2)/3*4 + (data.Length+75)/76];

    fixed(char* cp=b64e)
    fixed(char* op=output)
    fixed(byte* dp=data)
    { char* o=op;
      byte* p=dp, e=dp+data.Length-2;
      int i=0;
      byte a, b, c;

      while(p<e)
      { a=*p++; b=*p++; c=*p++;
        *o++ = cp[a>>2];
        *o++ = cp[((a&3)<<4) | (b>>4)];
        *o++ = cp[((b&0xF)<<2) | (c>>6)];
        *o++ = cp[c&0x3F];
        if(++i==19) { *o++='\n'; i=0; }
      }

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
  
  static const string b64e = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
  #endregion

  #region hex (base16)
  public static byte[] a2b_hex(string data) { return a2b_hex(Encoding.ASCII.GetBytes(data)); }
  public static byte[] a2b_hex(byte[] data) { throw new NotImplementedException(); }
  public static byte[] unhexlify(string data) { return a2b_hex(Encoding.ASCII.GetBytes(data)); }
  public static byte[] unhexlify(byte[] data) { return a2b_hex(data); }

  public static string b2a_hex(string data) { return b2a_hex(Encoding.ASCII.GetBytes(data)); }
  public static string b2a_hex(byte[] data) { throw new NotImplementedException(); }
  public static string hexlify(string data) { return b2a_hex(Encoding.ASCII.GetBytes(data)); }
  public static string hexlify(byte[] data) { return b2a_hex(data); }
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
  public static byte[] a2b_qp(string data) { return a2b_qp(Encoding.ASCII.GetBytes(data), false); }
  public static byte[] a2b_qp(string data, bool header) { return a2b_qp(Encoding.ASCII.GetBytes(data), header); }
  public static byte[] a2b_qp(byte[] data) { return a2b_qp(data, false); }
  public static byte[] a2b_qp(byte[] data, bool header) { throw new NotImplementedException(); }

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
  public static byte[] a2b_uu(string data) { return a2b_uu(Encoding.ASCII.GetBytes(data)); }
  public static byte[] a2b_uu(byte[] data) { throw new NotImplementedException(); }

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
