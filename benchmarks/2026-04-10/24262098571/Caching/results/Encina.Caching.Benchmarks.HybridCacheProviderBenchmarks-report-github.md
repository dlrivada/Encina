```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     |  1.160 μs | 0.0077 μs | 0.0046 μs |  0.83 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     |  1.149 μs | 0.0138 μs | 0.0082 μs |  0.82 |    0.01 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     |  1.396 μs | 0.0048 μs | 0.0029 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     |  6.706 μs | 0.2227 μs | 0.1325 μs |  4.79 |    0.09 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | 10.496 μs | 0.8785 μs | 0.5228 μs |  7.50 |    0.36 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     |  4.382 μs | 0.0859 μs | 0.0568 μs |  3.13 |    0.04 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | 10.267 μs | 0.7486 μs | 0.3915 μs |  7.34 |    0.26 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     |  4.248 μs | 0.1446 μs | 0.0861 μs |  3.04 |    0.06 | 0.0610 | 0.0534 |    1072 B |        2.16 |
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     |  1.399 μs | 0.0075 μs | 0.0039 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     |  1.120 μs | 0.0021 μs | 0.0011 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     |  4.164 μs | 0.1413 μs | 0.0841 μs |  2.98 |    0.06 | 0.0610 | 0.0534 |    1064 B |        2.15 |
|                               |            |                |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_True              | ShortRun   | 3              | 1           |  1.155 μs | 0.0339 μs | 0.0019 μs |  0.82 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | ShortRun   | 3              | 1           |  1.118 μs | 0.0461 μs | 0.0025 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           |  1.400 μs | 0.0906 μs | 0.0050 μs |  1.00 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           |  6.988 μs | 6.4058 μs | 0.3511 μs |  4.97 |    0.22 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           |  9.736 μs | 3.6738 μs | 0.2014 μs |  6.93 |    0.12 | 0.1831 | 0.1678 |    3417 B |        6.89 |
| RemoveAsync                   | ShortRun   | 3              | 1           |  4.382 μs | 1.3212 μs | 0.0724 μs |  3.12 |    0.04 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 10.128 μs | 4.5876 μs | 0.2515 μs |  7.20 |    0.16 | 0.1678 | 0.1526 |    3041 B |        6.13 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           |  4.036 μs | 1.0780 μs | 0.0591 μs |  2.87 |    0.04 | 0.0610 | 0.0534 |    1072 B |        2.16 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           |  1.406 μs | 0.0359 μs | 0.0020 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           |  1.113 μs | 0.0876 μs | 0.0048 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | ShortRun   | 3              | 1           |  4.044 μs | 0.3808 μs | 0.0209 μs |  2.88 |    0.01 | 0.0610 | 0.0534 |    1064 B |        2.15 |
