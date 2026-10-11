```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.69GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method      | Job        | IterationCount | LaunchCount | WarmupCount | Mean            | Error         | StdDev        | Median          | Ratio    | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------ |----------- |--------------- |------------ |------------ |----------------:|--------------:|--------------:|----------------:|---------:|--------:|-------:|-------:|----------:|------------:|
| Add         | Job-NUBXJZ | 20             | Default     | 5           |     1,056.16 ns |     64.298 ns |     66.029 ns |     1,059.71 ns |     1.00 |    0.09 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | Job-NUBXJZ | 20             | Default     | 5           |        53.51 ns |      0.049 ns |      0.055 ns |        53.49 ns |     0.05 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | Job-NUBXJZ | 20             | Default     | 5           |        21.41 ns |      0.023 ns |      0.026 ns |        21.41 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | Job-NUBXJZ | 20             | Default     | 5           | 5,366,139.24 ns | 10,893.105 ns | 12,544.516 ns | 5,365,470.23 ns | 5,099.07 |  303.29 | 7.8125 |      - |  209656 B |    1,455.94 |
|             |            |                |             |             |                 |               |               |                 |          |         |        |        |           |             |
| Add         | MediumRun  | 15             | 2           | 10          |     1,068.54 ns |     41.448 ns |     55.332 ns |     1,072.14 ns |     1.00 |    0.07 | 0.0057 | 0.0038 |     144 B |        1.00 |
| Exists_Hit  | MediumRun  | 15             | 2           | 10          |        56.75 ns |      2.774 ns |      3.978 ns |        60.31 ns |     0.05 |    0.00 |      - |      - |         - |        0.00 |
| Exists_Miss | MediumRun  | 15             | 2           | 10          |        21.42 ns |      0.067 ns |      0.092 ns |        21.44 ns |     0.02 |    0.00 |      - |      - |         - |        0.00 |
| Cleanup     | MediumRun  | 15             | 2           | 10          | 5,399,873.80 ns |  6,606.569 ns |  9,888.402 ns | 5,398,814.26 ns | 5,066.17 |  251.02 | 7.8125 |      - |  209656 B |    1,455.94 |
