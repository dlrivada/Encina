```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,086.03 ns |      75.167 ns |     77.191 ns |     1,090.07 ns |     1.00 |     0.10 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        37.56 ns |       0.558 ns |      0.642 ns |        37.52 ns |     0.03 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        13.00 ns |       0.265 ns |      0.305 ns |        13.01 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,327,306.96 ns |  20,514.741 ns | 23,624.806 ns | 5,324,592.62 ns | 4,928.59 |   340.29 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,329.12 ns |  12,749.105 ns |    698.821 ns |       950.07 ns |     1.17 |     0.69 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        39.49 ns |       3.871 ns |      0.212 ns |        39.37 ns |     0.03 |     0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        13.05 ns |       1.340 ns |      0.073 ns |        13.01 ns |     0.01 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,311,672.85 ns | 302,954.031 ns | 16,605.933 ns | 5,316,623.54 ns | 4,656.20 | 1,631.94 | 7.8125 |      - |  209656 B |    1,455.94 |
