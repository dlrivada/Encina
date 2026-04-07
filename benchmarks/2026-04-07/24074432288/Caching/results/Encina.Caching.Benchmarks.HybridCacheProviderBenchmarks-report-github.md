```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | 3           |  1.385 μs | 0.0037 μs | 0.0022 μs |  1.384 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | 3           |  1.114 μs | 0.0033 μs | 0.0017 μs |  1.114 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3           |  4.451 μs | 0.3493 μs | 0.2079 μs |  4.451 μs |  3.21 |    0.14 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | 3           |  1.210 μs | 0.0278 μs | 0.0184 μs |  1.205 μs |  0.87 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | 3           |  1.177 μs | 0.0094 μs | 0.0056 μs |  1.175 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | 3           |  1.407 μs | 0.0085 μs | 0.0051 μs |  1.405 μs |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 3           |  6.634 μs | 0.2695 μs | 0.1604 μs |  6.617 μs |  4.79 |    0.11 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | 3           | 10.449 μs | 1.2162 μs | 0.7237 μs | 10.519 μs |  7.55 |    0.50 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 3           |  4.274 μs | 0.0472 μs | 0.0281 μs |  4.283 μs |  3.09 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | 3           | 10.000 μs | 0.4405 μs | 0.2622 μs | 10.007 μs |  7.22 |    0.18 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 3           |  4.401 μs | 0.1971 μs | 0.1173 μs |  4.396 μs |  3.18 |    0.08 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.388 μs | 0.0105 μs | 0.0147 μs |  1.385 μs |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.130 μs | 0.0014 μs | 0.0021 μs |  1.130 μs |  0.81 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.567 μs | 0.0868 μs | 0.1189 μs |  4.567 μs |  3.29 |    0.09 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.174 μs | 0.0020 μs | 0.0029 μs |  1.175 μs |  0.85 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.140 μs | 0.0012 μs | 0.0017 μs |  1.140 μs |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.448 μs | 0.0362 μs | 0.0520 μs |  1.491 μs |  1.04 |    0.04 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  6.823 μs | 0.1243 μs | 0.1783 μs |  6.854 μs |  4.92 |    0.14 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 12.636 μs | 0.7814 μs | 1.1207 μs | 12.457 μs |  9.11 |    0.80 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.290 μs | 0.0538 μs | 0.0805 μs |  4.293 μs |  3.09 |    0.07 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          | 11.111 μs | 0.3709 μs | 0.5319 μs | 11.129 μs |  8.01 |    0.39 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  5.092 μs | 0.4587 μs | 0.6431 μs |  4.804 μs |  3.67 |    0.46 | 0.0687 | 0.0610 |    1265 B |        2.55 |
