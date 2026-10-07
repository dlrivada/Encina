```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,661.5 ns |     595.04 ns |   527.49 ns |  6,770.0 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,905.6 ns |     304.47 ns |   269.91 ns |  6,850.5 ns |  1.04 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    332.5 ns |      95.39 ns |    89.22 ns |    310.0 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,634.6 ns |     308.01 ns |   273.04 ns |  6,600.5 ns |  1.00 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,724.0 ns |     145.25 ns |   121.29 ns | 10,685.0 ns |  1.62 |    0.13 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 12,284.3 ns | 161,017.46 ns | 8,825.91 ns |  7,390.0 ns |  1.32 |    1.06 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,656.2 ns |  13,367.09 ns |   732.70 ns |  7,937.5 ns |  0.82 |    0.37 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    540.5 ns |   2,371.68 ns |   130.00 ns |    470.5 ns |  0.06 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,164.3 ns |  15,392.30 ns |   843.70 ns |  6,770.0 ns |  0.77 |    0.35 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 14,170.0 ns |  16,889.67 ns |   925.78 ns | 13,676.0 ns |  1.53 |    0.68 |    5528 B |        7.05 |
