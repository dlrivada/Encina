```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 4.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Median     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   650.0 ns |      4.09 ns |   4.55 ns |   649.2 ns |  0.77 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   700.9 ns |     32.97 ns |  36.65 ns |   723.2 ns |  0.83 |    0.04 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   840.0 ns |      3.39 ns |   3.62 ns |   840.2 ns |  1.00 |    0.01 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   690.8 ns |     24.40 ns |  28.10 ns |   672.5 ns |  0.82 |    0.03 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   846.9 ns |      1.89 ns |   1.95 ns |   847.3 ns |  1.01 |    0.00 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,534.2 ns |    272.64 ns | 291.72 ns | 4,573.4 ns |  5.40 |    0.34 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,743.4 ns |    631.21 ns | 675.39 ns | 8,640.9 ns | 10.41 |    0.78 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,506.8 ns |     33.33 ns |  35.66 ns | 2,517.4 ns |  2.98 |    0.04 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 5,693.6 ns |    278.22 ns | 285.71 ns | 5,783.9 ns |  6.78 |    0.33 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,218.7 ns |    207.28 ns | 212.86 ns | 3,237.9 ns |  3.83 |    0.25 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,533.8 ns |    303.46 ns | 337.29 ns | 3,501.7 ns |  4.21 |    0.39 | 0.0114 | 0.0076 |    1138 B |        2.29 |
|                               |            |                |             |             |            |              |           |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   647.8 ns |     11.90 ns |   0.65 ns |   647.6 ns |  0.78 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   697.9 ns |    218.51 ns |  11.98 ns |   697.6 ns |  0.84 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   830.0 ns |     14.84 ns |   0.81 ns |   830.5 ns |  1.00 |    0.00 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   669.5 ns |     58.82 ns |   3.22 ns |   668.9 ns |  0.81 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   847.8 ns |     25.34 ns |   1.39 ns |   848.4 ns |  1.02 |    0.00 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,259.7 ns |  4,953.01 ns | 271.49 ns | 4,121.0 ns |  5.13 |    0.28 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,242.8 ns | 12,472.59 ns | 683.66 ns | 6,849.8 ns |  8.73 |    0.71 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,620.0 ns |  2,113.65 ns | 115.86 ns | 2,565.2 ns |  3.16 |    0.12 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 5,513.1 ns |  7,950.26 ns | 435.78 ns | 5,512.8 ns |  6.64 |    0.45 | 0.0305 | 0.0153 |    3045 B |        6.14 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,120.9 ns |  1,832.19 ns | 100.43 ns | 3,133.1 ns |  3.76 |    0.10 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,976.2 ns |  1,801.54 ns |  98.75 ns | 2,975.2 ns |  3.59 |    0.10 | 0.0114 | 0.0076 |    1150 B |        2.32 |
