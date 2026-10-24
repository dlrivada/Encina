```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |       951.62 ns |      78.701 ns |     84.209 ns |       959.84 ns |     1.01 |     0.12 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        43.78 ns |       0.108 ns |      0.111 ns |        43.73 ns |     0.05 |     0.00 |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        18.04 ns |       0.147 ns |      0.169 ns |        18.08 ns |     0.02 |     0.00 |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,290,582.55 ns |   9,431.061 ns | 10,860.824 ns | 5,290,135.45 ns | 5,601.11 |   485.88 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,244.29 ns |  12,033.838 ns |    659.615 ns |       876.66 ns |     1.17 |     0.70 | 0.0010 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        45.63 ns |       1.443 ns |      0.079 ns |        45.67 ns |     0.04 |     0.02 |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        17.30 ns |       2.265 ns |      0.124 ns |        17.35 ns |     0.02 |     0.01 |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,284,182.81 ns | 134,266.743 ns |  7,359.613 ns | 5,286,413.54 ns | 4,958.56 | 1,744.95 |      - |  209656 B |    1,455.94 |
