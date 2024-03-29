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
using System.Reflection;
using System.Reflection.Emit;
using Boa.Runtime;

namespace Boa.AST
{

public sealed class CodeGenerator
{ public CodeGenerator(TypeGenerator tg, MethodBase mb, ILGenerator ilg)
  { TypeGenerator = tg; MethodBase = mb; ILG = ilg;
  }

  public Slot AllocLocalTemp(Type type) { return AllocLocalTemp(type, false); }
  public Slot AllocLocalTemp(Type type, bool keepAround)
  { if(localTemps==null) localTemps = new ArrayList();
    if(keepAround && IsGenerator) return Namespace.AllocTemp(type);
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
  { if(!MethodBase.IsStatic) index++;
    switch(index)
    { case 0: ILG.Emit(OpCodes.Ldarg_0); break;
      case 1: ILG.Emit(OpCodes.Ldarg_1); break;
      case 2: ILG.Emit(OpCodes.Ldarg_2); break;
      case 3: ILG.Emit(OpCodes.Ldarg_3); break;
      default: ILG.Emit(index<256 ? OpCodes.Ldarg_S : OpCodes.Ldarg, index); break;
    }
  }
  public void EmitArgGetAddr(int index)
  { if(!MethodBase.IsStatic) index++;
    ILG.Emit(index<256 ? OpCodes.Ldarga_S : OpCodes.Ldarga, index);
  }
  public void EmitArgSet(int index)
  { if(!MethodBase.IsStatic) index++;
    ILG.Emit(index<256 ? OpCodes.Starg_S : OpCodes.Starg, index);
  }

  public void EmitBool(bool value) { ILG.Emit(value ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0); }

  public void EmitCall(ConstructorInfo ci) { ILG.Emit(OpCodes.Call, ci); }
  public void EmitCall(MethodInfo mi)
  { if(mi.IsVirtual && !mi.DeclaringType.IsSealed && !mi.IsFinal) ILG.Emit(OpCodes.Callvirt, mi);
    else ILG.Emit(OpCodes.Call, mi);
  }
  public void EmitCall(Type type, string method) { EmitCall(type.GetMethod(method)); }
  public void EmitCall(Type type, string method, Type[] paramTypes) { EmitCall(type.GetMethod(method, paramTypes)); }

  public void EmitConstant(object value)
  { if(value==null) ILG.Emit(OpCodes.Ldnull);
    else if(value is bool) EmitFieldGet(typeof(Ops), (bool)value ? "TRUE" : "FALSE");
    else
    { string s = value as string;
      if(s!=null) EmitString(s); // TODO: should strings be passed to TypeGenerator as well (and stored in one place?)
      else TypeGenerator.GetConstant(value).EmitGet(this);
    }
  }

  public void EmitDouble(double value) { ILG.Emit(OpCodes.Ldc_R8, value); }

  public void EmitExpression(Expression e)
  { if(e==null) ILG.Emit(OpCodes.Ldnull);
    else e.Emit(this);
  }

  public void EmitFieldGet(Type type, string name) { EmitFieldGet(type.GetField(name)); }
  public void EmitFieldGet(FieldInfo field) { ILG.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field); }
  public void EmitFieldGetAddr(Type type, string name) { EmitFieldGetAddr(type.GetField(name)); }
  public void EmitFieldGetAddr(FieldInfo field)
  { ILG.Emit(field.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, field);
  }
  public void EmitFieldSet(Type type, string name) { EmitFieldSet(type.GetField(name)); }
  public void EmitFieldSet(FieldInfo field) { ILG.Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field); }

  public void EmitDel(Name name) { Namespace.DeleteSlot(name); }
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
				if(value>=-128 && value<=127) ILG.Emit(OpCodes.Ldc_I4_S, (byte)value);
				else ILG.Emit(OpCodes.Ldc_I4, value);
				return;
		}
		ILG.Emit(op);
  }
  
  public void EmitIsFalse()
  { EmitIsTrue();
    EmitInt(0);
    ILG.Emit(OpCodes.Ceq);
  }

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
  { EmitExpression(expr);
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
  { if(MethodBase.IsStatic) throw new InvalidOperationException("no 'this' for a static method");
    ILG.Emit(OpCodes.Ldarg_0);
  }
  
  public void EmitTypeOf(Type type)
  { ILG.Emit(OpCodes.Ldtoken, type);
    EmitCall(typeof(Type), "GetTypeFromHandle");
  }

  public void Finish()
  {
  }

  public void FreeLocalTemp(Slot slot) { localTemps.Add(slot); }

  public void SetArgs(Name[] names) { Namespace.SetArgs(names, 0, MethodBase); }
  public void SetArgs(Name[] names, int offset) { Namespace.SetArgs(names, offset, MethodBase); }

  public Namespace Namespace;
  public bool IsGenerator;

  public readonly TypeGenerator TypeGenerator;
  public readonly MethodBase MethodBase;
  public readonly ILGenerator   ILG;

  ArrayList localTemps;
}

} // namespace Boa.AST
