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
using System.Collections;

// TODO: don't allow __repr__ to go into an infinite loop with circular references

namespace Boa.Runtime
{

[BoaType("list")]
public class List : IMutableSequence, IList, IComparable, ICloneable, IRepresentable
{ public List() { items = new object[16]; }
  public List(int capacity) { items = new object[Math.Max(capacity, 4)]; }
  public List(ICollection c) : this(c.Count) { c.CopyTo(items, 0); size=c.Count; }
  public List(IEnumerator e) : this() { while(e.MoveNext()) append(e.Current); }
  public List(object o) : this(Ops.GetEnumerator(o)) { }
  internal List(object[] arr, int length) { items=arr; size=length; }

  public void append(object item)
  { ResizeTo(size+1);
    items[size++] = item;
  }

  public int count(object item)
  { int num=0;
    for(int i=0; i<size; i++) if(items[i].Equals(item)) num++;
    return num;
  }

  public void extend(object seq)
  { List list = this;
    if(!TryAppend(seq, ref list))
    { IEnumerator e = Ops.GetEnumerator(seq);
      while(e.MoveNext()) append(e.Current);
    }
  }

  public override bool Equals(object o) { return CompareTo(o)==0; }
  public override int GetHashCode() { throw Ops.TypeError("list objects are unhashable"); }

  public int index(object item) { return IndexOrError(IndexOf(item, 0, size)); }
  public int index(object item, int start)
  { start = Ops.FixIndex(start, size);
    return IndexOrError(IndexOf(item, start, size-start));
  }
  public int index(object item, int start, int end)
  { start = Ops.FixIndex(start, size);
    end   = Ops.FixIndex(end, size);
    if(start>end) { int t=end; end=start; start=t; }
    return IndexOrError(IndexOf(item, start, end-start));
  }

  public void insert(int index, object o) { Insert(index==size ? index : Ops.FixIndex(index, size), o); }

  public object pop()
  { if(size==0) throw Ops.ValueError("pop off of empty list");
    return items[--size];
  }

  public void remove(object item) { RemoveAt(index(item)); }

  public void RemoveRange(int start, int count)
  { if(start<0 || start+count>size) throw Ops.ValueError("RemoveRange(): indices out of range");
    size -= count;
    for(; start<size; start++) items[start] = items[start+count];
    for(int i=0; i<count; i++) items[size+i] = null;
  }
  
  public void reverse() { Array.Reverse(items, 0, size); }

  public void sort() { Array.Sort(items, 0, size, Ops.DefaultComparer); }
  public void sort(object cmpfunc) { Array.Sort(items, 0, size, new FunctionComparer(cmpfunc)); }

  public List sorted()
  { List ret = new List(this);
    ret.sort();
    return ret;
  }
  public List sorted(object cmpfunc)
  { List ret = new List(this);
    ret.sort(cmpfunc);
    return ret;
  }

  public Tuple ToTuple()
  { object[] ti = new object[size];
    items.CopyTo(ti, 0);
    return new Tuple(ti);
  }

  #region IMutableSequence Members
  public object __add__(object o)
  { List list=null;
    if(!TryAppend(o, ref list))
      throw Ops.TypeError("can not concatenate list to ('{0}')", Ops.GetDynamicType(o).__name__);
    return list;
  }

  public bool __contains__(object value) { return IndexOf(value, 0, size) != -1; }

  public void __delitem__(int index) { RemoveAt(Ops.FixIndex(index, size)); }
  public void __delitem__(Slice slice)
  { Tuple tup = slice.indices(size);
    int start=(int)tup.items[0], stop=(int)tup.items[1], step=(int)tup.items[2];
    if(step<0 && start<=stop || step>0 && start>=stop) return;

    if(step==1) RemoveRange(start, stop-start);
    else if(step==-1) RemoveRange(stop+1, start-stop);
    else
    { int off=1;
      for(int i=start,next=start+step; i<size; i++)
      { if(i+off==next) { if((next+=step)>=stop) next=0; else off++; }
        items[i] = items[i+off];
      }
      size -= off;
    }
  }

  public object __getitem__(int index) { return items[Ops.FixIndex(index, size)]; }
  public object __getitem__(Slice slice) { return Ops.SequenceSlice(this, slice); }

  public int __len__() { return size; }

  public void __setitem__(int index, object value) { items[Ops.FixIndex(index, size)] = value; }
  public void __setitem__(Slice slice, object value)
  { Tuple tup = slice.indices(size);
    int start=(int)tup.items[0], stop=(int)tup.items[1], step=(int)tup.items[2];
    if(step<0 && start<=stop || step>0 && start>=stop) return;
    int sign=Math.Sign(step), slen=(stop-start+step-sign)/step;

    ISequence seq = value as ISequence;
    if(seq==null && value is string) seq = new StringOps.SequenceWrapper((string)value);

    int len = seq==null ? Ops.ToInt(Ops.Invoke(value, "__len__")) : seq.__len__();
    if(step==1 || step==-1)
    { int diff = Math.Abs(len-slen);
      if(step==1)
      { if(len<slen) RemoveRange(start+len, diff);
        else if(len>slen) Insert(start+slen, diff);
      }
      else if(len>slen) { Insert(stop+1, diff); start += diff; }
      else if(len<slen) { RemoveRange(stop+1, diff); start -= diff; }
    }
    else if(len!=slen)
      throw Ops.ValueError("can't assign sequence of size {0} to extended slice of size {1}", len, slen);

    if(seq!=null)
    { if(step==1) for(int i=0; i<len; i++) items[i+start] = seq.__getitem__(i);
      else if(step==-1) for(int i=0; i<len; i++) items[start-i] = seq.__getitem__(i);
      else if(step>0) for(int i=0; start<stop; i++,start+=step) items[start] = seq.__getitem__(i);
      else for(int i=0; start>stop; i++,start+=step) items[start] = seq.__getitem__(i);
    }
    else
    { object getitem = Ops.GetAttr(value, "__getitem__");
      if(step==1) for(int i=0; i<len; i++) items[i+start] = Ops.Call(getitem, i);
      else if(step==-1) for(int i=0; i<len; i++) items[start-i] = Ops.Call(getitem, i);
      else if(step>0) for(int i=0; start<stop; i++,start+=step) items[start] = Ops.Call(getitem, i);
      else for(int i=0; start>stop; i++,start+=step) items[start] = Ops.Call(getitem, i);
    }
  }
  #endregion

