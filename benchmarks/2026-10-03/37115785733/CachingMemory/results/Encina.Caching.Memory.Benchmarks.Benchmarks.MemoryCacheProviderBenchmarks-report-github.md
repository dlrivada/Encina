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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    38.32 ns |     0.015 ns |   0.014 ns |  0.68 |    0.03 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    40.38 ns |     0.043 ns |   0.044 ns |  0.71 |    0.03 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    56.77 ns |     2.384 ns |   2.746 ns |  1.00 |    0.07 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    40.03 ns |     0.326 ns |   0.320 ns |  0.71 |    0.03 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |    84.29 ns |     3.563 ns |   4.103 ns |  1.49 |    0.10 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,598.21 ns |   344.440 ns | 368.547 ns | 63.52 |    6.95 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,942.63 ns |    70.314 ns |  80.973 ns | 34.29 |    2.08 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 2,806.19 ns |   370.127 ns | 411.395 ns | 49.54 |    7.43 | 0.0076 | 0.0038 |     816 B |        5.67 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 2,816.65 ns |   513.860 ns | 549.825 ns | 49.72 |    9.72 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    37.03 ns |     0.339 ns |   0.019 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    43.68 ns |     2.766 ns |   0.152 ns |  0.75 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    58.40 ns |    22.513 ns |   1.234 ns |  1.00 |    0.03 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    41.89 ns |    42.070 ns |   2.306 ns |  0.72 |    0.04 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |    79.56 ns |     3.718 ns |   0.204 ns |  1.36 |    0.03 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,441.22 ns | 8,106.465 ns | 444.343 ns | 58.94 |    6.68 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,842.84 ns |   137.710 ns |   7.548 ns | 31.57 |    0.59 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,242.73 ns | 4,329.895 ns | 237.336 ns | 38.42 |    3.59 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,575.96 ns | 5,086.459 ns | 278.806 ns | 44.12 |    4.22 | 0.0076 | 0.0038 |     712 B |        4.94 |
