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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.413 μs | 0.0069 μs | 0.0041 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.099 μs | 0.0065 μs | 0.0039 μs |  0.78 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.211 μs | 0.1432 μs | 0.0852 μs |  2.98 |    0.06 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.178 μs | 0.0020 μs | 0.0011 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.133 μs | 0.0018 μs | 0.0011 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      1.395 μs | 0.0045 μs | 0.0027 μs |  0.99 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      7.060 μs | 0.1930 μs | 0.1149 μs |  5.00 |    0.08 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10.852 μs | 1.0685 μs | 0.6358 μs |  7.68 |    0.43 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.277 μs | 0.0437 μs | 0.0228 μs |  3.03 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     10.442 μs | 0.2826 μs | 0.1682 μs |  7.39 |    0.11 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4.370 μs | 0.1997 μs | 0.1188 μs |  3.09 |    0.08 | 0.0610 | 0.0534 |    1072 B |        2.16 |
|                               |            |                |             |             |              |             |               |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,591.540 μs |        NA | 0.0000 μs |  1.00 |    0.00 |      - |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 19,326.501 μs |        NA | 0.0000 μs |  0.94 |    0.00 |      - |      - |     616 B |        1.24 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,614.648 μs |        NA | 0.0000 μs |  0.13 |    0.00 |      - |      - |    1064 B |        2.15 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,681.245 μs |        NA | 0.0000 μs |  1.05 |    0.00 |      - |      - |     616 B |        1.24 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 21,043.227 μs |        NA | 0.0000 μs |  1.02 |    0.00 |      - |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 20,842.152 μs |        NA | 0.0000 μs |  1.01 |    0.00 |      - |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 31,308.729 μs |        NA | 0.0000 μs |  1.52 |    0.00 |      - |      - |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 34,285.881 μs |        NA | 0.0000 μs |  1.67 |    0.00 |      - |      - |    2216 B |        4.47 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,784.746 μs |        NA | 0.0000 μs |  0.43 |    0.00 |      - |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 40,614.884 μs |        NA | 0.0000 μs |  1.97 |    0.00 |      - |      - |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,835.633 μs |        NA | 0.0000 μs |  0.23 |    0.00 |      - |      - |    1072 B |        2.16 |
