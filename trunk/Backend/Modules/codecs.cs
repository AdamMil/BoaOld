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
using System.Collections;
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

  
  #region Support classes
  public class BoaEncoding // TODO: : Encoding
  { public BoaEncoding(Tuple tup) { encoder=tup.items[0]; decoder=tup.items[1]; }

    object encoder, decoder;
  }

  public class encoder
  { public encoder(Encoder encoder) { enc=encoder; }
    
    Encoder enc;
  }
  
  public class decoder
  { public decoder(Decoder decoder) { dec=decoder; }
    
    Decoder dec;
  }
  #endregion
  
  public static string __repr__() { return "<module 'codecs' (built-in)>"; }
  public static string __str__() { return __repr__(); }
  
  public static byte[] convert(Encoding from, Encoding to, byte[] bytes)
  { return Encoding.Convert(from, to, bytes);
  }
  public static byte[] convert(Encoding from, Encoding to, byte[] bytes, int offset, int length)
  { return Encoding.Convert(from, to, bytes, offset, length);
  }

  public static decoder getdecoder(string encoding) { return new decoder(lookup(encoding).GetDecoder()); }
  public static decoder getdecoder(Encoding encoding) { return new decoder(encoding.GetDecoder()); }
  public static encoder getencoder(string encoding) { return new encoder(lookup(encoding).GetEncoder()); }
  public static encoder getencoder(Encoding encoding) { return new encoder(encoding.GetEncoder()); }

  public static Encoding lookup(string encoding)
  { foreach(object lf in lookups)
    { object ret = Ops.Call(lf, encoding);
      if(ret!=null)
      { if(ret is Encoding) return (Encoding)ret;
        Tuple tup = ret as Tuple;
        if(tup==null || tup.Count!=2)
          throw Ops.TypeError("lookup(): expected null or (encoder, decoder), but got {0}", Ops.Repr(ret));
        // TODO: return new BoaEncoding(ret);
        throw new NotImplementedException("BoaEncoding");
      }
    }

    try { return Encoding.GetEncoding(encoding); }
    catch(NotSupportedException) { throw Ops.LookupError("codec '{0}' not supported on this system"); }
  }
  
  public static void register(object function)
  { if(lookups==null) lookups = new ArrayList();
    lookups.Add(function);
  }

  public static readonly Encoding ascii = Encoding.ASCII;
  public static readonly Encoding bigendianunicode = Encoding.BigEndianUnicode;
  public static readonly Encoding @default = Encoding.Default;
  public static readonly Encoding unicode = Encoding.Unicode;
  public static readonly Encoding utf7 = Encoding.UTF7;
  public static readonly Encoding utf8 = Encoding.UTF8;
  
  static ArrayList lookups;
}

} // namespace Boa.Modules