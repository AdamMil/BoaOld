using System;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;

namespace Boa.AST
{

public class TypeGenerator
{ public TypeGenerator(AssemblyGenerator assembly, TypeBuilder typeBuilder)
  { Assembly=assembly; TypeBuilder=typeBuilder;
  }

  public Slot AddModuleSlot(Type moduleType)
  { if(ModuleSlot!=null) return ModuleSlot;
    return ModuleSlot = AddStaticSlot("__myModule", moduleType);
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
    switch(Convert.GetTypeCode(value))
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
  public Slot ModuleSlot;

  Hashtable constants = new Hashtable();
  CodeGenerator initGen;
}

} // namespace Boa.AST
