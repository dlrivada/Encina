```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                               | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev      | Ratio  | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|------------:|-------:|--------:|-------:|----------:|------------:|
| AcquireAndRelease_100Iterations      | Job-YFEFPZ | 10             | Default     | 129,119.8 ns |  1,101.42 ns |   728.52 ns | 140.32 |    1.14 | 0.4883 |   57600 B |       93.51 |
| IsLockedAsync_LockedResource         | Job-YFEFPZ | 10             | Default     |   1,480.4 ns |      9.33 ns |     4.88 ns |   1.61 |    0.01 | 0.0076 |     704 B |        1.14 |
| IsLockedAsync_UnlockedResource       | Job-YFEFPZ | 10             | Default     |     443.8 ns |      2.10 ns |     1.39 ns |   0.48 |    0.00 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | Job-YFEFPZ | 10             | Default     |  13,007.5 ns |     60.47 ns |    31.63 ns |  14.14 |    0.09 | 0.0610 |    5984 B |        9.71 |
| TryAcquireAsync_SingleLock           | Job-YFEFPZ | 10             | Default     |     920.2 ns |      8.82 ns |     5.84 ns |   1.00 |    0.01 | 0.0067 |     616 B |        1.00 |
|                                      |            |                |             |              |              |             |        |         |        |           |             |
| AcquireAndRelease_100Iterations      | ShortRun   | 3              | 1           | 128,248.6 ns | 21,691.27 ns | 1,188.97 ns | 141.38 |    1.15 | 0.4883 |   57600 B |       94.74 |
| IsLockedAsync_LockedResource         | ShortRun   | 3              | 1           |   1,480.7 ns |     80.29 ns |     4.40 ns |   1.63 |    0.00 | 0.0076 |     704 B |        1.16 |
| IsLockedAsync_UnlockedResource       | ShortRun   | 3              | 1           |     443.8 ns |     17.48 ns |     0.96 ns |   0.49 |    0.00 | 0.0024 |     232 B |        0.38 |
| ParallelAcquire_10DifferentResources | ShortRun   | 3              | 1           |  13,394.8 ns |    644.75 ns |    35.34 ns |  14.77 |    0.04 | 0.0610 |    5984 B |        9.84 |
| TryAcquireAsync_SingleLock           | ShortRun   | 3              | 1           |     907.1 ns |     25.94 ns |     1.42 ns |   1.00 |    0.00 | 0.0067 |     608 B |        1.00 |
