```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-MJLQTR : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

UnrollFactor=1  

```
| Method                         | Job        | InvocationCount | IterationCount | LaunchCount | RunStrategy | WarmupCount | Mean            | Error      | StdDev      | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------------- |----------- |---------------- |--------------- |------------ |------------ |------------ |----------------:|-----------:|------------:|------:|--------:|----------:|------------:|
| TryAcquireAsync_HighLimit      | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     10,202.0 ns | 2,158.4 ns |   560.52 ns |  1.00 |    0.07 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      9,346.7 ns | 4,943.2 ns | 1,283.72 ns |  0.92 |    0.12 |     784 B |        1.00 |
| GetMetrics                     | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |        268.5 ns |   351.0 ns |    54.31 ns |  0.03 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |      8,930.0 ns | 5,347.6 ns | 1,388.74 ns |  0.88 |    0.13 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Job-MJLQTR | 1               | 5              | Default     | Default     | 3           |     10,755.0 ns | 2,048.5 ns |   317.01 ns |  1.06 |    0.06 |    5528 B |        7.05 |
|                                |            |                 |                |             |             |             |                 |            |             |       |         |           |             |
| TryAcquireAsync_HighLimit      | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,702,290.0 ns |         NA |     0.00 ns |  1.00 |    0.00 |     784 B |        1.00 |
| TryAcquireAsync_SmallLimit     | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,289,397.0 ns |         NA |     0.00 ns |  0.96 |    0.00 |     784 B |        1.00 |
| GetMetrics                     | Dry        | Default         | 1              | 1           | ColdStart   | 1           |    504,510.0 ns |         NA |     0.00 ns |  0.05 |    0.00 |         - |        0.00 |
| AcquireAndRelease_Cycle        | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 10,183,106.0 ns |         NA |     0.00 ns |  0.95 |    0.00 |     672 B |        0.86 |
| AcquireMultiple_ThenReleaseAll | Dry        | Default         | 1              | 1           | ColdStart   | 1           | 11,076,634.0 ns |         NA |     0.00 ns |  1.03 |    0.00 |    5528 B |        7.05 |
