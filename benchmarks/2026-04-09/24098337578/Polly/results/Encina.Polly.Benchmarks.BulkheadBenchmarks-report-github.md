```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 5              | Default     | 3           |  7,130.6 ns |   792.79 ns |   205.9 ns |  1.00 |    0.04 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 5              | Default     | 3           |  6,934.8 ns | 1,704.34 ns |   263.7 ns |  0.97 |    0.04 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 5              | Default     | 3           |    485.9 ns |   537.58 ns |   139.6 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 5              | Default     | 3           |  6,918.2 ns | 1,229.65 ns |   190.3 ns |  0.97 |    0.03 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 5              | Default     | 3           | 11,326.8 ns | 2,010.90 ns |   522.2 ns |  1.59 |    0.08 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | MediumRun  | 15             | 2           | 10          |  7,057.1 ns |   298.43 ns |   418.4 ns |  1.00 |    0.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | MediumRun  | 15             | 2           | 10          |  6,901.0 ns |   292.41 ns |   390.4 ns |  0.98 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | MediumRun  | 15             | 2           | 10          |    578.4 ns |    77.73 ns |   113.9 ns |  0.08 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | MediumRun  | 15             | 2           | 10          |  7,148.9 ns |   487.44 ns |   714.5 ns |  1.02 |    0.11 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | MediumRun  | 15             | 2           | 10          | 14,083.7 ns | 1,188.45 ns | 1,742.0 ns |  2.00 |    0.27 |    5528 B |        7.05 |
