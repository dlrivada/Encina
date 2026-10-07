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
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,575.09 ns |      85.152 ns |     87.445 ns |     1,597.50 ns |     1.00 |    0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        63.93 ns |       0.204 ns |      0.218 ns |        63.85 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        25.80 ns |       0.024 ns |      0.024 ns |        25.80 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,412,399.44 ns |  14,640.456 ns | 16,859.971 ns | 5,415,997.05 ns | 3,446.46 |  190.71 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |         |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,900.25 ns |  14,977.716 ns |    820.979 ns |     1,446.79 ns |     1.11 |    0.55 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        65.36 ns |       6.573 ns |      0.360 ns |        65.23 ns |     0.04 |    0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        25.71 ns |       0.156 ns |      0.009 ns |        25.71 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,407,696.53 ns | 334,184.318 ns | 18,317.770 ns | 5,405,457.09 ns | 3,160.88 |  947.77 | 7.8125 |      - |  209664 B |    1,456.00 |
