#define CONTRACTS_FULL

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication8
{

  public class ScottRepro
  {
    public static object PingMethodPost(dynamic state, dynamic[] args)
    {
      /*            Contract.Requires(args != null);
                  Contract.Requires(0 < args.Length);
                  Contract.Ensures(Contract.Result<System.Object>() == null);
                  Contract.Ensures(ActorStressTests.PingPongActor.< PingMethodPost > o__SiteContainer0.<> p__Site1.Target != null);
                  Contract.Ensures(ActorStressTests.PingPongActor.< PingMethodPost > o__SiteContainer0.<> p__Site2.Target != null);*/

      int remainingCount = args[0];
      state.Publish("Status", remainingCount - 1);
      if (remainingCount > 0)
      {
        string otherActorName = state.otherActorName;
        dynamic proxy = state.GetActorProxy(otherActorName);
        proxy.Command("PingMethodPost", new object[] { remainingCount - 1 });
      }
      return null;
    }
  }
#if true

  public class SomeOtherClass
  {
    [ContractVerification(false)]
    public Foo.MyEnum ReturnAnEnumValue()
    {
      return Foo.MyEnum.Three; // dummy
    }
  }

  public static class Helper
  {
    [ContractVerification(false)]
    static public Exception FailedAssertion()
    {
      Contract.Requires(false, "Randy wants you to handle all the enums ;-)");
      throw new Exception();
    }
  }
  public class Foo
  {
    public enum MyEnum { One, Two, Three };

    public int Switch(MyEnum e)
    {
      
      var i = 0;

      switch (e)
      {
        case MyEnum.One:
          i += 1;
          break;

        case MyEnum.Two:
          i += 2;
          break;

        default:
          throw Helper.FailedAssertion();
      }

      return i;
    }
    public int SwitchWithAssertFalse(MyEnum e)
    {
      var i = 0;

      switch (e)
      {
        case MyEnum.One:
          i += 1;
          break;

        case MyEnum.Two:
          i += 2;
          break;

        default:
          Contract.Assert(false);
          throw new Exception();
      }

      return i;
    }
    public int Switch_ViaMethodReturnValue(SomeOtherClass so)
    {
      Contract.Requires(so != null);
      var i = 0;

      var e = so.ReturnAnEnumValue();

      switch (e)
      {
        case MyEnum.One:
          i += 1;
          break;

        case MyEnum.Two:
          i += 2;
          break;

        default:
          throw Helper.FailedAssertion();
      }

      return i;
    }

    public enum Days { Sun = 0, Mon = 1, Tue = 2, Wed = 3, Thu = 4, Fri = 5, Sat = 6 }
    public string Translate(Days d)
    {
      switch (d)
      {
        case Days.Sun: // 0
        case Days.Tue: // 2
        case Days.Thu: // 4
        case Days.Fri: // 5
          return "Even";

        case Days.Mon: // 1
        case Days.Sat: // 6
          return "Odd";

        default:
          Contract.Assert(false);     // we forgot one case!
          return null;
      }
    }
  }
  
#endif

  public interface MyInterface
  {
    string InterfaceMethod();

    string InterfaceMethod(string s);

    int Count { get;  }
  }
  
  public class ImplementMyInterface : MyInterface
  {
    public ImplementMyInterface()
    { }

    public string InterfaceMethod()
    {
      return "A non null hello world!!!";
    }

    public string InterfaceMethod(string s)
    {
      return s;
    }

    public int Count { get { return 12; } }
  }

  public abstract class AbstractClass
  {
    public abstract int[] AbstractMethod();
  }

  public class ImplementAbstractClass : AbstractClass
  {

    public override int[] AbstractMethod()
    {
      return new int[1234];
    }
  }

  class Program
  {

#if true
        static void TestMultidimensionalArrays0(int[,] a, int i, int j)
    {
      Contract.Requires(a != null);

      a[i, j] = 0;  // no error for index out of bounds
    }

    public void Foo(string s, Func<string> next)
    {
      var error = true;
      while(s != null && error)
      {
        Contract.Assert(s != null);

        if (System.Environment.TickCount % 2 == 0)
          error = false;

        s = next();
      }
    }
#endif

    static void Main(string[] args)
    {
      var x = new ImplementMyInterface();
      var z = x.InterfaceMethod();
      var len = z.Length;
      Console.WriteLine(len);
    }

    public void TestNonNullViaImplementation()
    {
      var x = new ImplementMyInterface();
      var z = x.InterfaceMethod();
      var len = z.Length;      
    }

    public void TestNotNullViaInterface(MyInterface m)
    {
      var tmp = m.InterfaceMethod();
      Contract.Assert(tmp != null);
    }

    public void TestNotNullViaInterface2(MyInterface m)
    {
      var tmp = m.InterfaceMethod("ciao!");
      Contract.Assert(tmp != null);
    }

    public void TestGEQ_ZeroViaInterface(MyInterface i)
    {
      var value = i.Count;
      Contract.Assert(value >= 0);
    }

    // not working 
    public void TestGEQ_ZeroViaInstance()
    {
      var tmp = new ImplementMyInterface();
      var value = tmp.Count;
      Contract.Assert(value >= 0);
    }

    // not working 
    public void TestGEQ_ZeroViaInstance_NoName()
    {
      var tmp = new ImplementMyInterface();
      Contract.Assert(tmp.Count>= 0);
    }

    public int TestViaInstance()
    {
      var myInstance = new ImplementAbstractClass();
      var x = myInstance.AbstractMethod();

      return x.Length;
    }

    public int TestViaAbstractClass(AbstractClass abs)
    {
      Contract.Requires(abs != null);

      var x = abs.AbstractMethod();

      return x.Length;
    }

    public int TestNegative(ImplementMyInterface i1, ImplementMyInterface i2)
    {
      Contract.Requires(i1 != null);
      Contract.Requires(i2 != null);

      var tmp = i2.InterfaceMethod();
      if(i1.Count > 0)
      {
        Contract.Assert(tmp != null);
      }

      return 0;
    }

  }
}
