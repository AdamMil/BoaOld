using System;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

public class TypeGenerator
{ public TypeGenerator(AssemblyGenerator assembly, TypeBuilder typeBuilder)
  { Assembly=assembly; TypeBuilder=typeBuilder;
  }

  public Slot ModuleField
  { get
    { if(moduleField==null) moduleField = AddStaticSlot(Boa.Runtime.Module.FieldName, typeof(Boa.Runtime.Module));
      return moduleField;
    }
  }

  public Slot AddStaticSlot(string name, Type type)
  { return new StaticSlot(TypeBuilder.DefineField(name, type, FieldAttributes.Public|FieldAttributes.Static));
  }

  public CodeGenerator DefineMethod(string name, Type retType, Type[] paramTypes)
  { return DefineMethod(MethodAttributes.Public|MethodAttributes.Static, name, retType, paramTypes);
  }
  public CodeGenerator DefineMethod(MethodAttributes attrs, string name, Type retType, Type[] paramTypes)
  { MethodBuilder mb = TypeBuilder.DefineMethod(name, attrs, retType, paramTypes);
    return new CodeGenerator(this, mb, mb.GetILGenerator());
  }
  
  public Type FinishType()
  { if(initGen!=null) initGen.ILG.Emit(OpCodes.Ret);
    Type ret = TypeBuilder.CreateType();
    // finish nested types
    return ret;
  }

  public Slot GetConstant(object value)
  { Slot slot = (Slot)constants[value];
    if(slot!=null) return slot;

    FieldBuilder fb = TypeBuilder.DefineField("c$"+constants.Count, typeof(object), FieldAttributes.Static);
    constants[value] = slot = new StaticSlot(fb);
    EmitConstantInitializer(value);
    initGen.EmitFieldSet(fb);

    return slot;
  }

  void EmitConstantInitializer(object value)
  { CodeGenerator cg = GetInitializer();

    if(value is Tuple)
    { Tuple tup = (Tuple)value;
      cg.EmitObjectArray(tup.items);
      cg.EmitNew(typeof(Tuple), new Type[] { typeof(object[]) });
    }
    else if(value is List)
    { List list = (List)value;
      cg.EmitInt(list.Count);
      cg.EmitNew(typeof(List), new Type[] { typeof(int) });
      MethodInfo mi = typeof(List).GetMethod("append");
      foreach(object o in list)
      { cg.ILG.Emit(OpCodes.Dup);
        GetConstant(o).EmitGet(cg);
        cg.EmitCall(mi);
      }
    }
    else if(value is Dict)
    { Dict dict = (Dict)value;
      cg.EmitInt(dict.Count);
      cg.EmitNew(typeof(Dict), new Type[] { typeof(int) });
      MethodInfo mi = typeof(Dict).GetMethod("Add");
      foreach(DictionaryEntry e in dict)
      { cg.ILG.Emit(OpCodes.Dup);
        GetConstant(e.Key).EmitGet(cg);
        GetConstant(e.Value).EmitGet(cg);
        cg.EmitCall(mi);
      }
    }
    else if(value is Slice)
    { Slice slice = (Slice)value;
      cg.EmitConstant(slice.start);
      cg.EmitConstant(slice.stop);
      cg.EmitConstant(slice.step);
      cg.EmitNew(typeof(Slice), new Type[] { typeof(object), typeof(object), typeof(object) });
    }
    else switch(Convert.GetTypeCode(value)) // TODO: see if this is faster than using 'is'
    { case TypeCode.Int32:
        cg.EmitInt((int)value);
        cg.ILG.Emit(OpCodes.Box, typeof(int));
        break;
      case TypeCode.Double:
        cg.ILG.Emit(OpCodes.Ldc_R8, (double)value);
        cg.ILG.Emit(OpCodes.Box, typeof(double));
        break;
      default: throw new NotImplementedException("constant: "+value.GetType());
    }
  }

  public CodeGenerator GetInitializer()
  { if(initGen==null) initGen = new CodeGenerator(this, null, TypeBuilder.DefineTypeInitializer().GetILGenerator());
    return initGen;
  }

  public AssemblyGenerator Assembly;
  public TypeBuilder TypeBuilder;

  HybridDictionary constants = new HybridDictionary();
  CodeGenerator initGen;
  Slot moduleField;
}

} // namespace Boa.AST
