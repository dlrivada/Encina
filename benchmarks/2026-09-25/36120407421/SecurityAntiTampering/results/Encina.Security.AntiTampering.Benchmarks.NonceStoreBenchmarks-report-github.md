```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,547.90 ns |      84.324 ns |     86.595 ns |     1,568.33 ns |     1.00 |    0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        67.22 ns |       0.274 ns |      0.293 ns |        67.13 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        25.68 ns |       0.029 ns |      0.031 ns |        25.67 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,400,253.29 ns |  14,273.128 ns | 15,864.544 ns | 5,397,230.76 ns | 3,499.12 |  191.94 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |         |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,852.24 ns |  14,505.110 ns |    795.074 ns |     1,424.23 ns |     1.11 |    0.54 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        59.52 ns |       0.887 ns |      0.049 ns |        59.52 ns |     0.04 |    0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        25.68 ns |       0.153 ns |      0.008 ns |        25.69 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,416,895.66 ns | 486,342.086 ns | 26,658.051 ns | 5,416,229.13 ns | 3,244.61 |  969.54 | 7.8125 |      - |  209656 B |    1,455.94 |
