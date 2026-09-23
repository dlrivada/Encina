```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    39.82 ns |     0.313 ns |   0.322 ns |  0.59 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    42.96 ns |     0.243 ns |   0.250 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    68.10 ns |     1.167 ns |   1.344 ns |  1.00 |    0.03 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    38.43 ns |     0.191 ns |   0.188 ns |  0.56 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   110.80 ns |     1.572 ns |   1.810 ns |  1.63 |    0.04 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,374.41 ns |   635.730 ns | 680.224 ns | 64.26 |    9.80 | 0.0114 | 0.0076 |    1103 B |        7.66 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,125.20 ns |    39.261 ns |  45.213 ns | 31.22 |    0.88 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,048.41 ns |   362.504 ns | 402.922 ns | 44.78 |    5.83 | 0.0076 | 0.0038 |     799 B |        5.55 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,106.31 ns |   443.740 ns | 474.797 ns | 45.63 |    6.84 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    42.18 ns |    24.175 ns |   1.325 ns |  0.63 |    0.02 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    45.91 ns |    29.230 ns |   1.602 ns |  0.69 |    0.02 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    66.57 ns |    18.782 ns |   1.029 ns |  1.00 |    0.02 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    40.70 ns |     8.928 ns |   0.489 ns |  0.61 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   106.92 ns |     4.343 ns |   0.238 ns |  1.61 |    0.02 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,939.66 ns | 6,371.979 ns | 349.270 ns | 59.19 |    4.61 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,999.76 ns |   783.557 ns |  42.949 ns | 30.04 |    0.69 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,742.42 ns | 3,135.384 ns | 171.861 ns | 41.20 |    2.30 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,718.39 ns | 4,081.138 ns | 223.701 ns | 40.84 |    2.96 | 0.0076 | 0.0038 |     712 B |        4.94 |
