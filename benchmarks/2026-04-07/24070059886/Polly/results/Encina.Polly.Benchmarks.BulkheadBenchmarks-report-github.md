```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

UnrollFactor=1  

```
| Method                         | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean            | Error      | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |----------------:|-----------:|----------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      6,942.9 ns |   972.2 ns | 252.49 ns |  1.00 |    0.05 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      6,764.0 ns | 1,083.9 ns | 281.48 ns |  0.98 |    0.05 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |        526.0 ns |   236.0 ns |  36.52 ns |  0.08 |    0.01 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      6,843.2 ns |   710.6 ns | 109.97 ns |  0.99 |    0.04 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     10,834.5 ns |   954.7 ns | 147.74 ns |  1.56 |    0.05 |    5528 B |        7.05 |
|                                |            |                 |                |             |             |             |                 |            |           |       |         |           |             |
| TryAcquireAsync_HighLimit      | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,755,124.0 ns |         NA |   0.00 ns |  1.00 |    0.00 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,694,310.0 ns |         NA |   0.00 ns |  0.99 |    0.00 |     784 B |        1.00 |
| GetMetrics                     | Dry        | Default         | 1              | 1           | ColdStart   | 1           |    588,749.0 ns |         NA |   0.00 ns |  0.05 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,390,874.0 ns |         NA |   0.00 ns |  0.97 |    0.00 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 11,469,307.0 ns |         NA |   0.00 ns |  1.07 |    0.00 |    5528 B |        7.05 |
