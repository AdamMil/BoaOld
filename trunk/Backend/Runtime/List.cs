using System;
using System.Collections;

namespace Boa.Runtime
{

public class List : IMutableSequence, IList, IComparable
{ public List() { items = new object[16]; }
  public List(int capacity) { items = new object[Math.Max(capacity, 4)]; }
  public List(ICollection c) : this(c.Count) { c.CopyTo(items, index); }
  public List(IEnumerator e) : this() { while(e.MoveNext(append(e.Current))); }

  public void append(object item)
  { ResizeTo(size+1);
    data[size++] = item;
  }

  public int count(object item)
  { int num=0;
    for(int i=0; i<size; i++) if(items[i].Equals(item)) num++;
    return num;
  }

  public void extend(object seq)
  { if(!TryAppend(seq))
    { IEnumerator e = Ops.GetEnumerator(seq);
      while(e.MoveNext()) append(e.Current);
    }
  }

  public override bool Equals(object obj) { return CompareTo(obj)==0; }
  public override int GetHashCode() { throw Ops.TypeError("list objects are unhashable"); }

  public void index(object item) { return IndexOrError(Array.IndexOf(items, item, 0, size)); }
  public void index(object item, int start)
  { start = Ops.FixIndex(start, size);
    return IndexOrError(Array.IndexOf(items, item, start, size-start));
  }
  public void index(object item, int start, int end)
  { start = Ops.FixIndex(start, size);
    end   = Ops.FixIndex(end, size);
    if(start>end) { int t=end; end=start; start=t; }
    return IndexOrError(Array.IndexOf(items, item, start, end-start));
  }

  public void insert(int index, object obj) { Insert(Ops.FixIndex(index, size), obj); }

  public object pop()
  { if(size==0) throw Ops.ValueError("pop off of empty list");
    return items[--size];
  }

  public void remove(object item) { RemoveAt(index(item)); }
  public void reverse() { Array.Reverse(items, 0, size); }
  public void sort() { Array.Sort(items, 0, size, Ops.DefaultComparer); }
  public void sort(object cmpfunc) { Array.Sort(items, 0, size, new FunctionComparer(cmpfunc)); }

  #region IMutableSequence Members
  public object __add__(object obj)
  { List list;
    if(!TryAppend(obj, out list))
      throw Ops.TypeError("can not concatenate list to ('{0}')", Ops.GetDynamicType(obj).__name__);
    return list;
  }

  public bool __contains__(object value) { return Array.IndexOf(items, value, 0, size)>=0; }
  public void __delitem__(int index) { RemoveAt(Ops.FixIndex(index, size)); }
  public object __getitem__(int index) { return data[Ops.FixIndex(index, size)]; }
  public int __len__() { return size; }
  public void __setitem__(int index, object value) { items[Ops.FixIndex(index, size)] = value; }
  #endregion

  #region IList Members
  public bool IsReadOnly { get { return false; } }
  public object this[int index]
  { get
    { if(index>=size) throw new IndexOutOfRangeException();
      return items[index];
    }
    set
    { if(index>=size) throw new IndexOutOfRangeException();
      items[index] = value;
    }
  }

  public void RemoveAt(int index)
  { if(index<0 || index>=size) throw new IndexOutOfRangeException();
    size--;
    for(; i<size; index++) items[index] = items[index+1];
  }

  public void Insert(int index, object value)
  { if(index<0 || index>=size) throw new IndexOutOfRangeException();
    ResizeTo(size+1);
    for(int i=size++; i>index; i--) items[i] = items[i-1];
    items[index] = value;
  }

  public void Remove(object value) { remove(value); }
  public bool Contains(object value) { return __contains__(value); }
  public void Clear() { size=0; }
  public int IndexOf(object value) { return Array.IndexOf(items, value, 0, size); }
  public int Add(object value) { append(value); return size-1; }
  public bool IsFixedSize { get { return false; } }
  #endregion

  #region ICollection Members
  public bool IsSynchronized { get { return false; } }
  public int Count { get { return size; } }
  public void CopyTo(Array array, int index) { return items.CopyTo(array, index); }
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
    { if(index>=list.size) return false;
      index++;
      return true;
    }

    List list;
    int index;
  }
  #endregion

  #region IComparable Members
  public int CompareTo(object obj)
  { List list = obj as List;
    if(list!=null) Ops.CompareArrays(items, size, list.items, list.size);
    else return -1; // FIXME: compare by type name
  }
  #endregion

  public override string ToString()
  { System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.Append('[');
    for(int i=0; i<size; i++)
    { if(i>0) sb.Append(", ");
      sb.Append(Ops.Repr(items[i]));
    }
    sb.Append(']');
    return sb.ToString();
  }

  class FunctionComparer : IComparer
  { public FunctionComparer(object func) { this.func=func; }
    public int Compare(object x, object y) { return Ops.ObjectToInt(Ops.Call(func, x, y)); }
    object func;
  }

  int IndexOrError(int index)
  { if(index<0) throw Ops.ValueError("item not in list");
    return index;
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

  bool TryAppend(object obj, out List list)
  { if(obj is ICollection)
    { list = new List(size+list.Count);
      Array.Copy(items, list.items, size);
      ((IList)obj).CopyTo(list.items, size);
      list.size = size+list.Count;
    }
    else if(obj is IEnumerable)
    { list = new List(this);
      IEnumerator e = ((IEnumerable)obj).GetEnumerator();
      while(e.MoveNext()) list.append(e.Current);
    }
    else if(obj is IEnumerator)
    { list = new List(this);
      IEnumerator e = (IEnumerator)e;
      while(e.MoveNext()) list.append(e.Current);
    }
    else return false;
    return true;
  }

  object[] items;
  int size;
}

} // namespace Boa.Runtime