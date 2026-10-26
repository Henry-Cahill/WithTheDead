using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Debug = UnityEngine.Debug;

namespace CloneZ
{
    /// <summary>
    /// A small job system for audio rendering: work runs on a couple of low-priority background threads and its result
    /// is handed back on the main thread by <see cref="Pump"/> (clips can only be made there). Urgent jobs jump the
    /// queue. Engine-free apart from logging, so the offline sound tools can run it too.
    /// </summary>
    public static class AudioJobs
    {
        /// <summary>Base type for audio rendering jobs.</summary>
        private abstract class Job
        {
            public int Generation;
            public abstract void Run();
            public abstract void Complete();
        }

        /// <summary>Concrete generic job that stores work and completion delegates.</summary>
        private sealed class Job<T> : Job
        {
            public Func<T> Work;
            public Action<T> Done;
            private T _result;
            private Exception _error;

            public override void Run()
            {
                try { _result = Work(); }
                catch (Exception e) { _error = e; }
            }

            public override void Complete()
            {
                if (_error != null) { Debug.LogException(_error); return; }
                try { Done?.Invoke(_result); }
                catch (Exception e) { Debug.LogException(e); }
            }
        }

        // One set of worker threads; a shut-down crew keeps its own stop flag, so a restarted crew never revives it.
        private sealed class Crew
        {
            public volatile bool Stop;
            public Thread[] Threads;
        }

        private static readonly ConcurrentQueue<Job> s_Urgent = new ConcurrentQueue<Job>();
        private static readonly ConcurrentQueue<Job> s_Normal = new ConcurrentQueue<Job>();
        private static readonly ConcurrentQueue<Job> s_Finished = new ConcurrentQueue<Job>();
        private static readonly SemaphoreSlim s_Signal = new SemaphoreSlim(0);
        private static readonly object s_Lock = new object();
        private static volatile Crew s_Crew;
        private static bool s_Inline;
        private static volatile int s_Generation;
        private static int s_Pending;

        /// <summary>Jobs queued or running that have not been completed on the main thread yet.</summary>
        public static int Pending => Volatile.Read(ref s_Pending);

        /// <summary>
        /// Runs <paramref name="work"/> in the background and <paramref name="done"/> with its result on the main thread
        /// (inside <see cref="Pump"/>). Without threads the work runs inside <see cref="Pump"/> too.
        /// </summary>
        public static void Run<T>(Func<T> work, Action<T> done, bool urgent = false)
        {
            if (work == null) return;
            var job = new Job<T> { Work = work, Done = done, Generation = s_Generation };
            Interlocked.Increment(ref s_Pending);
            (urgent ? s_Urgent : s_Normal).Enqueue(job);
            EnsureThreads();
            if (s_Crew != null) s_Signal.Release();
        }

        /// <summary>Main thread: completes finished jobs (and, with no worker threads, runs queued ones) for about <paramref name="budgetMs"/>.</summary>
        public static void Pump(float budgetMs)
        {
            long budget = (long)(Math.Max(0.1f, budgetMs) * Stopwatch.Frequency / 1000f);
            long start = Stopwatch.GetTimestamp();
            if (s_Inline)
            {
                while (Stopwatch.GetTimestamp() - start < budget && Take(out Job inline))
                {
                    inline.Run();
                    s_Finished.Enqueue(inline);
                }
            }
            while (s_Finished.TryDequeue(out Job job))
            {
                Interlocked.Decrement(ref s_Pending);
                if (job.Generation == s_Generation) job.Complete();
                if (Stopwatch.GetTimestamp() - start >= budget) break;
            }
        }

        /// <summary>Forgets every queued job (a new play session in the editor must not finish the last one's work).</summary>
        public static void Reset()
        {
            s_Generation++;
            while (s_Urgent.TryDequeue(out _)) Interlocked.Decrement(ref s_Pending);
            while (s_Normal.TryDequeue(out _)) Interlocked.Decrement(ref s_Pending);
        }

        /// <summary>Stops the worker threads (they finish the job in hand); <see cref="Run{T}"/> starts them again.</summary>
        public static void Shutdown()
        {
            lock (s_Lock)
            {
                Crew crew = s_Crew;
                if (crew == null) return;
                s_Crew = null;
                crew.Stop = true;
                s_Signal.Release(crew.Threads.Length);
            }
        }

        /// <summary>Ensures worker threads exist; starts them if needed.</summary>
        private static void EnsureThreads()
        {
            if (s_Crew != null || s_Inline) return;
            lock (s_Lock)
            {
                if (s_Crew != null || s_Inline) return;
                int n = Math.Max(1, Math.Min(3, Environment.ProcessorCount - 2));
                var crew = new Crew { Threads = new Thread[n] };
                try
                {
                    for (int i = 0; i < n; i++)
                    {
                        crew.Threads[i] = new Thread(() => Worker(crew)) { IsBackground = true, Name = "AudioJobs " + i, Priority = ThreadPriority.BelowNormal };
                        crew.Threads[i].Start();
                    }
                    s_Crew = crew;
                }
                catch (Exception e)
                {
                    // No threads on this platform: Pump runs the work inline instead.
                    Debug.LogWarning("[CloneZ] Audio rendering falls back to the main thread: " + e.Message);
                    crew.Stop = true;
                    s_Inline = true;
                }
            }
        }

        private static bool Take(out Job job) => s_Urgent.TryDequeue(out job) || s_Normal.TryDequeue(out job);

        /// <summary>Background worker loop that executes jobs and enqueues their results.</summary>
        private static void Worker(Crew crew)
        {
            while (!crew.Stop)
            {
                if (Take(out Job job))
                {
                    job.Run();
                    s_Finished.Enqueue(job);
                }
                else s_Signal.Wait(250);
            }
        }
    }
}
