```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  7,023.9 ns |    172.52 ns |   152.94 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  7,372.5 ns |    616.30 ns |   576.49 ns |  1.05 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    544.5 ns |     31.41 ns |    29.38 ns |  0.08 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,773.2 ns |    137.42 ns |   121.82 ns |  0.96 |    0.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,767.9 ns |    319.02 ns |   249.07 ns |  1.53 |    0.05 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,468.3 ns | 34,181.48 ns | 1,873.60 ns |  1.03 |    0.27 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  8,267.2 ns |  8,849.89 ns |   485.09 ns |  1.01 |    0.18 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    607.3 ns |  1,640.95 ns |    89.95 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,161.5 ns |  5,619.75 ns |   308.04 ns |  0.87 |    0.15 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,249.5 ns |  7,524.45 ns |   412.44 ns |  1.37 |    0.24 |    5528 B |        7.05 |
