using System;
using System.Linq;
using JetBrains.Etw.HostService.Updater.Util;
using NUnit.Framework;

namespace JetBrains.Etw.HostService.Updater.Tests
{
  [TestFixture]
  public class ConvertUtilTests
  {
    private static readonly byte[] ourBytes0 = Array.Empty<byte>();
    private static readonly byte[] ourBytes1 = {0xc6};
    private static readonly byte[] ourBytes2 = {0xAF, 0x7D, 0x4e};
    private static readonly byte[] ourBytes3 = {0x1d, 0x54, 0x1a, 0xd6, 0x9b, 0x4d, 0x4d, 0x59, 0x9a, 0x1c, 0xbd, 0x46, 0xa4, 0x5a, 0x6c, 0xe2, 0x2e, 0x87, 0x27, 0x4a, 0xc0, 0xad, 0xd1, 0x97, 0xff, 0x2c, 0x0e, 0x8b, 0xf4, 0x54, 0x13, 0xc9};

    [Test]
    public void LowerTest()
    {
      static void Check(byte[] origBytes, string origStr)
      {
        var str = origBytes.ToLoverHexString();
        Assert.AreEqual(origStr, str);
        var res = str.FromHexString();
        Assert.IsTrue(origBytes.SequenceEqual(res));
      }

      Check(ourBytes0, "");
      Check(ourBytes1, "c6");
      Check(ourBytes2, "af7d4e");
      Check(ourBytes3, "1d541ad69b4d4d599a1cbd46a45a6ce22e87274ac0add197ff2c0e8bf45413c9");
    }

    [Test]
    public void UpperTest()
    {
      static void Check(byte[] origBytes, string origStr)
      {
        var str = origBytes.ToUpperHexString();
        Assert.AreEqual(origStr, str);
        var res = str.FromHexString();
        Assert.IsTrue(origBytes.SequenceEqual(res));
      }

      Check(ourBytes0, "");
      Check(ourBytes1, "C6");
      Check(ourBytes2, "AF7D4E");
      Check(ourBytes3, "1D541AD69B4D4D599A1CBD46A45A6CE22E87274AC0ADD197FF2C0E8BF45413C9");
    }
  }
}