```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,562.52 ns |      78.287 ns |     76.888 ns |     1,592.59 ns |     1.00 |    0.07 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        63.85 ns |       0.090 ns |      0.100 ns |        63.84 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        25.67 ns |       0.039 ns |      0.042 ns |        25.67 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,468,752.27 ns |   8,433.148 ns |  9,711.626 ns | 5,468,344.07 ns | 3,508.23 |  173.90 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |         |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,885.92 ns |  15,486.702 ns |    848.878 ns |     1,418.73 ns |     1.12 |    0.57 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        59.83 ns |       1.310 ns |      0.072 ns |        59.82 ns |     0.04 |    0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        25.94 ns |       2.745 ns |      0.150 ns |        25.95 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,430,268.59 ns | 464,914.544 ns | 25,483.535 ns | 5,431,786.28 ns | 3,225.57 |  999.62 | 7.8125 |      - |  209656 B |    1,455.94 |
