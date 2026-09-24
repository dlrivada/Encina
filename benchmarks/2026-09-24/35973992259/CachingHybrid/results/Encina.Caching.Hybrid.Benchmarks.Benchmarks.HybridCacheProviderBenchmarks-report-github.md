```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   673.9 ns |     4.91 ns |   4.82 ns |  0.79 |    0.02 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   682.5 ns |    21.83 ns |  25.14 ns |  0.80 |    0.03 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |   858.6 ns |    18.09 ns |  20.11 ns |  1.00 |    0.03 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   677.5 ns |    28.33 ns |  32.62 ns |  0.79 |    0.04 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   860.5 ns |    34.77 ns |  40.04 ns |  1.00 |    0.05 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,427.4 ns |   232.33 ns | 248.60 ns |  5.16 |    0.30 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,551.6 ns |   517.61 ns | 531.54 ns |  9.97 |    0.64 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,533.8 ns |    42.24 ns |  48.65 ns |  2.95 |    0.09 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 6,958.2 ns |   431.56 ns | 461.76 ns |  8.11 |    0.55 | 0.0229 | 0.0153 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,216.0 ns |   264.31 ns | 271.42 ns |  3.75 |    0.32 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,671.7 ns |   252.18 ns | 280.30 ns |  4.28 |    0.33 | 0.0114 | 0.0076 |    1131 B |        2.28 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   690.6 ns |   558.90 ns |  30.64 ns |  0.82 |    0.04 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   665.6 ns |    69.36 ns |   3.80 ns |  0.79 |    0.02 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |   846.2 ns |   473.55 ns |  25.96 ns |  1.00 |    0.04 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   659.2 ns |    57.06 ns |   3.13 ns |  0.78 |    0.02 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   840.0 ns |    87.77 ns |   4.81 ns |  0.99 |    0.03 | 0.0067 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,254.6 ns | 4,500.28 ns | 246.68 ns |  5.03 |    0.28 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,479.7 ns | 4,601.95 ns | 252.25 ns |  8.84 |    0.35 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,559.8 ns |   395.47 ns |  21.68 ns |  3.03 |    0.08 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 5,302.9 ns | 5,908.22 ns | 323.85 ns |  6.27 |    0.37 | 0.0305 | 0.0153 |    3045 B |        6.14 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,123.6 ns | 2,069.71 ns | 113.45 ns |  3.69 |    0.15 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,738.1 ns | 4,837.06 ns | 265.14 ns |  3.24 |    0.28 | 0.0114 | 0.0076 |    1114 B |        2.25 |
