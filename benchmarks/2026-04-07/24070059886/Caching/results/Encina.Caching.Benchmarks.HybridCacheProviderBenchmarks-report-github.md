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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.430 μs | 0.0171 μs | 0.0113 μs |  1.00 |    0.01 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.108 μs | 0.0052 μs | 0.0031 μs |  0.77 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.192 μs | 0.1417 μs | 0.0843 μs |  2.93 |    0.06 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.154 μs | 0.0112 μs | 0.0067 μs |  0.81 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.143 μs | 0.0031 μs | 0.0018 μs |  0.80 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.409 μs | 0.0039 μs | 0.0023 μs |  0.99 |    0.01 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      6.837 μs | 0.1696 μs | 0.0887 μs |  4.78 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10.582 μs | 1.2598 μs | 0.7497 μs |  7.40 |    0.50 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.222 μs | 0.1340 μs | 0.0886 μs |  2.95 |    0.06 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10.194 μs | 0.4588 μs | 0.2400 μs |  7.13 |    0.17 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.159 μs | 0.2168 μs | 0.1290 μs |  2.91 |    0.09 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,394.445 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 18,717.272 μs |        NA | 0.0000 μs |  0.92 |    0.00 |      - |      - |     616 B |        1.24 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,447.157 μs |        NA | 0.0000 μs |  0.12 |    0.00 |      - |      - |    1064 B |        2.15 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,597.118 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |     616 B |        1.24 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,669.345 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 19,949.776 μs |        NA | 0.0000 μs |  0.98 |    0.00 |      - |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 30,126.624 μs |        NA | 0.0000 μs |  1.48 |    0.00 |      - |      - |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 33,412.239 μs |        NA | 0.0000 μs |  1.64 |    0.00 |      - |      - |    2216 B |        4.47 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,191.677 μs |        NA | 0.0000 μs |  0.40 |    0.00 |      - |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 37,894.838 μs |        NA | 0.0000 μs |  1.86 |    0.00 |      - |      - |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,510.901 μs |        NA | 0.0000 μs |  0.22 |    0.00 |      - |      - |    1072 B |        2.16 |
