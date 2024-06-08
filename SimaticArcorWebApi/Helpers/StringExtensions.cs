using System;
using System.Collections.Generic;
using System.Text;

namespace SimaticArcorWebApi.Helpers
{
  public static class StringExtensions
  {
    public static bool IsNA(this string s)
    {
      return s.ToLower() == "n/a";
    }

  }
}
