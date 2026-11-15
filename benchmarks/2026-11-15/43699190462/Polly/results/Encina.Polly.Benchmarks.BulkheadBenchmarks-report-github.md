```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5,326.9 ns |    434.31 ns | 362.67 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5,057.8 ns |    225.79 ns | 188.55 ns |  0.95 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   449.0 ns |     87.80 ns |  82.13 ns |  0.08 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 5,352.6 ns |    434.16 ns | 406.12 ns |  1.01 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 8,141.3 ns |    208.84 ns | 174.39 ns |  1.53 |    0.10 |    5528 B |        7.05 |
|                                |            |                |             |             |            |              |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,555.7 ns | 10,235.27 ns | 561.03 ns |  1.01 |    0.12 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 5,915.7 ns | 10,030.97 ns | 549.83 ns |  1.07 |    0.13 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   411.0 ns |  1,740.34 ns |  95.39 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,594.7 ns | 10,919.20 ns | 598.52 ns |  1.01 |    0.13 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 8,690.8 ns |  8,710.96 ns | 477.48 ns |  1.58 |    0.16 |    5528 B |        7.05 |
