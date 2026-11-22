```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  IterationCount=15  UnrollFactor=1  

```
| Method                         | Job        | LaunchCount | WarmupCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |------------ |------------ |-----------:|----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | Default     | 5           | 5,277.9 ns | 719.30 ns | 637.64 ns |  1.01 |    0.16 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | Default     | 5           | 5,227.4 ns | 316.09 ns | 280.21 ns |  1.00 |    0.12 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | Default     | 5           |   366.2 ns |  35.92 ns |  30.00 ns |  0.07 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | Default     | 5           | 5,667.9 ns | 547.52 ns | 485.36 ns |  1.09 |    0.15 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | Default     | 5           | 8,028.0 ns | 139.22 ns | 108.69 ns |  1.54 |    0.17 |    5528 B |        7.05 |
|                                |            |             |             |            |           |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 2           | 10          | 5,151.7 ns | 141.21 ns | 193.29 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 2           | 10          | 5,209.6 ns | 133.82 ns | 187.59 ns |  1.01 |    0.05 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 2           | 10          |   406.5 ns |  75.31 ns | 108.01 ns |  0.08 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 2           | 10          | 5,114.1 ns | 185.68 ns | 266.29 ns |  0.99 |    0.06 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 2           | 10          | 8,469.9 ns | 237.50 ns | 340.61 ns |  1.65 |    0.09 |    5528 B |        7.05 |
