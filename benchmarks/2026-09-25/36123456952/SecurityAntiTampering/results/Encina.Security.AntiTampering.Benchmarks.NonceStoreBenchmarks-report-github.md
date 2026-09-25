```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error          | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|---------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,564.49 ns |      81.691 ns |     83.890 ns |     1,583.06 ns |     1.00 |    0.07 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        59.46 ns |       0.051 ns |      0.056 ns |        59.46 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        25.70 ns |       0.069 ns |      0.071 ns |        25.67 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,412,331.53 ns |  24,143.413 ns | 27,803.591 ns | 5,405,815.05 ns | 3,469.03 |  185.06 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |                |               |                 |          |         |        |        |           |             |
| Add         | ShortRun   | 3              | 1           | 3           |     1,853.87 ns |  14,812.622 ns |    811.930 ns |     1,417.68 ns |     1.11 |    0.55 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | ShortRun   | 3              | 1           | 3           |        59.40 ns |       0.206 ns |      0.011 ns |        59.39 ns |     0.04 |    0.01 |      - |      - |         - |        0.00 |
| Exists_Miss | ShortRun   | 3              | 1           | 3           |        25.72 ns |       0.753 ns |      0.041 ns |        25.71 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | ShortRun   | 3              | 1           | 3           | 5,431,576.48 ns | 405,067.520 ns | 22,203.118 ns | 5,422,019.23 ns | 3,263.77 |  991.30 | 7.8125 |      - |  209656 B |    1,455.94 |
