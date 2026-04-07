```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean          | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |--------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.437 μs | 0.0051 μs | 0.0027 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.140 μs | 0.0073 μs | 0.0048 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.316 μs | 0.1780 μs | 0.1059 μs |  3.00 |    0.07 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.195 μs | 0.0054 μs | 0.0036 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.149 μs | 0.0045 μs | 0.0027 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.427 μs | 0.0240 μs | 0.0159 μs |  0.99 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.182 μs | 0.1566 μs | 0.0819 μs |  5.00 |    0.05 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     11.251 μs | 0.7748 μs | 0.4611 μs |  7.83 |    0.30 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.430 μs | 0.1131 μs | 0.0748 μs |  3.08 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10.255 μs | 0.4433 μs | 0.2638 μs |  7.14 |    0.17 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.192 μs | 0.1005 μs | 0.0598 μs |  2.92 |    0.04 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,035.091 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 18,534.957 μs |        NA | 0.0000 μs |  0.93 |    0.00 |      - |      - |     616 B |        1.24 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,540.772 μs |        NA | 0.0000 μs |  0.13 |    0.00 |      - |      - |    1064 B |        2.15 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,152.462 μs |        NA | 0.0000 μs |  1.06 |    0.00 |      - |      - |     616 B |        1.24 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,662.860 μs |        NA | 0.0000 μs |  1.03 |    0.00 |      - |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,238.019 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 30,161.229 μs |        NA | 0.0000 μs |  1.51 |    0.00 |      - |      - |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 33,602.096 μs |        NA | 0.0000 μs |  1.68 |    0.00 |      - |      - |    2216 B |        4.47 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,626.415 μs |        NA | 0.0000 μs |  0.43 |    0.00 |      - |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 38,271.275 μs |        NA | 0.0000 μs |  1.91 |    0.00 |      - |      - |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,596.245 μs |        NA | 0.0000 μs |  0.23 |    0.00 |      - |      - |    1072 B |        2.16 |
