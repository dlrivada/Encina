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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           | 1.106 μs | 0.0167 μs | 0.0179 μs |  0.81 |    0.02 | 0.0057 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           | 1.123 μs | 0.0097 μs | 0.0104 μs |  0.82 |    0.02 | 0.0057 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           | 1.373 μs | 0.0201 μs | 0.0231 μs |  1.00 |    0.02 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           | 1.093 μs | 0.0227 μs | 0.0262 μs |  0.80 |    0.02 | 0.0057 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           | 1.219 μs | 0.0116 μs | 0.0133 μs |  0.89 |    0.02 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 5.860 μs | 0.4891 μs | 0.5233 μs |  4.27 |    0.38 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 8.307 μs | 0.6532 μs | 0.6989 μs |  6.05 |    0.51 | 0.0153 |      - |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 3.798 μs | 0.1008 μs | 0.1161 μs |  2.77 |    0.09 | 0.0114 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 7.402 μs | 0.4199 μs | 0.4312 μs |  5.39 |    0.32 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 4.224 μs | 0.2171 μs | 0.2323 μs |  3.08 |    0.17 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 4.531 μs | 0.4242 μs | 0.4539 μs |  3.30 |    0.33 | 0.0114 | 0.0076 |    1072 B |        2.16 |
|                               |            |                |             |             |          |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           | 1.085 μs | 0.2109 μs | 0.0116 μs |  0.77 |    0.02 | 0.0057 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           | 1.153 μs | 0.1548 μs | 0.0085 μs |  0.82 |    0.02 | 0.0057 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           | 1.414 μs | 0.7418 μs | 0.0407 μs |  1.00 |    0.04 | 0.0057 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           | 1.061 μs | 0.1720 μs | 0.0094 μs |  0.75 |    0.02 | 0.0057 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           | 1.239 μs | 0.3227 μs | 0.0177 μs |  0.88 |    0.02 | 0.0057 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 5.555 μs | 3.3454 μs | 0.1834 μs |  3.93 |    0.15 | 0.0153 | 0.0076 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 7.838 μs | 8.6152 μs | 0.4722 μs |  5.55 |    0.32 | 0.0305 | 0.0153 |    3401 B |        6.86 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 3.558 μs | 5.9726 μs | 0.3274 μs |  2.52 |    0.21 | 0.0076 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 6.919 μs | 7.0754 μs | 0.3878 μs |  4.89 |    0.27 | 0.0153 |      - |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3.945 μs | 7.0922 μs | 0.3887 μs |  2.79 |    0.25 | 0.0114 | 0.0076 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 4.112 μs | 7.4688 μs | 0.4094 μs |  2.91 |    0.26 | 0.0114 | 0.0076 |    1072 B |        2.16 |
