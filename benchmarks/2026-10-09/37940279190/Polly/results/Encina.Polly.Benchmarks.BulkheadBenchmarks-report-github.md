```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Median      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  4,502.0 ns |    538.61 ns |   449.77 ns |  4,344.0 ns |  1.01 |    0.13 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,212.2 ns |    564.78 ns |   471.61 ns |  5,981.0 ns |  1.39 |    0.16 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    245.3 ns |     47.79 ns |    42.36 ns |    238.5 ns |  0.05 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  7,438.1 ns |  2,068.20 ns | 1,833.41 ns |  6,460.0 ns |  1.67 |    0.43 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 10,042.4 ns |  2,974.72 ns | 2,637.01 ns |  8,616.0 ns |  2.25 |    0.61 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  8,441.0 ns | 54,492.85 ns | 2,986.94 ns |  8,382.0 ns |  1.09 |    0.50 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,969.3 ns | 58,404.12 ns | 3,201.33 ns |  6,885.0 ns |  1.03 |    0.50 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    469.8 ns |  4,654.16 ns |   255.11 ns |    352.5 ns |  0.06 |    0.04 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,860.8 ns | 55,714.40 ns | 3,053.89 ns |  6,839.5 ns |  1.02 |    0.49 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 12,359.0 ns | 61,591.06 ns | 3,376.01 ns | 10,845.0 ns |  1.60 |    0.65 |    5528 B |        7.05 |
