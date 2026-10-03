```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,267.41 ns |      58.889 ns |     57.837 ns |     1,290.82 ns |     1.00 |     0.06 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        59.46 ns |       0.138 ns |      0.159 ns |        59.43 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        22.72 ns |       0.025 ns |      0.027 ns |        22.72 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,432,518.49 ns |  18,172.560 ns | 20,927.547 ns | 5,425,953.75 ns | 4,295.11 |   200.18 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,580.79 ns |  14,333.948 ns |    785.692 ns |     1,154.63 ns |     1.15 |     0.65 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        61.64 ns |       0.907 ns |      0.050 ns |        61.61 ns |     0.04 |     0.02 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        22.80 ns |       1.615 ns |      0.089 ns |        22.84 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,418,744.81 ns | 444,487.090 ns | 24,363.837 ns | 5,410,087.97 ns | 3,932.15 | 1,319.23 | 7.8125 |      - |  209656 B |    1,455.94 |
