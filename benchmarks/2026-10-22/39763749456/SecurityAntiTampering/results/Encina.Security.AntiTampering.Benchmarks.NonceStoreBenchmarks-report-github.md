```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD  | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|---------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,074.04 ns |      69.432 ns |     74.292 ns |     1,076.52 ns |     1.00 |     0.09 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        52.25 ns |       0.034 ns |      0.038 ns |        52.24 ns |     0.05 |     0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        20.41 ns |       0.041 ns |      0.044 ns |        20.41 ns |     0.02 |     0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,418,098.80 ns |  11,553.106 ns | 13,304.574 ns | 5,415,661.25 ns | 5,066.86 |   333.27 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |          |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,390.50 ns |  14,349.096 ns |    786.522 ns |       962.93 ns |     1.19 |     0.76 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        51.57 ns |       1.035 ns |      0.057 ns |        51.56 ns |     0.04 |     0.02 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        21.05 ns |       2.823 ns |      0.155 ns |        21.11 ns |     0.02 |     0.01 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,381,667.89 ns | 182,526.532 ns | 10,004.895 ns | 5,384,301.28 ns | 4,613.98 | 1,709.95 | 7.8125 |      - |  209656 B |    1,455.94 |
