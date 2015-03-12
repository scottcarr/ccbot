using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InterfaceTest
{
  interface Shape
  {
    float getArea();
    string getName();

  }


  class Program
  {
    static void Main(string[] args)
    {
    }
    static int foo(Shape s)
    {
        return s.getName().Count();
    }
  }
}