  #region IList Members
  public bool IsReadOnly { get { return false; } }
  public object this[int index]
  { get
    { if(index<0 || index>=size) throw new IndexOutOfRangeException();
      return items[index];
    }
    set
    { if(index<0 || index>=size) throw new IndexOutOfRangeException();
      items[index] = value;
    }
  }

  public void RemoveAt(int index)
  { if(index<0 || index>=size) throw new IndexOutOfRangeException();
    size--;
    for(; index<size; index++) items[index] = items[index+1];
    items[size] = null;
  }

  public void Insert(int index, object value)
  { if(index<0 || index>size) throw new IndexOutOfRangeException();
    ResizeTo(size+1);
    for(int i=size++; i>index; i--) items[i] = items[i-1];
    items[index] = value;
  }

  public void Remove(object value)
  { int index = IndexOf(value);
    if(index!=-1) RemoveAt(index);
  }

  public bool Contains(object value) { return __contains__(value); }
  public void Clear() { size=0; }

  public int IndexOf(object value) { return IndexOf(value, 0, size); }
  public int IndexOf(object value, int start, int length)
  { int end = start+length;
    if(start<0 || end>size) throw new ArgumentOutOfRangeException();
    for(; start<end; start++) if(Ops.Compare(items[start], value)==0) return start;
    return -1;
  }

  public int Add(object value) { append(value); return size-1; }
  public bool IsFixedSize { get { return false; } }
  #endregion

  #region ICollection Members
  public bool IsSynchronized { get { return false; } }
  public int Count { get { return size; } }
  public void CopyTo(Array array, int index) { Array.Copy(items, 0, array, index, size); }
  public object SyncRoot { get { return this; } }
  #endregion

  #region IEnumerable Members
  public IEnumerator GetEnumerator() { return new ListEnumerator(this); }
  
  class ListEnumerator : IEnumerator
  { public ListEnumerator(List list) { this.list=list; index=-1; }

    public void Reset() { index=-1; }

    public object Current
    { get
      { if(index<0 || index>=list.size) throw new InvalidOperationException();
        return list.items[index];
      }
    }

    public bool MoveNext()
    { if(index>=list.size-1) return false;
      index++;
      return true;
    }

    List list;
    int index;
  }
  #endregion

  #region IComparable Members
  public int CompareTo(object o)
  { List list = o as List;
    return list==null ? -1 : ArrayOps.Compare(items, size, list.items, list.size); // FIXME: compare by type name
  }
  #endregion

  #region ICloneable Members
  public object Clone() { return new List(this); }
  #endregion

  #region IRepresentable Members
  public string __repr__()
  { System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.Append('[');
    for(int i=0; i<size; i++)
    { if(i>0) sb.Append(", ");
      sb.Append(Ops.Repr(items[i]));
    }
    sb.Append(']');
    return sb.ToString();
  }
  #endregion

  public override string ToString() { return __repr__(); }
  
  class FunctionComparer : IComparer
  { public FunctionComparer(object func) { this.func=func; }
    public int Compare(object x, object y) { return Ops.ToInt(Ops.Call(func, x, y)); }
    object func;
  }

  int IndexOrError(int index)
  { if(index<0) throw Ops.ValueError("item not in list");
    return index;
  }

  void Insert(int start, int count)
  { ResizeTo(size+count);
    for(int i=size-1; i>=start; i--) items[i+count] = items[i];
    size += count;
  }

  void ResizeTo(int capacity)
  { if(capacity>items.Length)
    { int len = Math.Max(items.Length, 4);
      while(len<capacity) len*=2;
      object[] arr = new object[len];
      Array.Copy(items, arr, size);
      items = arr;
    }
  }

  bool TryAppend(object o, ref List list)
  { ICollection col = o as ICollection;
    if(col!=null)
    { if(list==null)
      { list = new List(size+col.Count);
        Array.Copy(items, list.items, size);
      }
      else list.ResizeTo(size+col.Count);
      col.CopyTo(list.items, size);
      list.size = size+col.Count;
    }
    else
    { IEnumerator e;
      if(Ops.GetEnumerator(o, out e))
      { if(list==null) list = new List(this);
        while(e.MoveNext()) list.append(e.Current);
      }
      else return false;
    }
    return true;
  }

  object[] items;
  int size;
}

} // namespace Boa.Runtime