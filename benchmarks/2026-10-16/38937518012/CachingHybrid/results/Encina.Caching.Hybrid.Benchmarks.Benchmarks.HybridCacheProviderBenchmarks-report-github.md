```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |-----------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |   824.5 ns |     12.27 ns |  14.13 ns |  0.82 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |   826.4 ns |      2.39 ns |   2.35 ns |  0.82 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           | 1,010.9 ns |      6.33 ns |   6.78 ns |  1.00 |    0.01 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |   812.0 ns |      7.54 ns |   8.38 ns |  0.80 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           | 1,027.0 ns |      9.76 ns |  10.45 ns |  1.02 |    0.01 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,542.6 ns |    238.25 ns | 254.92 ns |  4.49 |    0.25 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8,502.7 ns |    626.28 ns | 670.11 ns |  8.41 |    0.65 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,976.8 ns |     79.66 ns |  91.74 ns |  2.94 |    0.09 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 5,985.2 ns |    286.90 ns | 306.98 ns |  5.92 |    0.30 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,237.9 ns |    175.43 ns | 187.71 ns |  3.20 |    0.18 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,526.1 ns |    378.97 ns | 421.23 ns |  3.49 |    0.41 | 0.0114 | 0.0076 |    1191 B |        2.40 |
|                               |            |                |             |             |            |              |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |   822.6 ns |     82.07 ns |   4.50 ns |  0.81 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |   818.7 ns |    147.05 ns |   8.06 ns |  0.81 |    0.01 | 0.0067 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           | 1,013.7 ns |     90.88 ns |   4.98 ns |  1.00 |    0.01 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |   816.6 ns |    365.78 ns |  20.05 ns |  0.81 |    0.02 | 0.0067 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           | 1,027.6 ns |     84.55 ns |   4.63 ns |  1.01 |    0.01 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 4,206.9 ns |  5,852.94 ns | 320.82 ns |  4.15 |    0.27 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7,617.5 ns | 10,793.05 ns | 591.60 ns |  7.52 |    0.51 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,735.2 ns |  1,205.96 ns |  66.10 ns |  2.70 |    0.06 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 5,853.0 ns |  6,951.02 ns | 381.01 ns |  5.77 |    0.33 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,118.1 ns |  2,381.49 ns | 130.54 ns |  3.08 |    0.11 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3,177.5 ns |  3,240.95 ns | 177.65 ns |  3.13 |    0.15 | 0.0114 | 0.0076 |    1072 B |        2.16 |
