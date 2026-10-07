```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.174 μs | 0.0027 μs | 0.0031 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.205 μs | 0.0043 μs | 0.0049 μs |  0.85 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.418 μs | 0.0068 μs | 0.0078 μs |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.131 μs | 0.0021 μs | 0.0025 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.412 μs | 0.0047 μs | 0.0053 μs |  1.00 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.995 μs | 0.1721 μs | 0.1767 μs |  4.93 |    0.12 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.628 μs | 0.7847 μs | 0.8397 μs |  8.20 |    0.58 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.471 μs | 0.0344 μs | 0.0396 μs |  3.15 |    0.03 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.637 μs | 0.3799 μs | 0.4065 μs |  7.50 |    0.28 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.417 μs | 0.1793 μs | 0.1919 μs |  3.11 |    0.13 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.357 μs | 0.1888 μs | 0.1854 μs |  3.07 |    0.13 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.168 μs | 0.0625 μs | 0.0034 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.187 μs | 0.0263 μs | 0.0014 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.426 μs | 0.0770 μs | 0.0042 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.125 μs | 0.1223 μs | 0.0067 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.426 μs | 0.1015 μs | 0.0056 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.544 μs | 1.1730 μs | 0.0643 μs |  4.59 |    0.04 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 10.680 μs | 5.6940 μs | 0.3121 μs |  7.49 |    0.19 | 0.1831 | 0.1678 |    3415 B |        6.89 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.410 μs | 0.5295 μs | 0.0290 μs |  3.09 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           | 10.399 μs | 6.2786 μs | 0.3442 μs |  7.29 |    0.21 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.165 μs | 1.3129 μs | 0.0720 μs |  2.92 |    0.04 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.125 μs | 0.2055 μs | 0.0113 μs |  2.89 |    0.01 | 0.0610 | 0.0534 |    1072 B |        2.16 |
