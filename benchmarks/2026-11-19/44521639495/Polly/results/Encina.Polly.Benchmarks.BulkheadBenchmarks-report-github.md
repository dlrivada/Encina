```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-IAMMPO : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

```
| Method                         | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |--------------- |------------ |------------ |-----------:|------------:|---------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-IAMMPO | 15             | Default     | 5           | 5,607.8 ns |    664.8 ns | 589.3 ns |  1.01 |    0.14 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-IAMMPO | 15             | Default     | 5           | 5,014.3 ns |    540.0 ns | 450.9 ns |  0.90 |    0.11 |     784 B |        1.00 |
| GetMetrics                     | Job-IAMMPO | 15             | Default     | 5           |   370.1 ns |    138.6 ns | 115.8 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-IAMMPO | 15             | Default     | 5           | 5,040.0 ns |    365.4 ns | 323.9 ns |  0.91 |    0.10 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-IAMMPO | 15             | Default     | 5           | 8,465.7 ns |    446.8 ns | 417.9 ns |  1.52 |    0.16 |    5528 B |        7.05 |
|                                |            |                |             |             |            |             |          |       |         |           |             |
| TryAcquireAsync_HighLimit      | ShortRun   | 3              | 1           | 3           | 5,651.7 ns |  9,367.1 ns | 513.4 ns |  1.01 |    0.11 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | ShortRun   | 3              | 1           | 3           | 6,245.8 ns |  7,066.7 ns | 387.4 ns |  1.11 |    0.10 |     784 B |        1.00 |
| GetMetrics                     | ShortRun   | 3              | 1           | 3           |   394.0 ns |  2,662.4 ns | 145.9 ns |  0.07 |    0.02 |         - |        0.00 |
| AcquireAndRelease_Cycle        | ShortRun   | 3              | 1           | 3           | 5,904.0 ns | 11,870.9 ns | 650.7 ns |  1.05 |    0.13 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | ShortRun   | 3              | 1           | 3           | 9,560.3 ns | 13,163.2 ns | 721.5 ns |  1.70 |    0.17 |    5528 B |        7.05 |
