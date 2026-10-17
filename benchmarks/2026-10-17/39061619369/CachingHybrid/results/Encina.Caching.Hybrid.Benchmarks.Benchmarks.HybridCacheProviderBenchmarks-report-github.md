```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |---------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           | 1.052 μs | 0.0033 μs | 0.0038 μs |  0.80 |    0.00 | 0.0057 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           | 1.109 μs | 0.0022 μs | 0.0024 μs |  0.84 |    0.00 | 0.0057 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           | 1.314 μs | 0.0030 μs | 0.0034 μs |  1.00 |    0.00 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           | 1.027 μs | 0.0021 μs | 0.0023 μs |  0.78 |    0.00 | 0.0057 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           | 1.192 μs | 0.0017 μs | 0.0020 μs |  0.91 |    0.00 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5.317 μs | 0.3141 μs | 0.3360 μs |  4.05 |    0.25 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 9.736 μs | 0.7217 μs | 0.7722 μs |  7.41 |    0.57 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 3.740 μs | 0.1019 μs | 0.1173 μs |  2.85 |    0.09 | 0.0076 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7.025 μs | 0.2749 μs | 0.2941 μs |  5.35 |    0.22 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3.869 μs | 0.1484 μs | 0.1588 μs |  2.94 |    0.12 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4.227 μs | 0.4884 μs | 0.5428 μs |  3.22 |    0.40 | 0.0114 | 0.0076 |    1091 B |        2.20 |
|                               |            |                |             |             |          |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           | 1.033 μs | 0.1235 μs | 0.0068 μs |  0.79 |    0.00 | 0.0057 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           | 1.097 μs | 0.0913 μs | 0.0050 μs |  0.84 |    0.00 | 0.0057 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           | 1.313 μs | 0.0157 μs | 0.0009 μs |  1.00 |    0.00 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           | 1.037 μs | 0.0561 μs | 0.0031 μs |  0.79 |    0.00 | 0.0057 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           | 1.207 μs | 0.1452 μs | 0.0080 μs |  0.92 |    0.01 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 5.401 μs | 4.2387 μs | 0.2323 μs |  4.11 |    0.15 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 9.270 μs | 8.4211 μs | 0.4616 μs |  7.06 |    0.30 | 0.0229 | 0.0153 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 3.768 μs | 0.9638 μs | 0.0528 μs |  2.87 |    0.03 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 6.786 μs | 5.8271 μs | 0.3194 μs |  5.17 |    0.21 | 0.0305 | 0.0153 |    3050 B |        6.15 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3.480 μs | 5.0093 μs | 0.2746 μs |  2.65 |    0.18 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 3.764 μs | 2.0532 μs | 0.1125 μs |  2.87 |    0.07 | 0.0114 | 0.0076 |    1072 B |        2.16 |
