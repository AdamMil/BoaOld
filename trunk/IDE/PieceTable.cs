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
using System.Collections;
using System.IO;
using System.Text;

namespace Boa.IDE
{

// TODO: make 'add' write into a file on disk rather than a memory buffer
// TODO: make PieceList singly linked if there's no need for it to be doubly linked

public class PieceTable
{ public PieceTable() : this(null) { }
  public PieceTable(string text)
  { add    = new char[1024];
    pieces = new PieceList();

    baseData = text==null ? "" : text;
    if(baseData.Length!=0) pieces.Append(new Piece(0, baseData.Length, true));
  }

  public event EventHandler LineCountChanged;

  public string[] Lines
  { get
    { if(cachedLines==null)
      { StringBuilder sb = new StringBuilder();
        ArrayList lines = new ArrayList();

        foreach(Piece p in pieces)
        { int i=0, offset=p.Offset, length=p.Length;
          if(p.File)
          { for(; i<length; i++) if(baseData[i+offset]=='\n') break;
            sb.Append(baseData, offset, i);
            if(i!=length)
            { lines.Add(sb.ToString());
              sb.Length = 0;
              if(++i<length) sb.Append(baseData, offset+i, length-i);
            }
          }
          else
          { for(; i<length; i++) if(add[i+offset]=='\n') break;
            sb.Append(add, offset, i);
            if(i!=length)
            { lines.Add(sb.ToString());
              sb.Length = 0;
              if(++i<length) sb.Append(add, offset+i, length-i);
            }
          }
        }
        if(sb.Length!=0) lines.Add(sb.ToString());
        
        cachedLines = (string[])lines.ToArray(typeof(string));
      }
      return cachedLines;
    }

    set
    { if(value==null) throw new ArgumentNullException("Lines");
      Text = string.Join("\n", value); // TODO: optimize this for the case where cacheLines!=null
    }
  }

  public int LineCount { get { return Lines.Length; } }

  public string Text
  { get
    { if(cachedText==null)
      { StringBuilder sb = new StringBuilder();
        foreach(Piece p in pieces)
          if(p.File) sb.Append(baseData, p.Offset, p.Length);
          else sb.Append(add, p.Offset, p.Length);
        cachedText = sb.ToString();
      }
      return cachedText;
    }

    set
    { if(value==null) throw new ArgumentNullException("Text");

      int start=0, len=Math.Min(textLength, value.Length), end=len;
      unsafe
      { fixed(char* os=Text)
        fixed(char* ns=value) // find common prefix and suffix
        { for(; start<len; start++) if(os[start]!=ns[start]) break;
          if(start==end) return;
          char* oe=os+Text.Length, ne=ns+value.Length;
          for(; end!=0; end--) if(oe[end]!=ns[end]) break;
        }
      }

      if(end!=0) Replace(start, end, value);
    }
  }

  public int TextLength { get { return textLength; } }

  public void Append(char c) { Insert(textLength, c); }
  public void Append(string text) { Insert(textLength, text); }

  public void Clear() { Delete(0, textLength); }

  public void Delete(int pos) { Delete(pos, 1); }
  public void Delete(int pos, int length)
  { if(pos<0 || length<0 || pos>=textLength) throw new ArgumentOutOfRangeException();

    Piece piece = pieces.Head;
    int offset=0, count=0;
    do
    { count += piece.Length;
      if(count>pos) break;
      offset += piece.Length;
      piece = piece.Next;
    } while(piece!=null);
    if(piece==null) return;

    offset = pos-offset;
    pos    = piece.Offset + offset; // convert logical offset to physical offset within node
    if(length==1) // special case single character deletion
    { if(pos==piece.Offset) { piece.Offset++; piece.Length--; }
      else if(pos==piece.End-1) piece.Length--;
      else
      { pieces.InsertAfter(piece, new Piece(pos+1, piece.End-(pos+1), piece.File));
        piece.Length = pos-piece.Offset;
        goto done;
      }
    }
    else
    { int end=pos+length, len=length;
      while(len>0)
      { Piece next = piece.Next;
        if(pos==piece.Offset)
        { int plen = piece.Length;
          if(plen<=len) pieces.Remove(piece);
          else { piece.Offset+=len; piece.Length-=len; }
          len -= plen;
        }
        else if(end>=piece.End)
        { int nlen = pos-piece.Offset;
          len -= piece.Length-nlen;
          piece.Length = nlen;
        }
        else
        { pieces.InsertAfter(piece, new Piece(pos+len, piece.End-(pos+len), piece.File));
          piece.Length = pos-piece.Offset;
          break;
        }

        if(next==null) break;
        piece=next; pos=piece.Offset; end=pos+len;
      }
    }

    if(piece.Length==0) pieces.Remove(piece);

    done:
    textLength -= length;
    OnChanged();
  }

  public int GetCharIndexFromLine(int line)
  { if(line<0 || line>=LineCount) throw new ArgumentOutOfRangeException();
    string[] lines = Lines;
    int pos=0;
    for(int i=0; i<line; i++) pos += lines[i].Length+1;
    return pos;
  }

  public int GetLineFromCharIndex(int index)
  { if(index<0 || index>=textLength) throw new ArgumentOutOfRangeException();
    string[] lines = Lines;
    int pos=0, len;
    while(pos<lines.Length && index >= (len=lines[pos].Length+1)) index -= len;
    return pos;
  }

