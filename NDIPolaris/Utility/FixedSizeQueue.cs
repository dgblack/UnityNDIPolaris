using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;

public class FixedSizedQueue<T>
{
    public ConcurrentQueue<T> q = new ConcurrentQueue<T>();

    public int Limit { get; set; }
    public void Enqueue(T obj)
    {
        q.Enqueue(obj);
        T overflow;
        while (q.Count > Limit && q.TryDequeue(out overflow)) ;
    }
}
