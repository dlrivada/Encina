```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   669.0 ns |     16.89 ns |  19.45 ns |  0.78 |    0.03 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   659.1 ns |      2.08 ns |   2.04 ns |  0.77 |    0.03 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   857.4 ns |     26.97 ns |  31.05 ns |  1.00 |    0.05 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   679.1 ns |     16.47 ns |  18.31 ns |  0.79 |    0.03 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   857.9 ns |     12.44 ns |  12.77 ns |  1.00 |    0.04 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,480.4 ns |    267.60 ns | 286.33 ns |  5.23 |    0.37 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,643.0 ns |    766.84 ns | 820.51 ns | 10.09 |    1.00 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,627.0 ns |     80.78 ns |  89.78 ns |  3.07 |    0.15 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 5,921.7 ns |    339.51 ns | 348.65 ns |  6.91 |    0.46 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,398.9 ns |    198.67 ns | 212.58 ns |  3.97 |    0.28 | 0.0114 | 0.0076 |    1091 B |        2.20 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,020.8 ns |    378.32 ns | 420.50 ns |  4.70 |    0.51 | 0.0114 | 0.0076 |    1072 B |        2.16 |
|                               |            |                |             |             |            |              |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   659.4 ns |     75.36 ns |   4.13 ns |  0.79 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   708.4 ns |    422.01 ns |  23.13 ns |  0.84 |    0.02 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   839.4 ns |     28.96 ns |   1.59 ns |  1.00 |    0.00 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   708.2 ns |    620.66 ns |  34.02 ns |  0.84 |    0.04 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   864.5 ns |    271.47 ns |  14.88 ns |  1.03 |    0.02 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,296.2 ns |  3,978.73 ns | 218.09 ns |  5.12 |    0.23 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,443.0 ns | 11,827.94 ns | 648.33 ns |  8.87 |    0.67 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,525.7 ns |  1,083.09 ns |  59.37 ns |  3.01 |    0.06 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 5,593.2 ns |  4,501.08 ns | 246.72 ns |  6.66 |    0.25 | 0.0305 | 0.0153 |    3047 B |        6.14 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,524.3 ns |  1,414.77 ns |  77.55 ns |  4.20 |    0.08 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,140.1 ns |  6,085.65 ns | 333.58 ns |  3.74 |    0.34 | 0.0114 | 0.0076 |    1156 B |        2.33 |
