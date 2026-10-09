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
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,073.93 ns |      85.598 ns |     91.589 ns |     1,064.98 ns |     1.01 |     0.12 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        52.79 ns |       0.036 ns |      0.037 ns |        52.78 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        20.83 ns |       0.040 ns |      0.044 ns |        20.81 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,375,586.00 ns |  10,667.091 ns | 12,284.238 ns | 5,377,207.94 ns | 5,038.79 |   403.67 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,348.77 ns |  13,818.466 ns |    757.437 ns |       940.96 ns |     1.19 |     0.75 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        52.30 ns |       1.164 ns |      0.064 ns |        52.27 ns |     0.05 |     0.02 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.58 ns |       2.466 ns |      0.135 ns |        21.57 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,392,726.26 ns | 217,170.238 ns | 11,903.834 ns | 5,386,527.22 ns | 4,755.75 | 1,754.90 | 7.8125 |      - |  209664 B |    1,456.00 |
