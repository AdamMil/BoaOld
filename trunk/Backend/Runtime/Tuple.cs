using System;
using System.Collections;

// TODO: don't allow __repr__ to go into an infinite loop with circular references

namespace Boa.Runtime
{

[BoaType("tuple")]
public class Tuple : ISequence, ICollection, IComparable, IRepresentable
{ public Tuple() { items = Misc.EmptyArray; }
  public Tuple(ICollection col)
  { items = new object[col.Count];
    col.CopyTo(items, 0);
  }
  public Tuple(object obj)
  { List list = new List(obj);
    items = new object[list.Count];
    list.CopyTo(items, 0);
  }
  internal Tuple(params object[] items) { this.items=items; }

  #region ISequence Members
  public object __add__(object o)
  { Tuple tup = o as Tuple;
    if(tup==null) throw Ops.TypeError("cannot concatenate tuple to {0}", Ops.GetDynamicType(o).__name__);
    object[] arr = new object[items.Length+tup.items.Length];
    items.CopyTo(arr, 0);
    tup.items.CopyTo(arr, items.Length);
    return new Tuple(arr);
  }

  public object __getitem__(int index) { return items[Ops.FixIndex(index, items.Length)]; }
  public object __getitem__(Slice slice) { return Ops.SequenceSlice(this, slice); }
  public int __len__() { return items.Length; }
  public bool __contains__(object value)
  { for(int i=0; i<items.Length; i++) if(Ops.Compare(items[i], value)==0) return true;
    return false;
  }
  #endregion

  #region ICollection Members
  public bool IsSynchronized { get { return items.IsSynchronized; } }
  public int Count { get { return items.Length; } }
  public void CopyTo(Array array, int index) { items.CopyTo(array, index); }
  public object SyncRoot { get { return items; } }
  #endregion

  #region IEnumerable Members
  public IEnumerator GetEnumerator() { return items.GetEnumerator(); }
  #endregion

  #region IComparable Members
  public int CompareTo(object o)
  { Tuple tup = o as Tuple;
    if(tup!=null) return ArrayOps.Compare(items, items.Length, tup.items, tup.items.Length);
    else return -1; // FIXME: compare by type name
  }
  #endregion

  #region IRepresentable Members
  public string __repr__()
  { System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.Append('(');
    for(int i=0; i<items.Length; i++)
    { if(i>0) sb.Append(", ");
      sb.Append(Ops.Repr(items[i]));
    }
    if(items.Length==1) sb.Append(',');
    sb.Append(')');
    return sb.ToString();
  }
  #endregion

  public override bool Equals(object o) { return CompareTo(o)==0; }

  public override int GetHashCode() // TODO: improve hashing
  { int ret = 0, inc = Math.Max(items.Length/10, 1); // limit to 10 items (bad idea?)
    for(int i=0; i<items.Length; i+=inc) ret ^= items[i].GetHashCode();
    return ret;
  }


  public override string ToString() { return __repr__(); }

  public static readonly Tuple Empty = new Tuple();
  
  internal object[] items;
}

} // namespace Boa.Runtime