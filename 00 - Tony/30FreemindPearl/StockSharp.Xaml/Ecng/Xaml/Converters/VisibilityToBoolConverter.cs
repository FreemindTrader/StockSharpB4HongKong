﻿// Decompiled with JetBrains decompiler
// Type: Ecng.Xaml.Converters.VisibilityToBoolConverter
// Assembly: StockSharp.Xaml, Version=5.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 333C37E3-F521-4513-E734-AF062F7EDC8B
// Assembly location: T:\00-FreemindTrader\packages\stocksharp.xaml\5.0.135\lib\net6.0-windows7.0\StockSharp.Xaml.dll

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Ecng.Xaml.Converters
{
  /// <summary>
  /// <see cref="T:System.Windows.Visibility" /> to <see cref="T:System.Boolean" /> converter.
  ///     </summary>
  public class VisibilityToBoolConverter : IValueConverter
  {
    object IValueConverter.Convert(
      object _param1,
      Type _param2,
      object _param3,
      CultureInfo _param4)
    {
      return (object) ((Visibility) _param1 == Visibility.Visible);
    }

    object IValueConverter.ConvertBack(
      object _param1,
      Type _param2,
      object _param3,
      CultureInfo _param4)
    {
      return (object) (Visibility) ((bool) _param1 ? 0 : 2);
    }
  }
}
