using System;
using System.Collections;
using Boa.Runtime;

namespace Boa.Modules
{

[BoaType("module")]
public sealed class @struct
{ @struct() { }

  public static string __repr__() { return "<module 'struct' (built-in)>"; }
  public static string __str__() { return __repr__(); }

  public static int calcsize(string format) { int dummy; return calcsize(format, out dummy); }

  public static byte[] pack(string format, params object[] args)
  { int nargs, size=calcsize(format, out nargs);
    if(args.Length!=nargs) throw Ops.WrongNumArgs("pack()", nargs+1, args.Length+1);
    byte[] ret = new byte[size];

    int ri=0, ai=0;
#if BIGENDIAN
    bool bigendian=true;
#else
    bool bigendian=false;
#endif

    for(int i=0; i<format.Length; i++)
    { int count;
      while(i<format.Length && char.IsWhiteSpace(format[i])) i++;
      if(i==format.Length) break;
      if(char.IsDigit(format[i]))
      { count = format[i++]-'0';
        while(i<format.Length && char.IsDigit(format[i])) count = count*10 + (format[i++]-'0');
        if(i==format.Length) throw Ops.ValueError("format string ended prematurely");
      }
      else count = 1;
      
      switch(format[i])
      { case 'b': case 'B': while(count-->0) ret[ri++] = (byte)Ops.ToInt(args[ai++]); break;
        case 'x': ai += count; break;

        case 'c':
          while(count-->0)
          { string s = Ops.ToString(args[ai++]);
            if(s.Length!=1)
              throw Ops.ValueError("char format requires a character, but got a string of length {0}", s.Length);
            if(s[0]>255) throw Ops.ValueError("character '{0}' (0x{1:x}) cannot be represented in a single byte",
                                              s[0], (int)s[0]);
            ret[ri++] = (byte)s[0];
          }
          break;

        case 's': case 'p':
        { if(count==0) break;
          object o = args[ai++];
          string s = o as string;
          if(s!=null)
          { if(format[i]=='p')
            { int len = Math.Min(Math.Min(s.Length, 255), --count);
              ret[ri++] = (byte)len;
            }
            for(int si=0; si<count; si++)
            { if(s[si]>255)
                throw Ops.ValueError("character '{0}' (0x{1:x}) cannot be represented in a single byte", s[si]);
              ret[ri++] = (byte)s[si];
            }
          }
          else
          { byte[] arr = o as byte[];
            if(arr==null) throw Ops.ValueError("'s' and 'p' format requires string or byte[], but got {0}",
                                               Ops.GetDynamicType(o).__name__);
            Array.Copy(arr, 0, ret, ri, Math.Min(count, arr.Length));
            ri += count;
          }
          break;
        }

        case 'h': case 'H':
          if(bigendian) while(count-->0) { WriteBE2(ret, ri, (short)Ops.ToInt(args[ai++])); ri+=2; }
          else while(count-->0) { WriteLE2(ret, ri, (short)Ops.ToInt(args[ai++])); ri+=2; }
          break;

        case 'i': case 'l':
          if(bigendian) while(count-->0) { WriteBE4(ret, ri, Ops.ToInt(args[ai++])); ri+=4; }
          else while(count-->0) { WriteLE4(ret, ri, Ops.ToInt(args[ai++])); ri+=4; }
          break;

        case 'I': case 'L':
          if(bigendian) while(count-->0) { WriteBE4(ret, ri, (int)Ops.ToUInt(args[ai++])); ri+=4; }
          else while(count-->0) { WriteLE4(ret, ri, (int)Ops.ToUInt(args[ai++])); ri+=4; }
          break;

        case 'f': 
          while(count-->0) { WriteFloat(ret, ri, (float)Ops.ToFloat(args[ai++])); ri+=4; }
          break;

        case 'd': 
          while(count-->0) { WriteDouble(ret, ri, (float)Ops.ToFloat(args[ai++])); ri+=8; }
          break;

        case 'q':
          if(bigendian) while(count-->0) { WriteBE8(ret, ri, Ops.ToLong(args[ai++])); ri+=8; }
          else while(count-->0) { WriteLE8(ret, ri, Ops.ToLong(args[ai++])); ri+=8; }
          break;

        case 'Q':
          if(bigendian) while(count-->0) { WriteBE8(ret, ri, (long)Ops.ToULong(args[ai++])); ri+=8; }
          else while(count-->0) { WriteLE8(ret, ri, (long)Ops.ToULong(args[ai++])); ri+=8; }
          break;

        case 'P':
          if(IntPtr.Size==4)
          { if(bigendian) while(count-->0) { WriteBE4(ret, ri, (int)Ops.ToUInt(args[ai++])); ri+=4; }
            else while(count-->0) { WriteLE4(ret, ri, (int)Ops.ToUInt(args[ai++])); ri+=4; }
          }
          else
          { if(bigendian) while(count-->0) { WriteBE8(ret, ri, (long)Ops.ToULong(args[ai++])); ri+=8; }
            else while(count-->0) { WriteLE8(ret, ri, (long)Ops.ToULong(args[ai++])); ri+=8; }
          }
          break;

        case '<': bigendian=false; break;
        case '>': case '!': bigendian=true; break;
        #if BIGENDIAN
        case '=': bigendian=true; break;
        #else
        case '=': bigendian=false; break;
        #endif
      }
    }
    return ret;
  }

