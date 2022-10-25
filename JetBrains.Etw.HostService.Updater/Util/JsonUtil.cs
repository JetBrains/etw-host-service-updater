using System;
using System.Collections.Generic;
using System.Text.Json;
using JetBrains.Annotations;

namespace JetBrains.Etw.HostService.Updater.Util
{
  internal static class JsonUtil
  {
    private static TResult GetPropertyEx<TResult>(this JsonElement element, [NotNull] string propertyName, [NotNull] Func<JsonElement, TResult> func)
    {
      if (func == null)
        throw new ArgumentNullException(nameof(func));
      if (!element.TryGetProperty(propertyName, out var childElement))
        throw new KeyNotFoundException($"Failed to find string property {propertyName}");
      try
      {
        return func(childElement);
      }
      catch (FormatException e)
      {
        throw new FormatException($"Failed to parse property {propertyName}", e);
      }
      catch (InvalidOperationException e)
      {
        throw new InvalidOperationException($"Failed to get property {propertyName}", e);
      }
    }

    private static TResult? TryGetPropertyEx<TResult>(this JsonElement element, [NotNull] string propertyName, [NotNull] Func<JsonElement, TResult> func) where TResult : struct
    {
      if (func == null)
        throw new ArgumentNullException(nameof(func));
      if (!element.TryGetProperty(propertyName, out var childElement))
        return null;
      try
      {
        return func(childElement);
      }
      catch (FormatException e)
      {
        throw new FormatException($"Failed to parse property {propertyName}", e);
      }
      catch (InvalidOperationException e)
      {
        throw new InvalidOperationException($"Failed to get property {propertyName}", e);
      }
    }

    public static JsonElement? TryGetPropertyEx(this JsonElement element, [NotNull] string propertyName)
    {
      if (!element.TryGetProperty(propertyName, out var childElement))
        return null;
      return childElement;
    }

    public static JsonElement GetPropertyEx(this JsonElement element, [NotNull] string propertyName)
    {
      if (!element.TryGetProperty(propertyName, out var childElement))
        throw new KeyNotFoundException($"Failed to find property {propertyName}");
      return childElement;
    }

    [NotNull]
    public static string GetStringPropertyEx(this JsonElement element, [NotNull] string propertyName) => element.GetPropertyEx(propertyName, x => x.GetString()) ?? "";

    public static long GetInt64PropertyEx(this JsonElement element, [NotNull] string propertyName) => element.GetPropertyEx(propertyName, x => x.GetInt64());

    [NotNull]
    public static Uri GetAbsoluteUriPropertyEx(this JsonElement element, [NotNull] string propertyName) => element.GetPropertyEx(propertyName, x =>
      {
        var str = x.GetString() ?? "";
        if (!Uri.TryCreate(str, UriKind.Absolute, out var res))
          throw new FormatException($"Failed to parse the absolute URI value {str}");
        return res;
      });

    [NotNull]
    public static Version GetVersionPropertyEx(this JsonElement element, [NotNull] string propertyName) => element.GetPropertyEx(propertyName, x =>
      {
        var str = x.GetString() ?? "";
        if (!Version.TryParse(str!, out var res))
          throw new FormatException($"Failed to parse the version value {str}");
        return res;
      });

    public static bool? TryGetBooleanPropertyEx(this JsonElement element, [NotNull] string propertyName) => element.TryGetPropertyEx(propertyName, x => x.GetBoolean());
  }
}