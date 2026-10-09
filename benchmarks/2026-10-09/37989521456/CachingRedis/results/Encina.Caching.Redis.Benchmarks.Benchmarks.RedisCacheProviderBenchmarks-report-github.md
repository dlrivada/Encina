```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.86GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-ZDPOZY : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | InvocationCount | IterationCount | LaunchCount | UnrollFactor | WarmupCount | Mean | Error | Ratio | RatioSD | Alloc Ratio |
|------------------------------ |----------- |---------------- |--------------- |------------ |------------- |------------ |-----:|------:|------:|--------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   NA |    NA |     ? |       ? |           ? |
| ExistsAsync_True              | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   NA |    NA |     ? |       ? |           ? |
| GetAsync_CacheHit             | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   NA |    NA |     ? |       ? |           ? |
| GetAsync_CacheMiss            | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   NA |    NA |     ? |       ? |           ? |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   NA |    NA |     ? |       ? |           ? |
| SetAsync                      | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   NA |    NA |     ? |       ? |           ? |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | Default         | 20             | Default     | 16           | 5           |   NA |    NA |     ? |       ? |           ? |
|                               |            |                 |                |             |              |             |      |       |       |         |             |
| ExistsAsync_False             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| ExistsAsync_True              | ShortRun   | Default         | 3              | 1           | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| GetAsync_CacheHit             | ShortRun   | Default         | 3              | 1           | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| GetAsync_CacheMiss            | ShortRun   | Default         | 3              | 1           | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| GetOrSetAsync_CacheHit        | ShortRun   | Default         | 3              | 1           | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| SetAsync                      | ShortRun   | Default         | 3              | 1           | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
| SetWithSlidingExpirationAsync | ShortRun   | Default         | 3              | 1           | 16           | 3           |   NA |    NA |     ? |       ? |           ? |
|                               |            |                 |                |             |              |             |      |       |       |         |             |
| GetOrSetAsync_CacheMiss       | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   NA |    NA |     ? |       ? |           ? |
| RemoveAsync                   | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   NA |    NA |     ? |       ? |           ? |
| RemoveByPatternAsync          | Job-ZDPOZY | 1               | 20             | Default     | 1            | 5           |   NA |    NA |     ? |       ? |           ? |
|                               |            |                 |                |             |              |             |      |       |       |         |             |
| GetOrSetAsync_CacheMiss       | ShortRun   | 1               | 3              | 1           | 1            | 3           |   NA |    NA |     ? |       ? |           ? |
| RemoveAsync                   | ShortRun   | 1               | 3              | 1           | 1            | 3           |   NA |    NA |     ? |       ? |           ? |
| RemoveByPatternAsync          | ShortRun   | 1               | 3              | 1           | 1            | 3           |   NA |    NA |     ? |       ? |           ? |

Benchmarks with issues:
  RedisCacheProviderBenchmarks.ExistsAsync_False: Job-NUBXJZ(IterationCount=20, WarmupCount=5)
  RedisCacheProviderBenchmarks.ExistsAsync_True: Job-NUBXJZ(IterationCount=20, WarmupCount=5)
  RedisCacheProviderBenchmarks.GetAsync_CacheHit: Job-NUBXJZ(IterationCount=20, WarmupCount=5)
  RedisCacheProviderBenchmarks.GetAsync_CacheMiss: Job-NUBXJZ(IterationCount=20, WarmupCount=5)
  RedisCacheProviderBenchmarks.GetOrSetAsync_CacheHit: Job-NUBXJZ(IterationCount=20, WarmupCount=5)
  RedisCacheProviderBenchmarks.SetAsync: Job-NUBXJZ(IterationCount=20, WarmupCount=5)
  RedisCacheProviderBenchmarks.SetWithSlidingExpirationAsync: Job-NUBXJZ(IterationCount=20, WarmupCount=5)
  RedisCacheProviderBenchmarks.ExistsAsync_False: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.ExistsAsync_True: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.GetAsync_CacheHit: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.GetAsync_CacheMiss: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.GetOrSetAsync_CacheHit: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.SetAsync: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.SetWithSlidingExpirationAsync: ShortRun(IterationCount=3, LaunchCount=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.GetOrSetAsync_CacheMiss: Job-ZDPOZY(InvocationCount=1, IterationCount=20, UnrollFactor=1, WarmupCount=5)
  RedisCacheProviderBenchmarks.RemoveAsync: Job-ZDPOZY(InvocationCount=1, IterationCount=20, UnrollFactor=1, WarmupCount=5)
  RedisCacheProviderBenchmarks.RemoveByPatternAsync: Job-ZDPOZY(InvocationCount=1, IterationCount=20, UnrollFactor=1, WarmupCount=5)
  RedisCacheProviderBenchmarks.GetOrSetAsync_CacheMiss: ShortRun(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.RemoveAsync: ShortRun(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=3)
  RedisCacheProviderBenchmarks.RemoveByPatternAsync: ShortRun(InvocationCount=1, IterationCount=3, LaunchCount=1, UnrollFactor=1, WarmupCount=3)
