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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.385 μs | 0.0024 μs | 0.0014 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.175 μs | 0.0043 μs | 0.0025 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.175 μs | 0.1529 μs | 0.0800 μs |  3.01 |    0.05 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.168 μs | 0.0123 μs | 0.0073 μs |  0.84 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.136 μs | 0.0057 μs | 0.0034 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.418 μs | 0.0055 μs | 0.0036 μs |  1.02 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.879 μs | 0.1658 μs | 0.0987 μs |  4.97 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     11.052 μs | 1.0686 μs | 0.6359 μs |  7.98 |    0.44 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.379 μs | 0.1082 μs | 0.0716 μs |  3.16 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      9.907 μs | 0.5143 μs | 0.2690 μs |  7.15 |    0.18 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.252 μs | 0.1734 μs | 0.1032 μs |  3.07 |    0.07 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,235.657 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 18,576.245 μs |        NA | 0.0000 μs |  0.92 |    0.00 |      - |      - |     616 B |        1.24 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,460.688 μs |        NA | 0.0000 μs |  0.12 |    0.00 |      - |      - |    1064 B |        2.15 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,508.705 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |     616 B |        1.24 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,554.030 μs |        NA | 0.0000 μs |  1.02 |    0.00 |      - |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 19,785.770 μs |        NA | 0.0000 μs |  0.98 |    0.00 |      - |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 30,136.133 μs |        NA | 0.0000 μs |  1.49 |    0.00 |      - |      - |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 33,790.857 μs |        NA | 0.0000 μs |  1.67 |    0.00 |      - |      - |    2216 B |        4.47 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,777.076 μs |        NA | 0.0000 μs |  0.43 |    0.00 |      - |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 38,000.880 μs |        NA | 0.0000 μs |  1.88 |    0.00 |      - |      - |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,751.077 μs |        NA | 0.0000 μs |  0.23 |    0.00 |      - |      - |    1072 B |        2.16 |
