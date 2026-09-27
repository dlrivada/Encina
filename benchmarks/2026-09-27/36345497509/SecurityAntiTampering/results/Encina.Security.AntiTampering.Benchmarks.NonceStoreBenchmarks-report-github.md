```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,053.70 ns |      67.381 ns |     72.097 ns |     1,050.89 ns |     1.00 |     0.09 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        50.64 ns |       0.047 ns |      0.051 ns |        50.65 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        21.34 ns |       0.033 ns |      0.034 ns |        21.32 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,373,092.43 ns |  11,857.853 ns | 13,655.521 ns | 5,373,826.14 ns | 5,121.19 |   332.24 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,341.82 ns |  13,725.765 ns |    752.356 ns |       932.16 ns |     1.19 |     0.75 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        54.98 ns |       6.728 ns |      0.369 ns |        54.85 ns |     0.05 |     0.02 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        22.21 ns |       0.871 ns |      0.048 ns |        22.19 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,362,203.36 ns | 270,791.751 ns | 14,843.010 ns | 5,364,493.55 ns | 4,750.01 | 1,748.35 | 7.8125 |      - |  209656 B |    1,455.94 |
