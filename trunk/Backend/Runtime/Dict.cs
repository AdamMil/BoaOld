using System;
using System.Collections;
using System.Collections.Specialized;

namespace Boa.Runtime
{

[BoaType("dict")]
public class Dict : HybridDictionary, IComparable, IMapping
{ public Dict() { }
  public Dict(IDictionary dict) { foreach(DictionaryEntry e in dict) Add(e.Key, e.Value); }

  public void clear() { Clear(); }

  public override int GetHashCode() { throw Ops.TypeError("dict objects are unhashable"); }

  public override bool Equals(object o)
  { if(o is Dict)
    { List l1 = items();
      l1.sort();
      List l2 = ((Dict)o).items();
      l2.sort();
      return l1.Equals(l2);
    }
    else return false;
  }
  
  public List items()
  { List ret = new List(Count);
    foreach(DictionaryEntry e in this) ret.Add(new Tuple(e.Key, e.Value));
    return ret;
  }

  public List keys() { return new List(Keys); }

  public override string ToString()
  { System.Text.StringBuilder sb = new System.Text.StringBuilder();
    sb.Append('{');
    bool first=true;
    foreach(DictionaryEntry e in this)
    { if(first) first=false;
      else sb.Append(", ");
      sb.Append(Ops.Repr(e.Key));
      sb.Append(": ");
      sb.Append(Ops.Repr(e.Value));
    }
    sb.Append('}');
    return sb.ToString();
  }

  public List values() { return new List(Values); }

  #region IComparable Members
  public int CompareTo(object o)
  { if(o is Dict)
    { List l1 = items();
      l1.sort();
      List l2 = ((Dict)o).items();
      l2.sort();
      return l1.CompareTo(l2);
    }
    else return -1; // FIXME: compare by type names, like python does
  }
  #endregion

  #region IMapping Members
  public object get(object key) { return this[key]; }
  object Boa.Runtime.IMapping.get(object key, object defaultValue)
  { return Contains(key) ? this[key] : defaultValue;
  }

  public void __delitem__(object key) { Remove(key); }
  public object __getitem__(object key)
  { if(Contains(key)) return this[key];
    else throw Ops.KeyError(Ops.Repr(key));
  }
  public void __setitem__(object key, object value) { this[key]=value; }
  public int __len__() { return Count; }
  public bool __contains__(object key) { return Contains(key); }
  #endregion
}

} // namespace Boa.Runtime