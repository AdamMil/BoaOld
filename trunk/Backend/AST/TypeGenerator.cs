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
    { if(moduleField==null) moduleField = DefineStaticField(Boa.Runtime.Module.FieldName, typeof(Boa.Runtime.Module));
      return moduleField;
    }
  }

  public CodeGenerator DefineChainedConstructor(ConstructorInfo parent)
  { ParameterInfo[] pi = parent.GetParameters();
    Type[] types = GetParamTypes(pi);
    ConstructorBuilder cb = TypeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, types);
    for(int i=0; i<pi.Length; i++)
    { ParameterBuilder pb = cb.DefineParameter(i+1, pi[i].Attributes, pi[i].Name);
      if(pi[i].IsDefined(typeof(ParamArrayAttribute), false))
        pb.SetCustomAttribute(
          new CustomAttributeBuilder(typeof(ParamArrayAttribute).GetConstructor(Type.EmptyTypes), Misc.EmptyArray));
    }
    
    CodeGenerator cg = new CodeGenerator(this, cb, cb.GetILGenerator());
    cg.EmitThis();
    for(int i=0; i<pi.Length; i++) cg.EmitArgGet(i);
    cg.ILG.Emit(OpCodes.Call, parent);
    return cg;
  }

  public CodeGenerator DefineDefaultConstructor(MethodAttributes attrs)
  { ConstructorBuilder cb = TypeBuilder.DefineDefaultConstructor(attrs);
    return new CodeGenerator(this, cb, cb.GetILGenerator());
  }

  public Slot DefineField(string name, Type type) { return DefineField(name, type, FieldAttributes.Public); }
  public Slot DefineField(string name, Type type, FieldAttributes access)
  { return new FieldSlot(new ThisSlot(TypeBuilder), TypeBuilder.DefineField(name, type, access));
  }

  public CodeGenerator DefineMethod(string name, Type retType, Type[] paramTypes)
  { return DefineMethod(MethodAttributes.Public|MethodAttributes.Static, name, retType, paramTypes);
  }
  public CodeGenerator DefineMethod(MethodAttributes attrs, string name, Type retType, Type[] paramTypes)
  { MethodBuilder mb = TypeBuilder.DefineMethod(name, attrs, retType, paramTypes);
    return new CodeGenerator(this, mb, mb.GetILGenerator());
  }

  public CodeGenerator DefineMethodOverride(MethodInfo baseMethod)
  { MethodAttributes attrs = baseMethod.Attributes & ~MethodAttributes.Abstract;
    MethodBuilder mb = TypeBuilder.DefineMethod(baseMethod.Name, attrs, baseMethod.ReturnType,
                                                GetParamTypes(baseMethod.GetParameters()));
    // TODO: figure out how to use this properly
    //TypeBuilder.DefineMethodOverride(mb, baseMethod);
    return new CodeGenerator(this, mb, mb.GetILGenerator());
  }

  public TypeGenerator DefineNestedType(string name, Type parent) { return DefineNestedType(0, name, parent); }
  public TypeGenerator DefineNestedType(TypeAttributes attrs, string name, Type parent)
  { if(nestedTypes==null) nestedTypes = new ArrayList();
    TypeAttributes ta = attrs | TypeAttributes.Class | TypeAttributes.NestedPublic;
    TypeGenerator ret = new TypeGenerator(Assembly, TypeBuilder.DefineNestedType(name, ta, parent));
    nestedTypes.Add(ret);
    return ret;
  }

  public Slot DefineStaticField(string name, Type type)
  { return new StaticSlot(TypeBuilder.DefineField(name, type, FieldAttributes.Public|FieldAttributes.Static));
  }

  public Type FinishType()
  { if(initGen!=null) initGen.ILG.Emit(OpCodes.Ret);
    Type ret = TypeBuilder.CreateType();
    if(nestedTypes!=null) foreach(TypeGenerator tg in nestedTypes) tg.FinishType();
    return ret;
  }

  public Slot GetConstant(object value)
  { Slot slot;
    bool hash = Convert.GetTypeCode(value)!=TypeCode.Object || !(value is List || value is Dict);

    if(hash) slot = (Slot)constants[value];
    else
    { if(constobjs==null) { constobjs = new ArrayList(); constslots = new ArrayList(); }
      else
      { int index = constobjs.IndexOf(value);
        if(index!=-1) return (Slot)constslots[index];
      }
      slot = null;
    }

    if(slot==null)
    { FieldBuilder fb = TypeBuilder.DefineField("c$"+constants.Count, typeof(object), FieldAttributes.Static);
      slot = new StaticSlot(fb);
      if(hash) constants[value] = slot;
      else { constobjs.Add(value); constslots.Add(slot); }
      EmitConstantInitializer(value);
      initGen.EmitFieldSet(fb);
    }
    return slot;
  }

  public CodeGenerator GetInitializer()
  { if(initGen==null)
    { ConstructorBuilder cb = TypeBuilder.DefineTypeInitializer();
      initGen = new CodeGenerator(this, cb, cb.GetILGenerator());
    }
    return initGen;
  }

  public AssemblyGenerator Assembly;
  public TypeBuilder TypeBuilder;

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
        cg.EmitConstant(o);
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
        cg.EmitConstant(e.Key);
        cg.EmitConstant(e.Value);
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
    else if(value is Complex)
    { Complex c = (Complex)value;
      cg.EmitDouble(c.real);
      cg.EmitDouble(c.imag);
      cg.EmitNew(typeof(Complex), new Type[] { typeof(double), typeof(double) });
      cg.ILG.Emit(OpCodes.Box, typeof(Complex));
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

  Type[] GetParamTypes(ParameterInfo[] pi)
  { Type[] paramTypes = new Type[pi.Length];
    for(int i=0; i<pi.Length; i++) paramTypes[i] = pi[i].ParameterType;
    return paramTypes;
  }

  HybridDictionary constants = new HybridDictionary();
  ArrayList nestedTypes, constobjs, constslots;
  CodeGenerator initGen;
  Slot moduleField;
}

} // namespace Boa.AST
