```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  Job-IAMMPO : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|-------------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  6,875.2 ns |    161.06 ns |   142.77 ns |  1.00 |    0.03 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  6,907.6 ns |    191.63 ns |   169.87 ns |  1.01 |    0.03 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    539.4 ns |     39.34 ns |    34.87 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  6,818.6 ns |    105.84 ns |    93.83 ns |  0.99 |    0.02 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 13,056.7 ns |  2,141.45 ns | 2,003.12 ns |  1.90 |    0.28 |    5528 B |        7.05 |
|                                |            |                |             |             |             |              |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  7,188.5 ns |  4,124.05 ns |   226.05 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,640.3 ns | 11,133.77 ns |   610.28 ns |  1.06 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    551.7 ns |  1,804.22 ns |    98.90 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,254.0 ns |  3,047.30 ns |   167.03 ns |  1.01 |    0.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,528.0 ns |  6,701.86 ns |   367.35 ns |  1.60 |    0.06 |    5528 B |        7.05 |
