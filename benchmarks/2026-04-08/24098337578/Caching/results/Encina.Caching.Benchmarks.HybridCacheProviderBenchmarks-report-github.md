```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | 3           |  1.144 μs | 0.0018 μs | 0.0011 μs |  1.144 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | 3           |  1.096 μs | 0.0050 μs | 0.0033 μs |  1.096 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | 3           |  1.385 μs | 0.0033 μs | 0.0022 μs |  1.385 μs |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 3           |  6.579 μs | 0.1596 μs | 0.0950 μs |  6.569 μs |  4.78 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | 3           | 10.337 μs | 0.9534 μs | 0.5674 μs | 10.251 μs |  7.52 |    0.39 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 3           |  4.275 μs | 0.1684 μs | 0.0881 μs |  4.301 μs |  3.11 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | 3           |  9.639 μs | 0.6750 μs | 0.3530 μs |  9.577 μs |  7.01 |    0.24 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 3           |  4.036 μs | 0.1627 μs | 0.0968 μs |  4.081 μs |  2.93 |    0.07 | 0.0610 | 0.0534 |    1072 B |        2.16 |
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | 3           |  1.375 μs | 0.0071 μs | 0.0047 μs |  1.374 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | 3           |  1.116 μs | 0.0041 μs | 0.0027 μs |  1.115 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3           |  4.098 μs | 0.1343 μs | 0.0799 μs |  4.106 μs |  2.98 |    0.06 | 0.0610 | 0.0534 |    1064 B |        2.15 |
|                               |            |                |             |             |           |           |           |           |       |         |        |        |           |             |
| ExistsAsync_True              | MediumRun  | 15             | 2           | 10          |  1.150 μs | 0.0099 μs | 0.0142 μs |  1.160 μs |  0.83 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | MediumRun  | 15             | 2           | 10          |  1.120 μs | 0.0137 μs | 0.0200 μs |  1.104 μs |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | MediumRun  | 15             | 2           | 10          |  1.407 μs | 0.0061 μs | 0.0089 μs |  1.413 μs |  1.01 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | MediumRun  | 15             | 2           | 10          |  6.530 μs | 0.0682 μs | 0.0934 μs |  6.526 μs |  4.69 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | MediumRun  | 15             | 2           | 10          | 11.331 μs | 0.4348 μs | 0.6235 μs | 11.180 μs |  8.13 |    0.44 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | MediumRun  | 15             | 2           | 10          |  4.311 μs | 0.0697 μs | 0.1022 μs |  4.344 μs |  3.09 |    0.07 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | MediumRun  | 15             | 2           | 10          |  9.784 μs | 0.1483 μs | 0.2078 μs |  9.765 μs |  7.02 |    0.15 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | MediumRun  | 15             | 2           | 10          |  4.208 μs | 0.1047 μs | 0.1433 μs |  4.270 μs |  3.02 |    0.10 | 0.0610 | 0.0572 |    1072 B |        2.16 |
| GetAsync_CacheHit             | MediumRun  | 15             | 2           | 10          |  1.393 μs | 0.0051 μs | 0.0075 μs |  1.393 μs |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | MediumRun  | 15             | 2           | 10          |  1.096 μs | 0.0031 μs | 0.0044 μs |  1.096 μs |  0.79 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | MediumRun  | 15             | 2           | 10          |  4.329 μs | 0.0891 μs | 0.1249 μs |  4.330 μs |  3.11 |    0.09 | 0.0610 | 0.0534 |    1064 B |        2.15 |
