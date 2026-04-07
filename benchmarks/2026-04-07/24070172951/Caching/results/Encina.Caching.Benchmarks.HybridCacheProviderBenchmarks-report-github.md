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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     |  1.370 μs | 0.0063 μs | 0.0042 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     |  1.103 μs | 0.0061 μs | 0.0040 μs |  0.80 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     |  4.062 μs | 0.0616 μs | 0.0367 μs |  2.96 |    0.03 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     |  1.158 μs | 0.0056 μs | 0.0037 μs |  0.85 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     |  1.107 μs | 0.0033 μs | 0.0022 μs |  0.81 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     |  1.414 μs | 0.0068 μs | 0.0041 μs |  1.03 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     |  6.471 μs | 0.1660 μs | 0.0988 μs |  4.72 |    0.07 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-YFEFPZ | 10             | Default     | 10.308 μs | 0.9242 μs | 0.5500 μs |  7.52 |    0.38 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     |  4.362 μs | 0.1217 μs | 0.0724 μs |  3.18 |    0.05 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-YFEFPZ | 10             | Default     |  9.934 μs | 0.6270 μs | 0.3279 μs |  7.25 |    0.23 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     |  4.162 μs | 0.1541 μs | 0.0806 μs |  3.04 |    0.06 | 0.0610 | 0.0572 |    1072 B |        2.16 |
|                               |            |                |             |           |           |           |       |         |        |        |           |             |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           |  1.445 μs | 0.0754 μs | 0.0041 μs |  1.00 |    0.00 | 0.0286 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           |  1.089 μs | 0.1065 μs | 0.0058 μs |  0.75 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| SetAsync                      | ShortRun   | 3              | 1           |  3.932 μs | 0.6163 μs | 0.0338 μs |  2.72 |    0.02 | 0.0610 | 0.0534 |    1064 B |        2.15 |
| ExistsAsync_True              | ShortRun   | 3              | 1           |  1.139 μs | 0.0516 μs | 0.0028 μs |  0.79 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| ExistsAsync_False             | ShortRun   | 3              | 1           |  1.101 μs | 0.0455 μs | 0.0025 μs |  0.76 |    0.00 | 0.0362 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           |  1.385 μs | 0.0568 μs | 0.0031 μs |  0.96 |    0.00 | 0.0324 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           |  6.698 μs | 1.1152 μs | 0.0611 μs |  4.64 |    0.04 | 0.1068 | 0.0992 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           |  9.588 μs | 5.3912 μs | 0.2955 μs |  6.64 |    0.18 | 0.1221 | 0.1068 |    2216 B |        4.47 |
| RemoveAsync                   | ShortRun   | 3              | 1           |  4.322 μs | 0.5298 μs | 0.0290 μs |  2.99 |    0.02 | 0.0610 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           |  9.660 μs | 2.9895 μs | 0.1639 μs |  6.69 |    0.10 | 0.1373 | 0.1221 |    2448 B |        4.94 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           |  4.060 μs | 0.5782 μs | 0.0317 μs |  2.81 |    0.02 | 0.0610 | 0.0572 |    1072 B |        2.16 |
