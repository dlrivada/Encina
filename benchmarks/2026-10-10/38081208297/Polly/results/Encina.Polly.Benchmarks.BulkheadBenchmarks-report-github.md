```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev       | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  8,188.8 ns |     408.3 ns |    318.75 ns |  8,185.5 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,136.5 ns |     321.8 ns |    268.76 ns |  7,068.0 ns |  0.87 |    0.05 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    572.5 ns |     120.3 ns |    106.61 ns |    542.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,873.5 ns |     893.0 ns |    697.22 ns |  8,139.5 ns |  0.96 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,605.9 ns |     213.8 ns |    166.93 ns | 10,619.5 ns |  1.30 |    0.05 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,463.3 ns |   7,284.0 ns |    399.26 ns |  7,603.0 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 20,221.2 ns | 385,087.0 ns | 21,107.92 ns |  8,225.5 ns |  2.71 |    2.46 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    534.3 ns |   1,530.0 ns |     83.86 ns |    491.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,519.2 ns |  14,531.5 ns |    796.52 ns |  7,338.5 ns |  1.01 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 21,950.3 ns | 168,191.4 ns |  9,219.14 ns | 17,683.0 ns |  2.95 |    1.08 |    5528 B |        7.05 |
