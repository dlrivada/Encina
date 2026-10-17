```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       895.98 ns |      46.550 ns |     49.808 ns |       888.82 ns |     1.00 |     0.08 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        51.16 ns |       0.115 ns |      0.132 ns |        51.12 ns |     0.06 |     0.00 |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        19.48 ns |       0.129 ns |      0.148 ns |        19.49 ns |     0.02 |     0.00 |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,304,446.86 ns |   7,449.952 ns |  7,971.365 ns | 5,306,354.71 ns | 5,937.50 |   319.13 |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,378.16 ns |  16,769.626 ns |    919.200 ns |       859.14 ns |     1.27 |     0.95 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        52.60 ns |       3.705 ns |      0.203 ns |        52.52 ns |     0.05 |     0.02 |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        20.53 ns |       1.115 ns |      0.061 ns |        20.54 ns |     0.02 |     0.01 |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,332,758.35 ns | 383,527.388 ns | 21,022.430 ns | 5,324,709.39 ns | 4,924.35 | 2,055.19 |  209656 B |    1,455.94 |