  public static Tuple unpack(string format, byte[] data)
  { int nargs, size=calcsize(format, out nargs);
    if(data.Length<size)
      throw Ops.ValueError("expected at least {0} bytes of input, but only got {1}", size, data.Length);
    object[] ret = new object[nargs];

    int ri=0, ai=0;
#if BIGENDIAN
    bool bigendian=true;
#else
    bool bigendian=false;
#endif

    for(int i=0; i<format.Length; i++)
    { int count;
      while(i<format.Length && char.IsWhiteSpace(format[i])) i++;
      if(i==format.Length) break;
      if(char.IsDigit(format[i]))
      { count = format[i++]-'0';
        while(i<format.Length && char.IsDigit(format[i])) count = count*10 + (format[i++]-'0');
        if(i==format.Length) throw Ops.ValueError("format string ended prematurely");
      }
      else count = 1;

      switch(format[i])
      { case 'b': case 'B': while(count-->0) ret[ri++] = (int)data[ai++]; break;
        case 'x': ai += count; break;
        case 'c': while(count-->0) ret[ri++] = new string((char)data[ai++], 1); break;

        case 's': case 'p':
        { if(count==0) break;
          int len;
          if(format[i]=='p') { len = data[ai++]; count--; }
          else len = count;
          
          byte[] arr = new byte[len];
          Array.Copy(data, ai, arr, 0, len);
          ret[ri++] = arr;
          ai += count;
          break;
        }

        case 'h':
          if(bigendian) while(count-->0) { ret[ri++] = (int)ReadBE2(data, ai); ai+=2; }
          else while(count-->0) { ret[ri++] = (int)ReadLE2(data, ai); ai+=2; }
          break;
        
        case 'H':
          if(bigendian) while(count-->0) { ret[ri++] = (int)ReadBE2U(data, ai); ai+=2; }
          else while(count-->0) { ret[ri++] = (int)ReadLE2U(data, ai); ai+=2; }
          break;

        case 'i': case 'l':
          if(bigendian) while(count-->0) { ret[ri++] = ReadBE4(data, ai); ai+=4; }
          else while(count-->0) { ret[ri++] = ReadLE4(data, ai); ai+=4; }
          break;

        case 'I': case 'L':
          while(count-->0)
          { uint v = bigendian ? ReadBE4U(data, ai) : ReadLE4U(data, ai);
            ret[ri++] = v>int.MaxValue ? (long)v : (object)(int)v;
            ai+=4;
          }
          break;

        case 'f': 
          while(count-->0) { ret[ri++] = (double)ReadFloat(data, ai); ai+=4; }
          break;

        case 'd': 
          while(count-->0) { ret[ri++] = ReadDouble(data, ai); ai+=8; }
          break;

        case 'q':
          while(count-->0)
          { long v = bigendian ? ReadBE8(data, ai) : ReadLE8(data, ai);
            ret[ri++] = v<=int.MaxValue && v>=int.MinValue ? (int)v : (object)v;
            ai+=8;
          }
          break;

        case 'Q':
          while(count-->0)
          { ulong v = bigendian ? ReadBE8U(data, ai) : ReadLE8U(data, ai);
            ret[ri++] = v<=int.MaxValue ? (int)v : v<=long.MaxValue ? (long)v : (object)v;
            ai+=8;
          }
          break;

        case 'P':
          if(IntPtr.Size==4)
          { if(bigendian) while(count-->0) { ret[ri++] = ReadBE4(data, ai); ai+=4; }
            else while(count-->0) { ret[ri++] = ReadLE4(data, ai); ai+=4; }
          }
          else
            while(count-->0)
            { long v = bigendian ? ReadBE8(data, ai) : ReadLE8(data, ai);
              ret[ri++] = v<=int.MaxValue && v>=int.MinValue ? (int)v : (object)v;
              ai+=8;
            }
          break;

        case '<': bigendian=false; break;
        case '>': case '!': bigendian=true; break;
        #if BIGENDIAN
        case '=': bigendian=true; break;
        #else
        case '=': bigendian=false; break;
        #endif
      }
    }
    return new Tuple(ret);
  }

