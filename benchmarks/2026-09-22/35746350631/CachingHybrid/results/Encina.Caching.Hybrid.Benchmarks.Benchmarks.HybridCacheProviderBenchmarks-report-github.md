```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   657.7 ns |      4.77 ns |   5.49 ns |  0.78 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   676.9 ns |      6.51 ns |   7.24 ns |  0.81 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   840.4 ns |      2.66 ns |   2.62 ns |  1.00 |    0.00 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   671.1 ns |      5.09 ns |   5.23 ns |  0.80 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   891.9 ns |     30.90 ns |  35.59 ns |  1.06 |    0.04 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,286.0 ns |    225.68 ns | 241.48 ns |  5.10 |    0.28 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 9,043.0 ns |    779.25 ns | 833.79 ns | 10.76 |    0.97 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,580.2 ns |    100.25 ns | 115.44 ns |  3.07 |    0.13 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 5,691.2 ns |    300.23 ns | 321.24 ns |  6.77 |    0.37 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,205.7 ns |    164.03 ns | 175.51 ns |  3.81 |    0.20 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,640.1 ns |    238.91 ns | 265.54 ns |  4.33 |    0.31 | 0.0114 | 0.0076 |    1078 B |        2.17 |
|                               |            |                |             |             |            |              |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   663.2 ns |    159.61 ns |   8.75 ns |  0.80 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   685.3 ns |    449.91 ns |  24.66 ns |  0.82 |    0.03 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   831.6 ns |    129.44 ns |   7.09 ns |  1.00 |    0.01 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   664.3 ns |     42.78 ns |   2.34 ns |  0.80 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   855.2 ns |    637.12 ns |  34.92 ns |  1.03 |    0.04 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,364.7 ns |  2,126.76 ns | 116.57 ns |  5.25 |    0.13 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 8,305.0 ns | 12,310.81 ns | 674.80 ns |  9.99 |    0.71 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,578.6 ns |  1,018.69 ns |  55.84 ns |  3.10 |    0.06 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 6,545.4 ns |  7,186.75 ns | 393.93 ns |  7.87 |    0.41 | 0.0229 | 0.0153 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,151.4 ns |  2,855.67 ns | 156.53 ns |  3.79 |    0.17 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,152.5 ns |  5,081.75 ns | 278.55 ns |  3.79 |    0.29 | 0.0114 | 0.0076 |    1072 B |        2.16 |
