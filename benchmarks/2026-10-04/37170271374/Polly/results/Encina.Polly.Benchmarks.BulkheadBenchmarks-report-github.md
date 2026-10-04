```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |------------:|----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           |  6,815.4 ns |  86.86 ns |  77.00 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           |  6,940.1 ns | 134.81 ns | 112.57 ns |  1.02 |    0.02 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |    481.8 ns |  25.17 ns |  21.02 ns |  0.07 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           |  6,742.1 ns | 102.63 ns |  90.98 ns |  0.99 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 10,798.7 ns | 166.38 ns | 138.93 ns |  1.58 |    0.03 |    5528 B |        7.05 |
|                                |            |             |             |             |           |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          |  6,703.8 ns |  72.33 ns |  96.55 ns |  1.00 |    0.02 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          |  6,892.5 ns | 234.04 ns | 328.09 ns |  1.03 |    0.05 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |    473.2 ns |  38.36 ns |  52.51 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          |  6,659.0 ns |  90.15 ns | 123.39 ns |  0.99 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 10,586.0 ns | 209.35 ns | 286.57 ns |  1.58 |    0.05 |    5528 B |        7.05 |