  public void Insert(int pos, char c)
  { Insert(pos, 1);
    Add(c);
  }
  
  public void Insert(int pos, string text)
  { if(text==null) throw new ArgumentNullException("text");
    Insert(pos, text.Length);
    Add(text);
  }

  public void Replace(int start, int length, string text)
  { if(length==0) Insert(start, text);
    else
    { if(text==null) throw new ArgumentNullException("text");
      Delete(start, length);
      Insert(start, text);
    }
  }

  public sealed class Piece
  { public Piece(int offset) { Offset=offset; Length=1; }
    public Piece(int offset, int length, bool file) { Offset=offset; Length=length; File=file; }
    public int End { get { return Offset+Length; } }

    public Piece Previous { get { return prev; } }
    public Piece Next { get { return next; } }

    public int  Offset, Length;
    public bool File;

    internal Piece prev, next;
  }

  #region PieceList
  sealed class PieceList : IEnumerable
  { public int Count { get { return count; } }

    #region IEnumerable
    /// <summary>Represents an enumerator for a <see cref="LinkedList"/> collection.</summary>
    public sealed class Enumerator : IEnumerator
    { internal Enumerator(PieceList list) { this.list=list; head=list.head; version=list.Version; Reset(); }

      public object Current
      { get
        { if(cur==null) throw new InvalidOperationException("Invalid position");
          return cur;
        }
      }

      public bool MoveNext()
      { AssertNotChanged();
        if(cur==null)
        { if(!reset || head==null) return false;
          cur   = head;
          reset = false;
          return true;
        }
        cur = cur.next;
        return cur!=null;
      }

      public void Reset() { AssertNotChanged(); cur=null; reset=true; }

      void AssertNotChanged()
      { if(list.Version!=version) throw new InvalidOperationException("The collection has changed");
      }

      PieceList list;
      Piece head, cur;
      uint  version;
      bool  reset;
    }

    public IEnumerator GetEnumerator() { return new Enumerator(this); }
    #endregion

    public Piece Head { get { return head; } }
    public Piece Tail { get { return tail; } }

    public Piece Append(Piece newNode)
    { if(tail==null)
      { count=1;
        newNode.next=newNode.prev=null;
        return head=tail=newNode;
      }
      else return InsertAfter(tail, newNode);
    }

    public Piece Prepend(Piece newNode)
    { if(head==null)
      { count=1;
        newNode.next=newNode.prev=null;
        return head=tail=newNode;
      }
      else return InsertBefore(head, newNode);
    }

    public Piece InsertAfter(Piece node, Piece newNode)
    { if(node==null || newNode==null) throw new ArgumentNullException();
      newNode.prev = node;
      newNode.next = node.next;
      node.next    = newNode;
      if(node==tail) tail=newNode;
      else newNode.next.prev=newNode;
      count++; Version++;
      return newNode;
    }

    public Piece InsertBefore(Piece node, Piece newNode)
    { if(node==null || newNode==null) throw new ArgumentNullException();
      newNode.prev = node.prev;
      newNode.next = node;
      node.prev    = newNode;
      if(node==head) head=newNode;
      else newNode.prev.next=newNode;
      count++; Version++;
      return newNode;
    }

    public void Remove(Piece node)
    { if(node==null) return;
      if(node==head) head=node.next;
      else node.prev.next=node.next;
      if(node==tail) tail=node.prev;
      else node.next.prev=node.prev;
      count--; Version++;
      #if DEBUG
      node.next = node.prev = null;
      #endif
    }

    public void Clear() { head=tail=null; count=0; }

    internal uint Version;

    Piece head, tail;
    int count;
  }
  #endregion

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
  { if(pos<0 || length<0 || pos>textLength) throw new ArgumentOutOfRangeException();

    if(pos==textLength || pieces.Count==0)
    { pieces.Append(new Piece(addi, length, false));
      goto done;
    }
    else if(pos==0)
    { pieces.Prepend(new Piece(addi, length, false));
      goto done;
    }

    Piece node=pieces.Head, piece;
    int offset=0, count=0;
    do
    { piece  = node;
      count += piece.Length;
      if(count>=pos) break;
      node    = node.Next;
      offset += piece.Length;
    } while(node!=null);

    if(node==null || pos==count)
    { if(piece.File) pieces.Append(new Piece(addi, length, false));
      else piece.Length += length;
    }
    else
    { pos = piece.Offset + (pos-offset); // convert logical offset to physical offset within node
      node = pieces.InsertAfter(piece, new Piece(addi, length, false));
      pieces.InsertAfter(node, new Piece(pos, piece.End-pos, piece.File));
      piece.Length = pos-piece.Offset;
    }
    
    done:
    textLength += length;
    OnChanged();
  }

  void OnChanged()
  { cachedText=null;
    cachedLines=null;
  }

  void OnLineCountChanged()
  { if(LineCountChanged!=null) LineCountChanged(this, EventArgs.Empty);
  }
  
  string baseData, cachedText;
  string[] cachedLines;
  char[] add;
  int    addi, textLength;
  PieceList pieces;

}

} // namespace Boa.IDE