```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.175 μs | 0.0025 μs | 0.0027 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.190 μs | 0.0023 μs | 0.0026 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.408 μs | 0.0041 μs | 0.0045 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.135 μs | 0.0030 μs | 0.0032 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.400 μs | 0.0050 μs | 0.0056 μs |  0.99 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  6.798 μs | 0.1141 μs | 0.1172 μs |  4.83 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.428 μs | 0.6057 μs | 0.6481 μs |  8.12 |    0.45 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  4.467 μs | 0.0257 μs | 0.0296 μs |  3.17 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           |  9.914 μs | 0.2715 μs | 0.2788 μs |  7.04 |    0.19 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.415 μs | 0.1305 μs | 0.1396 μs |  3.14 |    0.10 | 0.0610 | 0.0572 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.315 μs | 0.2602 μs | 0.2672 μs |  3.07 |    0.18 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.140 μs | 0.0473 μs | 0.0026 μs |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.193 μs | 0.0433 μs | 0.0024 μs |  0.84 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.426 μs | 0.3400 μs | 0.0186 μs |  1.00 |    0.02 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.122 μs | 0.1219 μs | 0.0067 μs |  0.79 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.422 μs | 0.0734 μs | 0.0040 μs |  1.00 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.621 μs | 2.4959 μs | 0.1368 μs |  4.64 |    0.10 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 10.180 μs | 5.0147 μs | 0.2749 μs |  7.14 |    0.19 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  4.475 μs | 1.0450 μs | 0.0573 μs |  3.14 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.290 μs | 3.7642 μs | 0.2063 μs |  6.52 |    0.15 | 0.1678 | 0.1526 |    3048 B |        6.15 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.051 μs | 0.6592 μs | 0.0361 μs |  2.84 |    0.04 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.130 μs | 1.0259 μs | 0.0562 μs |  2.90 |    0.05 | 0.0610 | 0.0534 |    1072 B |        2.16 |
