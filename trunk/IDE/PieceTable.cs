/*
Boaide is a simple IDE for working with the Boa language.

Boa is the reference implementation for a language similar to Python,
also called Boa. This implementation is both interpreted and compiled,
targetting the Microsoft .NET Framework.

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
using System.IO;
using System.Text;
using GameLib.Collections;
using GameLib.IO;

namespace Boa.IDE
{

// TODO: support unicode files
public class PieceTable
{ public class Piece
  { public Piece(int offset) { Offset=offset; Length=1; }
    public Piece(int offset, int length, bool file) { Offset=offset; Length=length; File=file; }
    public int End { get { return Offset+Length; } }

    public int  Offset, Length;
    public bool File;
  }

  public PieceTable() : this(null, null) { }
  public PieceTable(Stream file, Encoding encoding)
  { add    = new char[1024];
    pieces = new LinkedList();

    if(file!=null)
    { if(!file.CanRead || !file.CanSeek) throw new ArgumentException("Source file must be seekable and readable");
      if(encoding==null) throw new ArgumentNullException("encoding"); // TODO: support auto detection
      if(file.Length>0) pieces.Append(new Piece(0, (int)file.Length, true));
      this.file = file;
    }
    this.encoding = encoding;
  }

  public Encoding Encoding
  { get { return encoding; }
    set
    { if(value==null) throw new ArgumentNullException("Encoding", "encoding cannot be null");
      encoding = value;
    }
  }

  public void Delete(int pos) { Delete(pos, 1); }
  public void Delete(int pos, int length)
  { if(pos<0) return;

    LinkedList.Node node = pieces.Head;
    Piece piece;
    int offset=0, count=0;
    do
    { piece  = (Piece)node.Data;
      count += piece.Length;
      if(count>pos) break;
      offset += piece.Length;
      node = node.NextNode;
    } while(node!=null);
    if(node==null) return;

    offset = pos-offset;
    pos    = piece.Offset + offset; // convert logical offset to physical offset
    if(length==1) // special case single character deletion
    { if(pos==piece.Offset) { piece.Offset++; piece.Length--; }
      else if(pos==piece.End-1) piece.Length--;
      else
      { pieces.InsertAfter(node, new Piece(pos+1, piece.End-(pos+1), piece.File));
        piece.Length = pos-piece.Offset;
        return;
      }
    }
    else
    { int end = pos+length;
      while(length>0)
      { LinkedList.Node next = node.NextNode;
        if(pos==piece.Offset)
        { int plen = piece.Length;
          if(plen<=length) pieces.Remove(node);
          else { piece.Offset+=length; piece.Length-=length; }
          length -= plen;
        }
        else if(end>=piece.End)
        { int nlen = pos-piece.Offset;
          length -= piece.Length-nlen;
          piece.Length = nlen;
        }
        else
        { pieces.InsertAfter(node, new Piece(pos+length, piece.End-(pos+length), piece.File));
          piece.Length = pos-piece.Offset;
          break;
        }
        
        if(next==null) break;
        node   = next;
        piece  = (Piece)node.Data;
        pos    = piece.Offset; end = pos+length;
      }
    }
    
    if(piece.Length==0) pieces.Remove(node);
  }

  public void Insert(char c, int pos)
  { Insert(pos, 1);
    Add(c);
  }
  
  public void Insert(string str, int pos)
  { Insert(pos, str.Length);
    Add(str);
  }

  public override string ToString()
  { StringBuilder sb = new StringBuilder();
    foreach(Piece p in pieces)
      if(p.File)
      { file.Position = p.Offset;
        sb.Append(encoding.GetString(IOH.Read(file, p.Length)));
      }
      else sb.Append(add, p.Offset, p.Length);
    return sb.ToString();
  }

  void Add(char c)
  { if(addi==add.Length)
    { char[] narr = new char[addi*2];
      Array.Copy(add, narr, addi);
      add = narr;
    }
    add[addi++] = c;
  }

  void Add(string str)
  { int need = addi+str.Length;
    if(need>add.Length)
    { int alloc = add.Length*2;
      while(alloc<need) alloc*=2;
      char[] narr = new char[alloc];
      Array.Copy(add, narr, addi);
      add = narr;
    }
    str.CopyTo(0, add, addi, str.Length);
    addi = need;
  }

  void Insert(int pos, int length)
  { if(pos<=0 || pieces.Count==0)
    { pieces.Prepend(new Piece(addi, length, false));
      return;
    }

    LinkedList.Node node = pieces.Head;
    Piece piece;
    int offset=0, count=0;
    do
    { piece  = (Piece)node.Data;
      count += piece.Length;
      if(count>=pos) break;
      node   = node.NextNode;
      offset += piece.Length;
    } while(node!=null);

    if(node==null || pos==count)
    { if(piece.File) pieces.Append(new Piece(addi, length, false));
      else piece.Length += length;
    }
    else
    { pos = piece.Offset + (pos-offset); // convert logical offset to physical offset
      LinkedList.Node cnode = pieces.InsertAfter(node, new Piece(addi, length, false));
      pieces.InsertAfter(cnode, new Piece(pos, piece.End-pos, piece.File));
      piece.Length = pos-piece.Offset;
    }
  }

  Stream file;
  char[] add;
  int    addi;
  LinkedList pieces;

  Encoding encoding;
}

} // namespace Boa.IDE