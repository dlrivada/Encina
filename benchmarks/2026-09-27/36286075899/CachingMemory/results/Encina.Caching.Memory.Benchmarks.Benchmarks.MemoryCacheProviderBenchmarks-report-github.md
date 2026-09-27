```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error      | StdDev     | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-----------:|-----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    39.38 ns |   0.121 ns |   0.125 ns |    39.37 ns |  0.66 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    42.74 ns |   1.707 ns |   1.966 ns |    42.08 ns |  0.72 |    0.03 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    59.37 ns |   0.400 ns |   0.428 ns |    59.24 ns |  1.00 |    0.01 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    37.60 ns |   0.055 ns |   0.054 ns |    37.60 ns |  0.63 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |    91.56 ns |   3.182 ns |   3.536 ns |    90.60 ns |  1.54 |    0.06 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,757.35 ns | 515.783 ns | 551.882 ns | 3,570.64 ns | 63.29 |    9.06 | 0.0114 | 0.0076 |    1119 B |        7.77 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,926.30 ns |  27.313 ns |  29.225 ns | 1,923.11 ns | 32.45 |    0.53 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,154.53 ns | 346.986 ns | 385.674 ns | 3,269.01 ns | 53.13 |    6.34 | 0.0076 | 0.0038 |     808 B |        5.61 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 2,998.42 ns | 500.653 ns | 535.693 ns | 3,011.25 ns | 50.50 |    8.79 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |            |            |             |       |         |        |        |           |             |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |    39.54 ns |   1.138 ns |   1.703 ns |    39.11 ns |  0.64 |    0.04 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |    42.59 ns |   0.210 ns |   0.301 ns |    42.48 ns |  0.69 |    0.03 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |    62.13 ns |   2.182 ns |   3.129 ns |    61.37 ns |  1.00 |    0.07 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |    39.50 ns |   1.134 ns |   1.626 ns |    38.58 ns |  0.64 |    0.04 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |    97.24 ns |   4.366 ns |   6.535 ns |    97.71 ns |  1.57 |    0.13 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          | 3,978.21 ns | 415.399 ns | 595.753 ns | 3,876.30 ns | 64.18 |    9.94 | 0.0114 | 0.0076 |    1141 B |        7.92 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          | 1,894.85 ns |  14.656 ns |  21.482 ns | 1,893.09 ns | 30.57 |    1.50 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          | 3,144.94 ns | 201.697 ns | 295.644 ns | 3,187.05 ns | 50.74 |    5.29 | 0.0076 | 0.0038 |     817 B |        5.67 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          | 3,019.49 ns | 379.464 ns | 544.215 ns | 2,978.98 ns | 48.71 |    8.95 | 0.0076 | 0.0038 |     712 B |        4.94 |
