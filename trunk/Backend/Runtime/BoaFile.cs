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
using System.IO;

namespace Boa.Runtime
{

#region FileEnumerator
public class FileEnumerator : IEnumerator
{ public FileEnumerator(IFile file) { this.file=file; state=State.BOF; }

  public object Current
  { get
    { if(state!=State.IN) throw new InvalidOperationException();
      return line;
    }
  }

  public bool MoveNext()
  { if(state==State.EOF) return false;
    line = file.readline();
    if(line==null) { state=State.EOF; return false; }
    state = State.IN; return true;
  }

  public void Reset() { file.seek(0); state=State.BOF; }

  enum State { BOF, IN, EOF };
  IFile file;
  string line;
  State state;
}
#endregion

#region BoaFile
[BoaType("file")]
[DocString(@"file(filename[, mode[, bufsize]])
file(stream)

Return a new file object. In the first form, 'filename' is the file name to
be opened and 'mode' indicates how the file is to be opened:
'r' for reading, 'w' for writing (truncating an existing file), and 'a'
opens it for appending (which on some systems means that all writes append
to the end of the file, regardless of the current seek position).

Modes 'r+', 'w+' and 'a+' open the file for updating (note that 'w+'
truncates the file). Append 'b' to the mode to open the file in binary mode,
on systems that differentiate between binary and text files (else it is
ignored). If the file cannot be opened, IOError is raised.

If mode is omitted, it defaults to 'r'. When opening a binary file, you
should append 'b' to the mode value for improved portability. (It's useful
even on systems which don't treat binary and text files differently, where
it serves as documentation.) The optional bufsize argument specifies the
file's desired buffer size: 0 means unbuffered, 1 means line buffered, any
other positive value means use a buffer of (approximately) that size.
A negative bufsize means to use the system default, which is usually line
buffered for tty devices and fully buffered for other files. If omitted,
the system default is used.")]
public class BoaFile : IFile, IEnumerable
{ public BoaFile(string filename) : this(File.Open(filename, FileMode.Open, FileAccess.Read))
  { source=filename; smode="r";
  }

  public BoaFile(string filename, string mode)
  { char type = 'r';
    bool plus=false;
    for(int i=0; i<mode.Length; i++)
      switch(mode[i])
      { case 'r': case 'w': case 'a': type=mode[i]; break;
        case '+': plus=true; break;
        case 'b': case 'U': break;
        default: throw Ops.ValueError("unrecognized character {0} in file mode", mode[i]);
      }

    FileMode fmode = type=='a' ? FileMode.Append : type=='w' ? FileMode.Create :
                       plus ? FileMode.OpenOrCreate : FileMode.Open;
    FileAccess access = plus ? FileAccess.ReadWrite : type=='a' || type=='w' ? FileAccess.Write : FileAccess.Read;
    stream = File.Open(filename, fmode, access);
    source = filename;
    smode  = mode;
  }

  public BoaFile(string filename, string mode, int bufsize) : this(filename, mode) { }
  public BoaFile(Stream stream) { this.stream = stream; source = "<stream>"; smode = string.Empty; }

  public string mode { get { return smode; } }
  public string name { get { return source; } }

  #region IEnumerable
  public IEnumerator GetEnumerator() { return new FileEnumerator(this); }
  #endregion

  #region IFile Members
  public bool canread { get { return stream.CanRead; } }
  public bool canseek { get { return stream.CanSeek; } }
  public bool canwrite { get { return stream.CanWrite; } }
  public bool closed { get { return stream==null; } }
  public System.Text.Encoding encoding { get { return enc; } set { enc = value; } }
  public int length { get { return (int)stream.Length; } }

  public void close()
  { if(stream!=null)
    { stream.Close();
      stream = null;
    }
  }

  public void flush()
  { AssertOpen();
    try { stream.Flush(); }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }

  public bool isatty() { throw Ops.NotImplementedError("isatty(): not implemented"); }

  public string next()
  { string line = readline();
    if(line==null) throw new StopIterationException();
    return line;
  }

  public byte[] read()
  { AssertOpen();
    if(stream.CanSeek) return read((int)(stream.Length-stream.Position));
    try
    { byte[] buf = new byte[4096];
      int total=0;
      while(true)
      { int toread = buf.Length-total, bytes = doread(buf, total, toread);
        total += bytes;
        if(bytes<toread) break;
        byte[] narr = new byte[buf.Length*2];
        buf.CopyTo(narr, 0);
        buf = narr;
      }
      if(total==buf.Length) return buf;
      byte[] ret = new byte[total];
      Array.Copy(buf, ret, total);
      return ret;
    }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }
  
  public string readstr() { return Encoding.GetString(read()); }

  public byte[] read(int bytes)
  { AssertOpen();
    try
    { byte[] buf = new byte[bytes];
      int read = doread(buf, 0, bytes);
      if(bytes==read) return buf;
      byte[] ret = new byte[read];
      Array.Copy(buf, ret, read);
      return ret;
    }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }

  public string readstr(int bytes) { return Encoding.GetString(read(bytes)); }

  public int readbyte()
  { AssertOpen();
    try
    { if(bufLen>0)
      { int ret = buf[0];
        Array.Copy(buf, 1, buf, 0, --bufLen); // this is really slow
        return ret;
      }
      return stream.ReadByte();
    }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }

  public string readline() { return readline(-1); }
  public string readline(int max)
  { AssertOpen();
    try
    { int pos=0;
      while(true)
      { if(bufLen>pos)
        { bool trimOne=false;
          int idx = Array.IndexOf(buf, (byte)'\n', pos, bufLen-pos);
          if(idx==-1)
          { idx = Array.IndexOf(buf, (byte)'\r', pos, bufLen-pos);
            if(idx!=-1) buf[idx] = (byte)'\n';
          }
          else if(idx>0 && buf[idx-1]=='\r') { trimOne=true; buf[--idx]=(byte)'\n'; }

          if(idx==-1) pos = bufLen;
          else
          { if(max>=0 && idx>max) { idx=max; trimOne=false; }
            string ret = Encoding.GetString(buf, 0, ++idx);
            if(trimOne) idx++;
            bufLen -= idx;
            if(bufLen>0) Array.Copy(buf, idx, buf, 0, bufLen);
            return ret;
          }
        }

        if(max>=0 && pos>=max)
        { string ret = Encoding.GetString(buf, 0, max);
          bufLen -= max;
          if(bufLen>0) Array.Copy(buf, max, buf, 0, bufLen);
          return ret;
        }
        
        int toread=128, read;
        if(bufLen+toread>buf.Length)
        { byte[] narr = new byte[buf.Length*2];
          Array.Copy(buf, narr, bufLen);
          buf = narr;
        }
        read = stream.Read(buf, bufLen, toread);
        if(read==0) break;
        bufLen += read;
      }
      
      if(bufLen==0) return null;
      else
      { string ret = Encoding.GetString(buf, 0, bufLen);
        bufLen = 0;
        return ret;
      }
    }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }

  public List readlines()
  { List list = new List();
    while(true)
    { string line = readline(-1);
      if(line==null) break;
      list.append(line);
    }
    return list;
  }

  public List readlines(int sizehint) { return readlines(); }

  public int seek(int offset) { return (int)stream.Seek(offset, SeekOrigin.Begin); }
  public int seek(int offset, int whence)
  { SeekOrigin origin = whence==1 ? SeekOrigin.Current : whence==2 ? SeekOrigin.End : SeekOrigin.Begin;
    return (int)stream.Seek(offset, origin);
  }

  public int tell() { return (int)stream.Position; }

  public void truncate() { truncate((int)stream.Position); }
  public void truncate(int size)
  { AssertOpen();
    try { clearBuffer(); stream.SetLength(size); }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }

  public void write(byte[] bytes) { write(bytes, 0, bytes.Length); }
  public void write(string str) { write(Encoding.GetBytes(str)); }

  public void writebyte(int value)
  { AssertOpen();
    try { clearBuffer(); stream.WriteByte((byte)value); }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }

  public void writelines(object sequence)
  { IEnumerator e = Ops.GetEnumerator(sequence);
    while(e.MoveNext()) write(Ops.ToString(e.Current));
  }
  #endregion

  public void write(byte[] bytes, int offset, int length)
  { AssertOpen();
    try { clearBuffer(); stream.Write(bytes, offset, length); }
    catch(IOException e) { throw Ops.IOError(e.Message); }
  }

  System.Text.Encoding Encoding { get { return enc==null ? System.Text.Encoding.Default : enc; } }

  void AssertOpen() { if(stream==null) throw Ops.IOError("operation attempted on a closed file"); }

  void clearBuffer()
  { if(bufLen>0 && stream.CanSeek)
    { stream.Position -= bufLen;
      bufLen = 0;
    }
  }

  int doread(byte[] buffer, int offset, int length)
  { if(length==0) return 0;
    int total=0;

    if(bufLen>0)
    { int tocopy = Math.Min(length, bufLen);
      Array.Copy(buf, 0, buffer, offset, tocopy);
      bufLen -= tocopy;
      if(bufLen>0) Array.Copy(buf, tocopy, buf, 0, bufLen);
      
      total  += tocopy;
      length -= tocopy;
      if(length==0) return total;
      offset += tocopy;
    }

    while(true)
    { int read = stream.Read(buffer, offset, length);
      if(read==0) return total;
      total += read;
      length -= read;
      if(length==0) return total;
      offset += read;
    }
  }
  
  System.Text.Encoding enc;
  Stream stream;
  string source, smode;
  byte[] buf = new byte[128];
  int    bufLen;
}
#endregion

} // namespace Boa.Runtime