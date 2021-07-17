using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable InconsistentlySynchronizedField

namespace LDTTeam.Authentication.Modules.Api.Utils
{
    public class AsyncEvent<T>
        where T : class
    {
        private readonly object _subLock = new();
        private ImmutableArray<T> _subscriptions;

        public bool HasSubscribers => _subscriptions.Length != 0;
        public IReadOnlyList<T> Subscriptions => _subscriptions;

        public AsyncEvent()
        {
            _subscriptions = ImmutableArray.Create<T>();
        }

        public void Add(T subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            lock (_subLock)
                _subscriptions = _subscriptions.Add(subscriber);
        }

        public void Remove(T subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            lock (_subLock)
                _subscriptions = _subscriptions.Remove(subscriber);
        }
    }

    public static class EventExtensions
    {
        public static async Task InvokeAsync(this AsyncEvent<Func<Task>> eventHandler, bool concurrent = true)
        {
            IReadOnlyList<Func<Task>> subscribers = eventHandler.Subscriptions;
            if (concurrent)
            {
                IEnumerable<Task> tasks = subscribers.Select(x => x.Invoke());
                await Task.WhenAll(tasks);
                return;
            }

            foreach (Func<Task> t in subscribers)
                await t.Invoke();
        }

        public static async Task InvokeAsync<T>(this AsyncEvent<Func<T, Task>> eventHandler, T arg,
            bool concurrent = true)
        {
            IReadOnlyList<Func<T, Task>> subscribers = eventHandler.Subscriptions;
            if (concurrent)
            {
                IEnumerable<Task> tasks = subscribers.Select(x => x.Invoke(arg));
                await Task.WhenAll(tasks);
                return;
            }

            foreach (Func<T, Task> t in subscribers)
                await t.Invoke(arg);
        }

        public static async Task InvokeAsync<T1, T2>(this AsyncEvent<Func<T1, T2, Task>> eventHandler, T1 arg1, T2 arg2,
            bool concurrent = true)
        {
            IReadOnlyList<Func<T1, T2, Task>> subscribers = eventHandler.Subscriptions;
            if (concurrent)
            {
                IEnumerable<Task> tasks = subscribers.Select(x => x.Invoke(arg1, arg2));
                await Task.WhenAll(tasks);
                return;
            }
            foreach (Func<T1, T2, Task> t in subscribers)
                await t.Invoke(arg1, arg2);
        }

        public static async Task InvokeAsync<T1, T2, T3>(this AsyncEvent<Func<T1, T2, T3, Task>> eventHandler, T1 arg1,
            T2 arg2, T3 arg3, bool concurrent = true)
        {
            IReadOnlyList<Func<T1, T2, T3, Task>> subscribers = eventHandler.Subscriptions;
            if (concurrent)
            {
                IEnumerable<Task> tasks = subscribers.Select(x => x.Invoke(arg1, arg2, arg3));
                await Task.WhenAll(tasks);
                return;
            }
            foreach (Func<T1, T2, T3, Task> t in subscribers)
                await t.Invoke(arg1, arg2, arg3);
        }

        public static async Task InvokeAsync<T1, T2, T3, T4>(this AsyncEvent<Func<T1, T2, T3, T4, Task>> eventHandler,
            T1 arg1, T2 arg2, T3 arg3, T4 arg4, bool concurrent = true)
        {
            IReadOnlyList<Func<T1, T2, T3, T4, Task>> subscribers = eventHandler.Subscriptions;
            if (concurrent)
            {
                IEnumerable<Task> tasks = subscribers.Select(x => x.Invoke(arg1, arg2, arg3, arg4));
                await Task.WhenAll(tasks);
                return;
            }
            foreach (Func<T1, T2, T3, T4, Task> t in subscribers)
                await t.Invoke(arg1, arg2, arg3, arg4);
        }

        public static async Task InvokeAsync<T1, T2, T3, T4, T5>(
            this AsyncEvent<Func<T1, T2, T3, T4, T5, Task>> eventHandler, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5,
            bool concurrent = true)
        {
            IReadOnlyList<Func<T1, T2, T3, T4, T5, Task>> subscribers = eventHandler.Subscriptions;
            if (concurrent)
            {
                IEnumerable<Task> tasks = subscribers.Select(x => x.Invoke(arg1, arg2, arg3, arg4, arg5));
                await Task.WhenAll(tasks);
                return;
            }
            foreach (Func<T1, T2, T3, T4, T5, Task> t in subscribers)
                await t.Invoke(arg1, arg2, arg3, arg4, arg5);
        }
    }
}