using System;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Notifier.Util
{
  public static class ConvertUtil
  {
    private static readonly char[] ourLowerDigits = new char[16] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f'};
    private static readonly char[] ourUpperDigits = new char[16] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F'};

    [NotNull]
    private static char[] ConvertToHexString([NotNull] byte[] buffer, [NotNull] char[] digits)
    {
      if (buffer == null) throw new ArgumentNullException(nameof(buffer));
      var res = new char[buffer.Length * 2];
      var index = 0;
      foreach (var b in buffer)
      {
        res[index++] = digits[b >> 4];
        res[index++] = digits[b & 0xF];
      }

      return res;
    }

    [NotNull]
    public static char[] ToLoverHexChars([NotNull] this byte[] buffer)
    {
      return ConvertToHexString(buffer, ourLowerDigits);
    }

    [NotNull]
    public static char[] ToUpperHexChars([NotNull] this byte[] buffer)
    {
      return ConvertToHexString(buffer, ourUpperDigits);
    }

    [NotNull]
    public static string ToLoverHexString([NotNull] this byte[] buffer)
    {
      return new(ToLoverHexChars(buffer));
    }

    [NotNull]
    public static string ToUpperHexString([NotNull] this byte[] buffer)
    {
      return new(ToUpperHexChars(buffer));
    }

    [NotNull]
    public static byte[] FromHexString([NotNull] this char[] chars)
    {
      if (chars == null) throw new ArgumentNullException(nameof(chars));
      if (chars.Length % 2 != 0) throw new ArgumentException("Invalid buffer length", nameof(chars));
      var size = chars.Length / 2;

      var res = new byte[size];
      var k = 0;
      var n = 0;
      while (n < size)
      {
        var b1 = ParseHexSymbol(chars[k++]);
        var b0 = ParseHexSymbol(chars[k++]);
        res[n++] = (byte) ((b1 << 4) | b0);
      }

      return res;
    }

    [NotNull]
    public static byte[] FromHexString([NotNull] this string str)
    {
      return str.ToCharArray().FromHexString();
    }

    private static byte ParseHexSymbol(char ch)
    {
      if ('0' <= ch && ch <= '9')
        return (byte) (ch - '0');
      if ('A' <= ch && ch <= 'F')
        return (byte) (ch - 'A' + 10);
      if ('a' <= ch && ch <= 'f')
        return (byte) (ch - 'a' + 10);
      throw new FormatException($"Failed to parse hex symbol '{ch}'");
    }
  }
}