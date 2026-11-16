```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |------------:|------------:|-----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           |  5,667.9 ns |    367.8 ns |   326.0 ns |  1.00 |    0.08 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           |  5,460.2 ns |    477.4 ns |   398.7 ns |  0.97 |    0.08 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |    525.1 ns |    144.6 ns |   135.3 ns |  0.09 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           |  5,465.1 ns |    334.3 ns |   312.7 ns |  0.97 |    0.07 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           |  9,622.9 ns |  1,014.4 ns |   899.3 ns |  1.70 |    0.18 |    5528 B |        7.05 |
|                                |            |                |             |             |             |             |            |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           |  6,319.7 ns | 14,245.3 ns |   780.8 ns |  1.01 |    0.15 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           |  7,740.7 ns | 30,561.9 ns | 1,675.2 ns |  1.24 |    0.26 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |    717.5 ns |  2,668.0 ns |   146.2 ns |  0.11 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           |  7,487.0 ns | 30,637.1 ns | 1,679.3 ns |  1.20 |    0.26 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 11,460.5 ns | 26,344.6 ns | 1,444.0 ns |  1.83 |    0.27 |    5528 B |        7.05 |
