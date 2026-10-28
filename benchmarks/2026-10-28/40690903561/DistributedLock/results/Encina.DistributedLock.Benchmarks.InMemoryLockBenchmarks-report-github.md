```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|------------:|----------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 153,484.5 ns |   601.18 ns | 357.75 ns | 103.93 |    0.31 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,946.7 ns |    13.98 ns |   9.24 ns |   1.32 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     348.7 ns |     2.84 ns |   1.88 ns |   0.24 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  16,613.5 ns |    21.44 ns |  11.22 ns |  11.25 |    0.02 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |   1,476.8 ns |     5.22 ns |   3.10 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
|                                      |            |                |             |              |             |           |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 152,765.8 ns | 9,561.18 ns | 524.08 ns | 103.61 |    0.34 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,951.1 ns |    86.13 ns |   4.72 ns |   1.32 |    0.00 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     376.7 ns |     9.60 ns |   0.53 ns |   0.26 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  16,530.7 ns | 1,446.63 ns |  79.29 ns |  11.21 |    0.05 | 0.3357 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |   1,474.4 ns |    43.73 ns |   2.40 ns |   1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
