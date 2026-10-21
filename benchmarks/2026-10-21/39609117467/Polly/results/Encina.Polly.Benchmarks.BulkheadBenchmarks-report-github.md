```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error         | StdDev      | Median     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|--------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,642.3 ns |     428.23 ns |   400.57 ns | 5,567.5 ns |  1.00 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,370.6 ns |     226.42 ns |   189.07 ns | 5,308.0 ns |  0.96 |    0.07 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    394.0 ns |      47.69 ns |    42.27 ns |   385.5 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,538.6 ns |     345.29 ns |   322.98 ns | 5,397.5 ns |  0.99 |    0.09 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,441.6 ns |     460.67 ns |   430.91 ns | 8,283.0 ns |  1.50 |    0.12 |    5528 B |        7.05 |
|                                |            |                |             |             |             |               |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  5,905.7 ns |  12,449.58 ns |   682.40 ns | 5,579.0 ns |  1.01 |    0.14 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  6,287.3 ns |   6,361.66 ns |   348.70 ns | 6,414.0 ns |  1.07 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    417.7 ns |     526.65 ns |    28.87 ns |   401.0 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 11,372.7 ns | 138,099.77 ns | 7,569.71 ns | 8,408.0 ns |  1.94 |    1.14 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           |  8,987.3 ns |   6,079.11 ns |   333.22 ns | 8,934.0 ns |  1.53 |    0.15 |    5528 B |        7.05 |
