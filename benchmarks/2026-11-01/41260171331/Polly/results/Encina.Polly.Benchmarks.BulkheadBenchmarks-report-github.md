```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|---------:|---------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  7,159.9 ns | 273.7 ns | 256.1 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  7,933.3 ns | 227.9 ns | 213.2 ns |  1.11 |    0.05 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    785.7 ns | 168.3 ns | 157.4 ns |  0.11 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  6,927.9 ns | 154.2 ns | 120.4 ns |  0.97 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,999.4 ns | 337.5 ns | 299.2 ns |  1.54 |    0.07 |    5528 B |        7.05 |
|                                |            |             |             |             |          |          |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,834.1 ns | 191.7 ns | 262.4 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  7,418.6 ns | 400.5 ns | 561.5 ns |  1.09 |    0.09 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    631.7 ns | 147.1 ns | 215.7 ns |  0.09 |    0.03 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  6,807.5 ns | 158.8 ns | 217.4 ns |  1.00 |    0.05 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 11,008.6 ns | 159.7 ns | 213.2 ns |  1.61 |    0.07 |    5528 B |        7.05 |
