```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.376 μs | 0.0032 μs | 0.0021 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.125 μs | 0.0047 μs | 0.0024 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.273 μs | 0.1741 μs | 0.1036 μs |  3.11 |    0.07 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.188 μs | 0.0031 μs | 0.0018 μs |  0.86 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.117 μs | 0.0049 μs | 0.0029 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.398 μs | 0.0036 μs | 0.0021 μs |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.945 μs | 0.2541 μs | 0.1512 μs |  5.05 |    0.10 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10.713 μs | 0.9015 μs | 0.5365 μs |  7.79 |    0.37 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.299 μs | 0.2275 μs | 0.1504 μs |  3.12 |    0.10 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      9.834 μs | 0.3825 μs | 0.2276 μs |  7.15 |    0.16 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.198 μs | 0.1545 μs | 0.0920 μs |  3.05 |    0.06 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,343.082 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 18,752.527 μs |        NA | 0.0000 μs |  0.92 |    0.00 |      - |      - |     616 B |        1.24 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,544.495 μs |        NA | 0.0000 μs |  0.13 |    0.00 |      - |      - |    1064 B |        2.15 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,139.517 μs |        NA | 0.0000 μs |  1.04 |    0.00 |      - |      - |     616 B |        1.24 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,878.153 μs |        NA | 0.0000 μs |  1.08 |    0.00 |      - |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,467.185 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 30,551.953 μs |        NA | 0.0000 μs |  1.50 |    0.00 |      - |      - |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 33,683.842 μs |        NA | 0.0000 μs |  1.66 |    0.00 |      - |      - |    2216 B |        4.47 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,580.365 μs |        NA | 0.0000 μs |  0.42 |    0.00 |      - |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 38,762.725 μs |        NA | 0.0000 μs |  1.91 |    0.00 |      - |      - |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,826.449 μs |        NA | 0.0000 μs |  0.24 |    0.00 |      - |      - |    1072 B |        2.16 |
