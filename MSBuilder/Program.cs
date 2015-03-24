using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Framework;

namespace wtfbuild
{
  class mylogger : ILogger
  {
    public string Parameters
    {
      get
      {
        throw new NotImplementedException();
      }

      set
      {
        throw new NotImplementedException();
      }
    }

    public LoggerVerbosity Verbosity
    {
      get
      {
        throw new NotImplementedException();
      }

      set
      {
        throw new NotImplementedException();
      }
    }

    void handleErrorRaise(object sender, BuildErrorEventArgs e)
    {
      Console.WriteLine(e);
    }

    public void Initialize(IEventSource eventSource)
    {
      eventSource.ErrorRaised += new BuildErrorEventHandler(handleErrorRaise);
      throw new NotImplementedException();
    }

    public void Shutdown()
    {
      throw new NotImplementedException();
    }
  }
  class Program
  {
    static void Main(string[] args)
    {
      var p = new Project(@"C:\Users\carr27\Documents\GitHub\roslyn\BuildAndTest.proj");
      p.Build();
    }
  }
}

