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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    37.79 ns |     0.091 ns |   0.089 ns |  0.61 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    42.88 ns |     0.207 ns |   0.221 ns |  0.70 |    0.02 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    61.57 ns |     1.425 ns |   1.525 ns |  1.00 |    0.03 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    39.16 ns |     0.973 ns |   1.120 ns |  0.64 |    0.02 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   103.38 ns |     3.267 ns |   3.762 ns |  1.68 |    0.07 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,242.80 ns |   451.400 ns | 482.993 ns | 68.95 |    7.82 | 0.0114 | 0.0076 |    1104 B |        7.67 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,949.45 ns |    13.002 ns |  14.452 ns | 31.68 |    0.79 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,242.81 ns |   286.796 ns | 318.773 ns | 52.70 |    5.21 | 0.0076 | 0.0038 |     824 B |        5.72 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,133.43 ns |   407.540 ns | 436.063 ns | 50.92 |    7.01 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    40.52 ns |     4.783 ns |   0.262 ns |  0.68 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    41.84 ns |     4.140 ns |   0.227 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    59.79 ns |     4.710 ns |   0.258 ns |  1.00 |    0.01 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    39.80 ns |     1.410 ns |   0.077 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   106.83 ns |    33.861 ns |   1.856 ns |  1.79 |    0.03 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,422.93 ns | 8,498.984 ns | 465.858 ns | 57.25 |    6.75 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,907.21 ns |    98.346 ns |   5.391 ns | 31.90 |    0.14 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,780.52 ns | 2,915.605 ns | 159.814 ns | 46.50 |    2.32 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,826.60 ns | 4,578.626 ns | 250.970 ns | 47.27 |    3.64 | 0.0076 | 0.0038 |     712 B |        4.94 |
