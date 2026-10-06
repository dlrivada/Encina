```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev       | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|-------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,575.63 ns |      85.604 ns |    87.909 ns |     1,601.58 ns |     1.00 |    0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        59.52 ns |       0.022 ns |     0.025 ns |        59.52 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        25.70 ns |       0.042 ns |     0.043 ns |        25.68 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,397,190.66 ns |   6,871.329 ns | 7,637.464 ns | 5,395,091.33 ns | 3,435.73 |  191.32 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |              |                 |          |         |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,870.97 ns |  14,797.088 ns |   811.078 ns |     1,433.86 ns |     1.11 |    0.55 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        59.41 ns |       0.549 ns |     0.030 ns |        59.40 ns |     0.04 |    0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        25.70 ns |       0.281 ns |     0.015 ns |        25.69 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,436,986.92 ns | 110,853.169 ns | 6,076.236 ns | 5,438,312.34 ns | 3,230.37 |  972.80 | 7.8125 |      - |  209656 B |    1,455.94 |
