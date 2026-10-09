```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   951.9 ns |     2.21 ns |   2.37 ns |  0.79 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   963.8 ns |     2.11 ns |   2.43 ns |  0.80 |    0.00 | 0.0057 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           | 1,199.1 ns |     1.13 ns |   1.21 ns |  1.00 |    0.00 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   945.7 ns |     1.33 ns |   1.42 ns |  0.79 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           | 1,209.3 ns |     1.29 ns |   1.49 ns |  1.01 |    0.00 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5,371.1 ns |   290.92 ns | 311.28 ns |  4.48 |    0.25 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 7,591.8 ns |   363.62 ns | 389.07 ns |  6.33 |    0.32 | 0.0153 |      - |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 3,472.1 ns |    35.99 ns |  41.44 ns |  2.90 |    0.03 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7,148.8 ns |   315.88 ns | 324.38 ns |  5.96 |    0.26 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,812.0 ns |   205.18 ns | 219.54 ns |  3.18 |    0.18 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4,109.5 ns |   438.94 ns | 487.88 ns |  3.43 |    0.40 | 0.0114 | 0.0076 |    1205 B |        2.43 |
|                               |            |                |             |             |            |             |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   949.3 ns |    89.24 ns |   4.89 ns |  0.79 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   976.1 ns |    38.62 ns |   2.12 ns |  0.81 |    0.00 | 0.0057 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           | 1,200.5 ns |    38.93 ns |   2.13 ns |  1.00 |    0.00 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   945.7 ns |    16.70 ns |   0.92 ns |  0.79 |    0.00 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           | 1,209.8 ns |    74.52 ns |   4.08 ns |  1.01 |    0.00 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 5,209.1 ns | 2,238.37 ns | 122.69 ns |  4.34 |    0.09 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,257.9 ns | 7,323.08 ns | 401.40 ns |  6.05 |    0.29 | 0.0305 | 0.0153 |    3420 B |        6.90 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 3,359.9 ns | 2,855.70 ns | 156.53 ns |  2.80 |    0.11 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 6,906.6 ns | 8,779.70 ns | 481.24 ns |  5.75 |    0.35 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,408.9 ns | 2,528.43 ns | 138.59 ns |  2.84 |    0.10 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,477.8 ns | 4,668.23 ns | 255.88 ns |  2.90 |    0.18 | 0.0114 | 0.0076 |    1113 B |        2.24 |
