using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

public class CodeGenerator
{ public CodeGenerator(TypeGenerator tg, MethodBuilder mb, ILGenerator ilg)
  { TypeGenerator = tg; MethodBuilder = mb; ILG = ilg;
  }

  public Slot AllocLocalTemp(Type type)
  { if(localTemps==null) localTemps = new ArrayList();
    Slot ret;
    for(int i=localTemps.Count-1; i>=0; i--)
    { ret = (Slot)localTemps[i];
      if(ret.Type==type)
      { localTemps.RemoveAt(i);
        return ret;
      }
    }
    return new LocalSlot(ILG.DeclareLocal(type));
  }

  public void EmitArgGet(int index)
  { if(!MethodBuilder.IsStatic) index++;
    switch(index)
    { case 0: ILG.Emit(OpCodes.Ldarg_0); break;
      case 1: ILG.Emit(OpCodes.Ldarg_1); break;
      case 2: ILG.Emit(OpCodes.Ldarg_2); break;
      case 3: ILG.Emit(OpCodes.Ldarg_3); break;
      default: ILG.Emit(index<256 ? OpCodes.Ldarg_S : OpCodes.Ldarg, index); break;
    }
  }
  public void EmitArgGetAddr(int index)
  { if(!MethodBuilder.IsStatic) index++;
    ILG.Emit(index<256 ? OpCodes.Ldarga_S : OpCodes.Ldarga, index);
  }
  public void EmitArgSet(int index)
  { if(!MethodBuilder.IsStatic) index++;
    ILG.Emit(index<256 ? OpCodes.Starg_S : OpCodes.Starg, index);
  }

  public void EmitCall(MethodInfo mi)
  { if(mi.IsVirtual) ILG.Emit(OpCodes.Callvirt, mi);
    else ILG.Emit(OpCodes.Call, mi);
  }
  public void EmitCall(Type type, string method) { EmitCall(type.GetMethod(method)); }
  public void EmitCall(Type type, string method, Type[] paramTypes) { EmitCall(type.GetMethod(method, paramTypes)); }

  public void EmitConstant(object value)
  { if(value==null) ILG.Emit(OpCodes.Ldnull);
    else if(value is bool) EmitFieldGet(typeof(Ops), (bool)value ? "TRUE" : "FALSE");
    else
    { string s = value as string;
      if(s!=null) EmitString(s);
      else TypeGenerator.GetConstant(value).EmitGet(this);
    }
  }

