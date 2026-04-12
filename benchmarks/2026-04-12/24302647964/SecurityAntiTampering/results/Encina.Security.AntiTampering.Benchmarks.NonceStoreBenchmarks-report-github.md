```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error        | StdDev        | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|-------------:|--------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,257.97 ns |    66.928 ns |     68.730 ns |     1.00 |    0.08 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        56.36 ns |     0.040 ns |      0.039 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        28.97 ns |     0.073 ns |      0.084 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,417,710.09 ns | 9,657.721 ns | 11,121.846 ns | 4,318.70 |  227.58 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |              |               |          |         |        |        |           |             |
| Add         | MediumRun  | 15             | 2           | 10          |     1,253.98 ns |    30.323 ns |     39.428 ns |     1.00 |    0.04 | 0.0076 | 0.0057 |     144 B |        1.00 |
| Exists_Hit  | MediumRun  | 15             | 2           | 10          |        56.02 ns |     0.135 ns |      0.198 ns |     0.04 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | MediumRun  | 15             | 2           | 10          |        22.27 ns |     0.019 ns |      0.027 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | MediumRun  | 15             | 2           | 10          | 5,419,711.46 ns | 9,407.240 ns | 14,080.315 ns | 4,326.22 |  137.78 | 7.8125 |      - |  209656 B |    1,455.94 |
