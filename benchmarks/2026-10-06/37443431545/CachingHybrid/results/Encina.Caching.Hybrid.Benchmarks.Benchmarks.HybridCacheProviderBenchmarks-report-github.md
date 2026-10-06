```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   826.9 ns |      7.61 ns |   8.77 ns |  0.80 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   832.2 ns |      7.67 ns |   8.84 ns |  0.81 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           | 1,029.1 ns |      8.06 ns |   8.96 ns |  1.00 |    0.01 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   814.3 ns |      4.09 ns |   4.54 ns |  0.79 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           | 1,038.2 ns |     10.01 ns |  11.53 ns |  1.01 |    0.01 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,477.9 ns |    225.41 ns | 241.18 ns |  4.35 |    0.23 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,217.6 ns |    737.81 ns | 789.45 ns |  7.99 |    0.75 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,876.4 ns |    106.47 ns | 122.61 ns |  2.80 |    0.12 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 6,132.1 ns |    297.33 ns | 318.14 ns |  5.96 |    0.31 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,176.7 ns |     92.65 ns |  99.14 ns |  3.09 |    0.10 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,435.1 ns |    285.97 ns | 317.86 ns |  3.34 |    0.30 | 0.0114 | 0.0076 |    1209 B |        2.44 |
|                               |            |                |             |             |            |              |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   808.1 ns |     29.27 ns |   1.60 ns |  0.76 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   845.3 ns |     75.72 ns |   4.15 ns |  0.80 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           | 1,060.4 ns |     77.79 ns |   4.26 ns |  1.00 |    0.00 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   811.9 ns |     82.53 ns |   4.52 ns |  0.77 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           | 1,039.5 ns |    420.91 ns |  23.07 ns |  0.98 |    0.02 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,197.6 ns |  2,957.66 ns | 162.12 ns |  3.96 |    0.13 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 8,558.2 ns | 11,599.08 ns | 635.78 ns |  8.07 |    0.52 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 3,047.3 ns |    503.70 ns |  27.61 ns |  2.87 |    0.02 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 5,902.5 ns |  6,435.28 ns | 352.74 ns |  5.57 |    0.29 | 0.0305 | 0.0153 |    3046 B |        6.14 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,849.8 ns |  7,176.66 ns | 393.38 ns |  2.69 |    0.32 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,137.0 ns |    564.73 ns |  30.95 ns |  2.96 |    0.03 | 0.0114 | 0.0076 |    1072 B |        2.16 |