  public void EmitFieldGet(Type type, string name) { EmitFieldGet(type.GetField(name)); }
  public void EmitFieldGet(FieldInfo field) { ILG.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field); }
  public void EmitFieldGetAddr(Type type, string name) { EmitFieldGetAddr(type.GetField(name)); }
  public void EmitFieldGetAddr(FieldInfo field)
  { ILG.Emit(field.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, field);
  }
  public void EmitFieldSet(Type type, string name) { EmitFieldSet(type.GetField(name)); }
  public void EmitFieldSet(FieldInfo field) { ILG.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field); }

  public void EmitGet(Name name) { Namespace.GetSlotForGet(name).EmitGet(this); }
  public void EmitSet(Name name) { Namespace.GetSlotForSet(name).EmitSet(this); }
  
  public void EmitPropGet(Type type, string name) { EmitPropGet(type.GetProperty(name)); }
  public void EmitPropGet(PropertyInfo pi) { EmitCall(pi.GetGetMethod()); }
  public void EmitPropSet(Type type, string name) { EmitPropSet(type.GetProperty(name)); }
  public void EmitPropSet(PropertyInfo pi) { EmitCall(pi.GetSetMethod()); }

  public void EmitInt(int value)
  { OpCode op;
		switch(value)
		{ case -1: op=OpCodes.Ldc_I4_M1; break;
			case  0: op=OpCodes.Ldc_I4_0; break;
			case  1: op=OpCodes.Ldc_I4_1; break;
			case  2: op=OpCodes.Ldc_I4_2; break;
			case  3: op=OpCodes.Ldc_I4_3; break;
			case  4: op=OpCodes.Ldc_I4_4; break;
			case  5: op=OpCodes.Ldc_I4_5; break;
			case  6: op=OpCodes.Ldc_I4_6; break;
			case  7: op=OpCodes.Ldc_I4_7; break;
			case  8: op=OpCodes.Ldc_I4_8; break;
			default:
				if(value>=-128 && value<=127) ILG.Emit(OpCodes.Ldc_I4_S, value);
				else ILG.Emit(OpCodes.Ldc_I4, value);
				return;
		}
		ILG.Emit(op);
  }
  
  public void EmitIsFalse() { EmitIsTrue(); ILG.Emit(OpCodes.Not); }
  public void EmitIsFalse(Expression e) { e.Emit(this); EmitIsFalse(); }
  public void EmitIsTrue() { EmitCall(typeof(Ops), "IsTrue"); }
  public void EmitIsTrue(Expression e) { e.Emit(this); EmitIsTrue(); }

  public void EmitLine(int line)
  { if(TypeGenerator.Assembly.Symbols!=null)
      ILG.MarkSequencePoint(TypeGenerator.Assembly.Symbols, line, 0, line+1, 0);
  }

  public void EmitNew(Type type) { ILG.Emit(OpCodes.Newobj, type.GetConstructor(Type.EmptyTypes)); }
  public void EmitNew(Type type, Type[] paramTypes) { ILG.Emit(OpCodes.Newobj, type.GetConstructor(paramTypes)); }
  public void EmitNew(ConstructorInfo ci) { ILG.Emit(OpCodes.Newobj, ci); }

  public void EmitNewArray(Type type, int length)
  { EmitInt(length);
    ILG.Emit(OpCodes.Newarr, type);
  }

  public void EmitObjectArray(Expression[] exprs)
  { EmitNewArray(typeof(object), exprs.Length);
    for(int i=0; i<exprs.Length; i++)
    { ILG.Emit(OpCodes.Dup);
      EmitInt(i);
      exprs[i].Emit(this);
      ILG.Emit(OpCodes.Stelem_Ref);
    }
  }

  public void EmitObjectArray(object[] objs)
  { EmitNewArray(typeof(object), objs.Length);
    for(int i=0; i<objs.Length; i++)
    { ILG.Emit(OpCodes.Dup);
      EmitInt(i);
      EmitConstant(objs[i]);
      ILG.Emit(OpCodes.Stelem_Ref);
    }
  }

  // TODO: make this use actual spans
  public void EmitPosition(Node node)
  { if(TypeGenerator.Assembly.Symbols!=null)
      ILG.MarkSequencePoint(TypeGenerator.Assembly.Symbols, node.Line, node.Column, node.Line, node.Column+1);
  }

  public void EmitReturn() { ILG.Emit(OpCodes.Ret); }
  public void EmitReturn(Expression expr)
  { if(expr==null) ILG.Emit(OpCodes.Ldnull);
    else expr.Emit(this);
    ILG.Emit(OpCodes.Ret);
  }
  
  public void EmitString(string value)
  { if(value==null) ILG.Emit(OpCodes.Ldnull);
    else ILG.Emit(OpCodes.Ldstr, value);
  }

  public void EmitStringArray(string[] strings)
  { EmitNewArray(typeof(string), strings.Length);
    for(int i=0; i<strings.Length; i++)
    { ILG.Emit(OpCodes.Dup);
      EmitInt(i);
      EmitString(strings[i]);
      ILG.Emit(OpCodes.Stelem_Ref);
    }
  }

  public void EmitThis()
  { if(MethodBuilder.IsStatic) throw new InvalidOperationException("no 'this' for a static method");
    ILG.Emit(OpCodes.Ldarg_0);
  }

  public void Finish()
  {
  }

  public void FreeLocalTemp(Slot slot) { localTemps.Add(slot); }

  public void SetArgs(Name[] names) { Namespace.SetArgs(names, 0, MethodBuilder); }
  public void SetArgs(Name[] names, int offset) { Namespace.SetArgs(names, offset, MethodBuilder); }

  public Namespace Namespace;

  public readonly TypeGenerator TypeGenerator;
  public readonly MethodBuilder MethodBuilder;
  public readonly ILGenerator   ILG;

  ArrayList localTemps;
}

} // namespace Boa.AST
