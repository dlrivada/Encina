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
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     |  1.158 μs | 0.0058 μs | 0.0038 μs |  0.84 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     |  1.125 μs | 0.0069 μs | 0.0045 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     |  1.400 μs | 0.0061 μs | 0.0040 μs |  1.01 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     |  6.805 μs | 0.2444 μs | 0.1455 μs |  4.92 |    0.10 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | 10.643 μs | 1.0347 μs | 0.6157 μs |  7.70 |    0.42 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     |  4.263 μs | 0.0980 μs | 0.0648 μs |  3.08 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     | 10.204 μs | 0.4861 μs | 0.2893 μs |  7.38 |    0.20 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     |  4.075 μs | 0.1658 μs | 0.0986 μs |  2.95 |    0.07 | 0.0610 | 0.0534 |    1072 B |        2.16 |
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     |  1.383 μs | 0.0043 μs | 0.0023 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     |  1.113 μs | 0.0082 μs | 0.0054 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     |  4.073 μs | 0.1965 μs | 0.1169 μs |  2.95 |    0.08 | 0.0610 | 0.0534 |    1064 B |        2.15 |
|                               |            |                |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_True              | ShortRun   | 3              | 1           |  1.157 μs | 0.0537 μs | 0.0029 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | ShortRun   | 3              | 1           |  1.115 μs | 0.0108 μs | 0.0006 μs |  0.77 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           |  1.380 μs | 0.1168 μs | 0.0064 μs |  0.96 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           |  6.564 μs | 0.8492 μs | 0.0465 μs |  4.55 |    0.03 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 10.026 μs | 3.9216 μs | 0.2150 μs |  6.96 |    0.13 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           |  4.357 μs | 0.3865 μs | 0.0212 μs |  3.02 |    0.01 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           |  9.531 μs | 3.8279 μs | 0.2098 μs |  6.61 |    0.13 | 0.1678 | 0.1526 |    3046 B |        6.14 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           |  4.009 μs | 1.4895 μs | 0.0816 μs |  2.78 |    0.05 | 0.0610 | 0.0534 |    1072 B |        2.16 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           |  1.441 μs | 0.0317 μs | 0.0017 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           |  1.126 μs | 0.0166 μs | 0.0009 μs |  0.78 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | ShortRun   | 3              | 1           |  4.062 μs | 0.7028 μs | 0.0385 μs |  2.82 |    0.02 | 0.0610 | 0.0534 |    1064 B |        2.15 |
