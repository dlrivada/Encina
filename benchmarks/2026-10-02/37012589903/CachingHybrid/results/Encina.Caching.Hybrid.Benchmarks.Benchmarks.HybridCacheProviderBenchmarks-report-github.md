```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.138 μs | 0.0042 μs | 0.0048 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.169 μs | 0.0020 μs | 0.0022 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.391 μs | 0.0025 μs | 0.0028 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.118 μs | 0.0029 μs | 0.0030 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.403 μs | 0.0031 μs | 0.0036 μs |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.908 μs | 0.1481 μs | 0.1585 μs |  4.96 |    0.11 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.142 μs | 0.6959 μs | 0.7446 μs |  8.01 |    0.52 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.344 μs | 0.0903 μs | 0.1003 μs |  3.12 |    0.07 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.215 μs | 0.4130 μs | 0.4241 μs |  7.34 |    0.30 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.234 μs | 0.1261 μs | 0.1349 μs |  3.04 |    0.09 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.248 μs | 0.1205 μs | 0.1184 μs |  3.05 |    0.08 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.099 μs | 0.0663 μs | 0.0036 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.155 μs | 0.1779 μs | 0.0098 μs |  0.83 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.389 μs | 0.0495 μs | 0.0027 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.105 μs | 0.5800 μs | 0.0318 μs |  0.80 |    0.02 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.412 μs | 0.0762 μs | 0.0042 μs |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.678 μs | 2.5227 μs | 0.1383 μs |  4.81 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           |  9.921 μs | 2.9246 μs | 0.1603 μs |  7.14 |    0.10 | 0.1831 | 0.1678 |    3407 B |        6.87 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.410 μs | 0.9480 μs | 0.0520 μs |  3.17 |    0.03 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.603 μs | 2.3501 μs | 0.1288 μs |  6.91 |    0.08 | 0.1678 | 0.1526 |    3048 B |        6.15 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  3.966 μs | 1.5584 μs | 0.0854 μs |  2.86 |    0.05 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.026 μs | 1.0675 μs | 0.0585 μs |  2.90 |    0.04 | 0.0610 | 0.0534 |    1072 B |        2.16 |
