```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,540.5 ns |    519.2 ns |   433.5 ns |  1.01 |    0.10 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,611.9 ns |    411.7 ns |   385.1 ns |  1.02 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    425.1 ns |    136.9 ns |   121.3 ns |  0.08 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,587.6 ns |    586.3 ns |   548.4 ns |  1.01 |    0.12 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  8,436.6 ns |    432.6 ns |   361.2 ns |  1.53 |    0.13 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  6,116.3 ns |  8,511.8 ns |   466.6 ns |  1.00 |    0.09 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  6,302.0 ns | 11,335.0 ns |   621.3 ns |  1.03 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    420.7 ns |  2,103.2 ns |   115.3 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  6,373.0 ns |  5,472.4 ns |   300.0 ns |  1.05 |    0.08 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 14,725.7 ns | 79,336.5 ns | 4,348.7 ns |  2.42 |    0.64 |    5528 B |        7.05 |
