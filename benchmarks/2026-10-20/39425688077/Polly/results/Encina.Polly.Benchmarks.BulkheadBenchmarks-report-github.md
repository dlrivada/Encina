```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,126.6 ns |     352.8 ns |   294.60 ns |  6,038.5 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,005.4 ns |     373.0 ns |   311.47 ns |  5,942.0 ns |  0.98 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    246.1 ns |     101.7 ns |    90.20 ns |    245.0 ns |  0.04 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,408.6 ns |     587.5 ns |   520.81 ns |  7,502.5 ns |  1.21 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 15,219.9 ns |     546.2 ns |   484.22 ns | 14,985.0 ns |  2.49 |    0.14 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,703.7 ns |  19,826.4 ns | 1,086.75 ns |  7,858.0 ns |  1.01 |    0.18 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,901.0 ns |  12,756.3 ns |   699.21 ns |  7,602.0 ns |  1.04 |    0.15 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |  1,338.2 ns |  34,164.9 ns | 1,872.69 ns |    271.5 ns |  0.18 |    0.22 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 11,805.8 ns | 130,418.3 ns | 7,148.67 ns |  8,171.5 ns |  1.55 |    0.84 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,595.0 ns |  23,602.4 ns | 1,293.73 ns | 11,403.0 ns |  1.53 |    0.24 |    5528 B |        7.05 |
