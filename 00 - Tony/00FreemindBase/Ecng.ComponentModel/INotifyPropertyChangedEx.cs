﻿// Decompiled with JetBrains decompiler
// Type: Ecng.ComponentModel.INotifyPropertyChangedEx
// Assembly: Ecng.ComponentModel, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// MVID: 261D7AEC-F6F7-407C-AFF2-E6AD402BE28A
// Assembly location: T:\00-FreemindTrader\packages\ecng.componentmodel\1.0.143\lib\netstandard2.0\Ecng.ComponentModel.dll

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ecng.ComponentModel
{
  public interface INotifyPropertyChangedEx : INotifyPropertyChanged
  {
    void NotifyPropertyChanged([CallerMemberName] string propertyName = null);
  }
}
