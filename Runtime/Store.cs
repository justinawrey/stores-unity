using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stores
{
  public abstract class StoreSO : ScriptableObject
  {
    private List<Action> _unsubs = new List<Action>();

    private void Subscribe(Action unsub)
    {
      _unsubs.Add(unsub);
    }

    public void OnEnable()
    {
      ResetValues();
      Unsubscribe();
      SubscribeToComputed(Subscribe);
      SubscribeToWatchers(Subscribe);
    }

    private void Unsubscribe()
    {
      _unsubs.ForEach(unsub => unsub());
      _unsubs.Clear();
    }

    public virtual void SubscribeToComputed(Action<Action> subscribe) { }
    public virtual void SubscribeToWatchers(Action<Action> subscribe) { }
    public virtual void ResetValues() { }
    public virtual void SetToDefaultValues() { }
  }
}