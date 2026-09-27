```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.69GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,042.81 ns |      68.588 ns |     73.389 ns |     1,045.86 ns |     1.00 |     0.10 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        54.33 ns |       0.052 ns |      0.060 ns |        54.33 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        21.01 ns |       0.032 ns |      0.033 ns |        21.00 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,381,813.32 ns |  15,589.473 ns | 17,952.861 ns | 5,384,326.54 ns | 5,184.22 |   343.65 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,339.58 ns |  13,705.755 ns |    751.259 ns |       930.30 ns |     1.19 |     0.75 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        57.97 ns |       1.428 ns |      0.078 ns |        57.97 ns |     0.05 |     0.02 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.29 ns |       0.734 ns |      0.040 ns |        21.31 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,387,957.58 ns | 133,839.639 ns |  7,336.202 ns | 5,386,549.66 ns | 4,781.09 | 1,759.93 | 7.8125 |      - |  209664 B |    1,456.00 |
