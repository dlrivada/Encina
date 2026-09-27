```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|------------:|------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 86,739.8 ns |  1,172.95 ns |   775.84 ns | 94.92 |    1.43 | 3.4180 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |  1,056.9 ns |     16.97 ns |    10.10 ns |  1.16 |    0.02 | 0.0420 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |    142.0 ns |     13.85 ns |     9.16 ns |  0.16 |    0.01 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  9,501.9 ns |    137.20 ns |    90.75 ns | 10.40 |    0.16 | 0.3510 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |    914.0 ns |     20.53 ns |    12.22 ns |  1.00 |    0.02 | 0.0362 |     616 B |        1.00 |
|                                      |            |                |             |             |              |             |       |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 87,711.1 ns | 70,925.60 ns | 3,887.67 ns | 95.74 |    3.68 | 3.4180 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |  1,053.6 ns |    202.15 ns |    11.08 ns |  1.15 |    0.01 | 0.0420 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |    133.3 ns |     22.61 ns |     1.24 ns |  0.15 |    0.00 | 0.0138 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  9,522.7 ns |    911.19 ns |    49.95 ns | 10.39 |    0.06 | 0.3510 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |    916.2 ns |     53.50 ns |     2.93 ns |  1.00 |    0.00 | 0.0362 |     608 B |        1.00 |