  static int calcsize(string format, out int args)
  { int i=0, size=0, numargs=0;

    while(i<format.Length)
    { int count;
      while(i<format.Length && char.IsWhiteSpace(format[i])) i++;
      if(i==format.Length) break;
      if(char.IsDigit(format[i]))
      { count = format[i++]-'0';
        while(i<format.Length && char.IsDigit(format[i])) count = count*10 + (format[i++]-'0');
        if(i==format.Length) throw Ops.ValueError("format string ended prematurely");
      }
      else count = 1;
      
      char code = format[i++];
      if(code=='@' || code=='=' || code=='<' || code=='>' || code=='!') continue;
      if(count>0 && code!='x') numargs += code=='s' || code=='p' ? 1 : count;
      switch(code)
      { case 'x': case 'c': case 'b': case 'B': case 's': case 'p': size += count; break;
        case 'h': case 'H': size += count*2; break;
        case 'i': case 'I': case 'l': case 'L': case 'f': size += count*4; break;
        case 'q': case 'Q': case 'd': size += count*8; break;
        case 'P': size += count*IntPtr.Size; break;
        default: throw Ops.ValueError("unexpected character '{0}' (0x{1:x}) in format string", code, (int)code);
      }
    }
    args=numargs;
    return size;
  }

  public static readonly ReflectedType error = ReflectedType.FromType(typeof(ValueErrorException));

  #region Packing/unpacking
  static short ReadLE2(byte[] buf, int index) { return (short)(buf[index]|(buf[index+1]<<8)); }
  static short ReadBE2(byte[] buf, int index) { return (short)((buf[index]<<8)|buf[index+1]); }
  static int ReadLE4(byte[] buf, int index)
  { return (int)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }
  static int ReadBE4(byte[] buf, int index)
  { return (int)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }
  static long ReadLE8(byte[] buf, int index) { return ReadLE4U(buf, index)|((long)ReadLE4(buf, index+4)<<32); }
  static long ReadBE8(byte[] buf, int index)
  { return ((long)ReadBE4(buf, index)<<32)|(uint)ReadBE4(buf, index+4);
  }
  static ushort ReadLE2U(byte[] buf, int index) { return (ushort)(buf[index]|(buf[index+1]<<8)); }
  static ushort ReadBE2U(byte[] buf, int index) { return (ushort)((buf[index]<<8)|buf[index+1]); }
  static uint ReadLE4U(byte[] buf, int index)
  { return (uint)(buf[index]|(buf[index+1]<<8)|(buf[index+2]<<16)|(buf[index+3]<<24));
  }
  static uint ReadBE4U(byte[] buf, int index)
  { return (uint)((buf[index]<<24)|(buf[index+1]<<16)|(buf[index+2]<<8)|buf[index+3]);
  }
  static ulong ReadLE8U(byte[] buf, int index)
  { return ReadLE4U(buf, index)|((ulong)ReadLE4U(buf, index+4)<<32);
  }
  static ulong ReadBE8U(byte[] buf, int index)
  { return ((ulong)ReadBE4U(buf, index)<<32)|ReadBE4U(buf, index+4);
  }
  static unsafe float ReadFloat(byte[] buf, int index)
  { fixed(byte* ptr=buf) return *(float*)(ptr+index);
  }
  static unsafe double ReadDouble(byte[] buf, int index)
  { fixed(byte* ptr=buf) return *(double*)(ptr+index);
  }

  static void WriteLE2(byte[] buf, int index, short val)
  { buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
  }
  static void WriteBE2(byte[] buf, int index, short val)
  { buf[index]   = (byte)(val>>8);
    buf[index+1] = (byte)val;
  }
  static void WriteLE4(byte[] buf, int index, int val)
  { buf[index]   = (byte)val;
    buf[index+1] = (byte)(val>>8);
    buf[index+2] = (byte)(val>>16);
    buf[index+3] = (byte)(val>>24);
  }
  static void WriteBE4(byte[] buf, int index, int val)
  { buf[index]   = (byte)(val>>24);
    buf[index+1] = (byte)(val>>16);
    buf[index+2] = (byte)(val>>8);
    buf[index+3] = (byte)val;
  }
  static void WriteLE8(byte[] buf, int index, long val)
  { WriteLE4(buf, index, (int)val);
    WriteLE4(buf, index+4, (int)(val>>32));
  }
  static void WriteBE8(byte[] buf, int index, long val)
  { WriteBE4(buf, index, (int)(val>>32));
    WriteBE4(buf, index+4, (int)val);
  }
  static unsafe void WriteFloat(byte[] buf, int index, float val)
  { fixed(byte* pbuf=buf) *(float*)(pbuf+index) = val;
  }
  static unsafe void WriteDouble(byte[] buf, int index, double val)
  { fixed(byte* pbuf=buf) *(double*)(pbuf+index) = val;
  }
  #endregion
}

} // namespace Boa.Modules