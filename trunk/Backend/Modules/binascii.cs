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
  public static unsafe byte[] a2b_base32(byte[] data) { return a2b_base32(Encoding.ASCII.GetString(data)); }
  public static unsafe byte[] a2b_base32(string data)
  { if(data.Length==0) return new byte[0];

    byte[] output = new byte[(data.Length*5+7)/8];
    data = data.ToUpper();

    fixed(byte* cp=b32d)
    fixed(byte* op=output)
    fixed(char* dp=data)
    { byte* o=op;
      char* p=dp, e=dp+data.Length;
      byte* b=stackalloc byte[8];
      byte  c;

      while(true)
      { do
        { if(p==e) goto breakout;
          b[0]=cp[*p++&0x7F]; 
        } while(b[0]==255);
        
        do
        { if(p==e) goto breakout;
          b[1]=cp[*p++&0x7F];
        } while(b[1]==255);

        for(int i=2; i<8; i++)
        { if(p==e) c=64;
          else do c=cp[*p++&0x7F]; while(c==255 && p<e);
          if(c==64)
            switch(i)
            { case 2: case 3: // ======
                *o++ = (byte)((b[0]<<3) | (b[1]>>2));
                goto breakout;
              case 4: // ====
                *o++ = (byte)((b[0]<<3) | (b[1]>>2));
                *o++ = (byte)(((b[1]&3)<<6) | (b[2]<<1) | (b[3]>>4));
                goto breakout;
              case 5: case 6: // ===
                *o++ = (byte)((b[0]<<3) | (b[1]>>2));
                *o++ = (byte)(((b[1]&3)<<6) | (b[2]<<1) | (b[3]>>4));
                *o++ = (byte)(((b[3]&0xF)<<4) | (b[4]>>1));
                goto breakout;
              case 7: // =
                *o++ = (byte)((b[0]<<3) | (b[1]>>2));
                *o++ = (byte)(((b[1]&3)<<6) | (b[2]<<1) | (b[3]>>4));
                *o++ = (byte)(((b[3]&0xF)<<4) | (b[4]>>1));
                *o++ = (byte)(((b[4]&1)<<7) | (b[5]<<2) | (b[6]>>3));
                goto breakout;
              default: goto breakout;
            }
          b[i] = c;
        }

        *o++ = (byte)((b[0]<<3) | (b[1]>>2));
        *o++ = (byte)(((b[1]&3)<<6) | (b[2]<<1) | (b[3]>>4));
        *o++ = (byte)(((b[3]&0xF)<<4) | (b[4]>>1));
        *o++ = (byte)(((b[4]&1)<<7) | (b[5]<<2) | (b[6]>>3));
        *o++ = (byte)(((b[6]&7)<<5) | b[7]);
      }

      breakout:
      int len = (int)(o-op);
      if(len==output.Length) return output;
      byte[] narr = new byte[len];
      Array.Copy(output, narr, len);
      return narr;
    }
  }

  public static string b2a_base32(string data) { return b2a_base32(Encoding.ASCII.GetBytes(data)); }
  public static unsafe string b2a_base32(byte[] data)
  { if(data.Length==0) return string.Empty;
    char[] output = new char[(data.Length+4)/5*8 + data.Length/72];

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
        *(uint*)o = 0x3d003d; // ===
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
  static readonly byte[] b32d = new byte[91]
  { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 26,  27,  28,  29,  30,  31,  255, 255, 255, 255, 255, 64,  255, 255,
    255, 0,   1,   2,   3,   4,   5,   6,   7,   8,   9,   10,  11,  12,  13,  14,
    15,  16,  17,  18,  19,  20,  21,  22,  23,  24,  25,
  };  
  #endregion

  #region base64
  public static byte[] a2b_base64(byte[] data) { return a2b_base64(Encoding.ASCII.GetString(data)); }
  public static unsafe byte[] a2b_base64(string data)
  { if(data.Length==0) return new byte[0];
    byte[] output = new byte[(data.Length*3+3)/4];
    
    fixed(byte* cp=b64d)
    fixed(byte* op=output)
    fixed(char* dp=data)
    { byte* o=op;
      char* p=dp, e=dp+data.Length;
      byte a, b, c, d;

      while(true)
      { do
        { if(p==e) goto breakout;
          a=cp[(byte)*p++]; 
        } while(a==255);
        
        do
        { if(p==e) goto breakout;
          b=cp[(byte)*p++]; 
        } while(b==255);

        if(p==e) c=64;
        else do c=cp[(byte)*p++]; while(c==255 && p<e);
        if(c==64)
        { *o++ = (byte)((a<<2) | ((b>>4)&3));
          break;
        }

        if(p==e) d=64;
        else do d=cp[(byte)*p++]; while(d==255 && p<e);
        if(d==64)
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
  { if(data.Length==0) return "\n";
    char[] output = new char[(data.Length+2)/3*4 + data.Length/76];

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
  public static byte[] a2b_uu(byte[] data) { return a2b_uu(Encoding.ASCII.GetString(data)); }
  public static unsafe byte[] a2b_uu(string data)
  { if(data.Length==0) goto baddata;

    int len = data[0]-' ', left = len%3;
    if(len<0 || (data.Length-1)*3/4<len) goto baddata;

    byte[] output = new byte[len];
    if(len==0) return output;

    fixed(byte* cp=uud)
    fixed(byte* op=output)
    fixed(char* dp=data)
    { byte* o = op;
      char* p = dp+1, e=p+(len-left);
      byte a, b, c, d;

      while(p<e)
      { a=cp[*p++&0x7F]; b=cp[*p++&0x7F]; c=cp[*p++&0x7F]; d=cp[*p++&0x7F];
        if((a|b|c|d)>252) goto baddata;
        *o++ = (byte)((a<<2) | ((b>>4)&3));
        *o++ = (byte)((b<<4) | ((c>>2)&0xF));
        *o++ = (byte)((c<<6) | d);
      }

      if(left==1)
      { a=cp[*p++&0x7F]; b=cp[*p++&0x7F];
        if((a|b)>252) goto baddata;
        *o++ = (byte)((a<<2) | ((b>>4)&3));
      }
      else if(left==2)
      { a=cp[*p++&0x7F]; b=cp[*p++&0x7F]; c=cp[*p++&0x7F];
        if((a|b|c)>252) goto baddata;
        *o++ = (byte)((a<<2) | ((b>>4)&3));
        *o++ = (byte)((b<<4) | ((c>>2)&0xF));
      }
    }
    return output;

    baddata: throw Ops.TypeError("a2b_uu(): 'data' is not a valid uuencoded string");
  }

  public static string b2a_uu(string data) { return b2a_uu(Encoding.ASCII.GetBytes(data)); }
  public static unsafe string b2a_uu(byte[] data)
  { if(data.Length==0) return " \n";
    if(data.Length>45) throw Ops.TypeError("b2a_uu(): maximum of 45 bytes at a time");

    int left = data.Length%3;
    char[] output = new char[data.Length/3*4 + (left==0 ? 0 : left+1) + 1];

    fixed(char* op=output)
    fixed(byte* dp=data)
    { char* o=op;
      byte* p=dp, e=dp+data.Length-2;
      byte a, b, c;

      *o++ = (char)((int)(e-p)+2+' ');

      while(p<e)
      { a=*p++; b=*p++; c=*p++;
        *o++ = (char)((a>>2)+' ');
        *o++ = (char)((((a&3)<<4) | (b>>4))+' ');
        *o++ = (char)((((b&0xF)<<2) | (c>>6))+' ');
        *o++ = (char)((c&0x3F)+' ');
      }

      left = (int)(e-p)+2;

      if(left==1)
      { a = *p;
        *o++ = (char)((a>>2)+' ');
        *o++ = (char)(((a&3)<<4)+' ');
      }
      else if(left==2)
      { a=*p++; b=*p;
        *o++ = (char)((a>>2)+' ');
        *o++ = (char)((((a&3)<<4) | (b>>4))+' ');
        *o++ = (char)(((b&0xF)<<2)+' ');
      }
      *o = '\n';
    }

    return new string(output);
  }

  static readonly byte[] uud = new byte[128]
  { 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    0,   1,   2,   3,   4,   5,   6,   7,   8,   9,   10,  11,  12,  13,  14,  15,
    16,  17,  18,  19,  20,  21,  22,  23,  24,  25,  26,  27,  28,  29,  30,  31,
    32,  33,  34,  35,  36,  37,  38,  39,  40,  41,  42,  43,  44,  45,  46,  47,
    48,  49,  50,  51,  52,  53,  54,  55,  56,  57,  58,  59,  60,  61,  62,  63,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
    255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
  };  
  #endregion
  
  #region crc32
  public static int crc32(string data) { return crc32(Encoding.ASCII.GetBytes(data), 0); }
  public static int crc32(byte[] data) { return crc32(data, 0); }
  public static int crc32(string data, int crc) { return crc32(Encoding.ASCII.GetBytes(data), crc); }
  public static unsafe int crc32(byte[] data, int crc)
  { if(data.Length==0) return crc;
    crc = ~crc;
    fixed(uint* cp=crctab)
    fixed(byte* dp=data)
    { byte* p=dp, e=p+data.Length;
      while(p<e) crc = (int)(cp[(byte)(crc ^ *p++)] ^ ((uint)crc >> 8));
      return ~crc;
		}
  }

  static readonly uint[] crctab = new uint[]
  { 0x00000000, 0x77073096, 0xee0e612c, 0x990951ba, 0x076dc419, 0x706af48f, 0xe963a535, 0x9e6495a3, 0x0edb8832,
    0x79dcb8a4, 0xe0d5e91e, 0x97d2d988, 0x09b64c2b, 0x7eb17cbd, 0xe7b82d07, 0x90bf1d91, 0x1db71064, 0x6ab020f2,
    0xf3b97148, 0x84be41de, 0x1adad47d, 0x6ddde4eb, 0xf4d4b551, 0x83d385c7, 0x136c9856, 0x646ba8c0, 0xfd62f97a,
    0x8a65c9ec, 0x14015c4f, 0x63066cd9, 0xfa0f3d63, 0x8d080df5, 0x3b6e20c8, 0x4c69105e, 0xd56041e4, 0xa2677172,
    0x3c03e4d1, 0x4b04d447, 0xd20d85fd, 0xa50ab56b, 0x35b5a8fa, 0x42b2986c, 0xdbbbc9d6, 0xacbcf940, 0x32d86ce3,
    0x45df5c75, 0xdcd60dcf, 0xabd13d59, 0x26d930ac, 0x51de003a, 0xc8d75180, 0xbfd06116, 0x21b4f4b5, 0x56b3c423,
    0xcfba9599, 0xb8bda50f, 0x2802b89e, 0x5f058808, 0xc60cd9b2, 0xb10be924, 0x2f6f7c87, 0x58684c11, 0xc1611dab,
    0xb6662d3d, 0x76dc4190, 0x01db7106, 0x98d220bc, 0xefd5102a, 0x71b18589, 0x06b6b51f, 0x9fbfe4a5, 0xe8b8d433,
    0x7807c9a2, 0x0f00f934, 0x9609a88e, 0xe10e9818, 0x7f6a0dbb, 0x086d3d2d, 0x91646c97, 0xe6635c01, 0x6b6b51f4,
    0x1c6c6162, 0x856530d8, 0xf262004e, 0x6c0695ed, 0x1b01a57b, 0x8208f4c1, 0xf50fc457, 0x65b0d9c6, 0x12b7e950,
    0x8bbeb8ea, 0xfcb9887c, 0x62dd1ddf, 0x15da2d49, 0x8cd37cf3, 0xfbd44c65, 0x4db26158, 0x3ab551ce, 0xa3bc0074,
    0xd4bb30e2, 0x4adfa541, 0x3dd895d7, 0xa4d1c46d, 0xd3d6f4fb, 0x4369e96a, 0x346ed9fc, 0xad678846, 0xda60b8d0,
    0x44042d73, 0x33031de5, 0xaa0a4c5f, 0xdd0d7cc9, 0x5005713c, 0x270241aa, 0xbe0b1010, 0xc90c2086, 0x5768b525,
    0x206f85b3, 0xb966d409, 0xce61e49f, 0x5edef90e, 0x29d9c998, 0xb0d09822, 0xc7d7a8b4, 0x59b33d17, 0x2eb40d81,
    0xb7bd5c3b, 0xc0ba6cad, 0xedb88320, 0x9abfb3b6, 0x03b6e20c, 0x74b1d29a, 0xead54739, 0x9dd277af, 0x04db2615,
    0x73dc1683, 0xe3630b12, 0x94643b84, 0x0d6d6a3e, 0x7a6a5aa8, 0xe40ecf0b, 0x9309ff9d, 0x0a00ae27, 0x7d079eb1,
    0xf00f9344, 0x8708a3d2, 0x1e01f268, 0x6906c2fe, 0xf762575d, 0x806567cb, 0x196c3671, 0x6e6b06e7, 0xfed41b76,
    0x89d32be0, 0x10da7a5a, 0x67dd4acc, 0xf9b9df6f, 0x8ebeeff9, 0x17b7be43, 0x60b08ed5, 0xd6d6a3e8, 0xa1d1937e,
    0x38d8c2c4, 0x4fdff252, 0xd1bb67f1, 0xa6bc5767, 0x3fb506dd, 0x48b2364b, 0xd80d2bda, 0xaf0a1b4c, 0x36034af6,
    0x41047a60, 0xdf60efc3, 0xa867df55, 0x316e8eef, 0x4669be79, 0xcb61b38c, 0xbc66831a, 0x256fd2a0, 0x5268e236,
    0xcc0c7795, 0xbb0b4703, 0x220216b9, 0x5505262f, 0xc5ba3bbe, 0xb2bd0b28, 0x2bb45a92, 0x5cb36a04, 0xc2d7ffa7,
    0xb5d0cf31, 0x2cd99e8b, 0x5bdeae1d, 0x9b64c2b0, 0xec63f226, 0x756aa39c, 0x026d930a, 0x9c0906a9, 0xeb0e363f,
    0x72076785, 0x05005713, 0x95bf4a82, 0xe2b87a14, 0x7bb12bae, 0x0cb61b38, 0x92d28e9b, 0xe5d5be0d, 0x7cdcefb7,
    0x0bdbdf21, 0x86d3d2d4, 0xf1d4e242, 0x68ddb3f8, 0x1fda836e, 0x81be16cd, 0xf6b9265b, 0x6fb077e1, 0x18b74777,
    0x88085ae6, 0xff0f6a70, 0x66063bca, 0x11010b5c, 0x8f659eff, 0xf862ae69, 0x616bffd3, 0x166ccf45, 0xa00ae278,
    0xd70dd2ee, 0x4e048354, 0x3903b3c2, 0xa7672661, 0xd06016f7, 0x4969474d, 0x3e6e77db, 0xaed16a4a, 0xd9d65adc,
    0x40df0b66, 0x37d83bf0, 0xa9bcae53, 0xdebb9ec5, 0x47b2cf7f, 0x30b5ffe9, 0xbdbdf21c, 0xcabac28a, 0x53b39330,
    0x24b4a3a6, 0xbad03605, 0xcdd70693, 0x54de5729, 0x23d967bf, 0xb3667a2e, 0xc4614ab8, 0x5d681b02, 0x2a6f2b94,
    0xb40bbe37, 0xc30c8ea1, 0x5a05df1b, 0x2d02ef8d
  };
  #endregion
}

} // namespace Boa.Modules
