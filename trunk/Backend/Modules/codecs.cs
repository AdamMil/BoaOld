using System;
using System.Text;
using Boa.Runtime;

// TODO: make this conform more closely to python's module
// TODO: wrap encoder and decoder classes to make them more accessible
// TODO: add base64 encoder/decoder

namespace Boa.Modules
{

[BoaType("module")]
public sealed class codecs
{ codecs() { }

  #region Encodings
  // must override GetEncoder() and GetDecoder() to prevent infinite loops!
  public abstract class StatefulEncoding : Encoding
  { public override int GetByteCount(char[] chars, int index, int count)
    { return GetEncoder().GetByteCount(chars, index, count, true);
    }
    
    public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
    { return GetEncoder().GetBytes(chars, charIndex, charCount, bytes, byteIndex, true);
    }
    
    public override int GetCharCount(byte[] bytes, int index, int count)
    { return GetDecoder().GetCharCount(bytes, index, count);
    }

    public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
    { return GetDecoder().GetChars(bytes, byteIndex, byteCount, chars, charIndex);
      
    }
  }

  public class Base64Encoding : StatefulEncoding
  { 
  }

  public class HexEncoding : Encoding
  {
  }
  
  public class QuoPriEncoding : ???
  {
  }
  
  public class Rot13Encoding : Encoding
  {
  }
  
  public class EscapeEncoding : StatefulEncoding
  {
  }
  
  public class UUEncoding : ???
  {
  }
  
  public class ZLibEncoding : StatefulEncoding
  {
  }
  #endregion
  
  #region Support classes
  public class encoder
  { public encoder(Encoder encoder) { enc=encoder; }
    
    Encoder enc;
  }
  
  public class decoder
  { public decoder(Decoder decoder) { dec=decoder; }
    
    Decoder dec;
  }
  #endregion
  
  public static string __repr__() { return __str__(); }
  public static string __str__() { return "<module 'codecs' (built-in)>"; }
  
  public static byte[] convert(Encoding from, Encoding to, byte[] bytes)
  { return Encoding.Convert(from, to, bytes);
  }
  public static byte[] convert(Encoding from, Encoding to, byte[] bytes, int offset, int length)
  { return Encoding.Convert(from, to, bytes, offset, length);
  }

  public static Decoder getdecoder(string encoding) { return new decoder(lookup(encoding).GetDecoder()); }
  public static Encoder getencoder(string encoding) { return new encoder(lookup(encoding).GetEncoder()); }

  public static Encoding lookup(string encoding)
  { use registered functions;
    switch(encoding)
    { case "base64_codec": if(base64==null) base64=new Base64Encoding(); return base64;
      case "hex_codec": if(hex==null) hex=new HexEncoding(); return hex;
      case "quopri_codec": if(quopri==null) quopri=new QuoPriEncoding(); return quopri;
      case "rot_13": if(rot13==null) rot13=new Rot13Encoding(); return rot13;
      case "string_escape": case "unicode_escape": case "raw_unicode_escape":
        if(escape==null) escape=new EscapeEncoding(); return escape;
      case "uu_codec": if(uu==null) uu=new UUEncoding(); return uu;
      case "zlib_codec": if(zlib==null) zlib=new ZLibEncoding(); return zlib;
      default:
        try { return Encoding.GetEncoding(encoding); }
        catch(NotSupportedException) { throw Ops.LookupError("codec '{0}' not supported on this system"); }
    }
  }
  
  public static void register(object function);

  public static readonly Encoding ascii = Encoding.ASCII;
  public static readonly Encoding bigendianunicode = Encoding.BigEndianUnicode;
  public static readonly Encoding @default = Encoding.Default;
  public static readonly Encoding unicode = Encoding.Unicode;
  public static readonly Encoding utf7 = Encoding.UTF7;
  public static readonly Encoding utf8 = Encoding.UTF8;
  
  static Encoding base64, hex, quopri, rot13, escape, uu, zlib;
}

} // namespace Boa.Modules