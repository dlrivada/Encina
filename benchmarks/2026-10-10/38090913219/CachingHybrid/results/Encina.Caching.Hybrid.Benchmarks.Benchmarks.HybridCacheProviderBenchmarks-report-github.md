```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |  1.214 μs | 0.0034 μs | 0.0039 μs |  0.78 |    0.00 | 0.0229 |      - |     616 B |        1.24 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |  1.320 μs | 0.0062 μs | 0.0072 μs |  0.85 |    0.01 | 0.0229 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |  1.549 μs | 0.0044 μs | 0.0051 μs |  1.00 |    0.00 | 0.0191 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |  1.158 μs | 0.0032 μs | 0.0037 μs |  0.75 |    0.00 | 0.0229 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |  1.557 μs | 0.0042 μs | 0.0047 μs |  1.01 |    0.00 | 0.0210 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           |  7.179 μs | 0.1078 μs | 0.1107 μs |  4.64 |    0.07 | 0.0687 | 0.0610 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | Job-NUBXJZ | 20             | Default     | 5           | 11.393 μs | 0.4486 μs | 0.4800 μs |  7.36 |    0.30 | 0.0763 | 0.0610 |    2216 B |        4.47 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           |  3.995 μs | 0.1781 μs | 0.2051 μs |  2.58 |    0.13 | 0.0381 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | Job-NUBXJZ | 20             | Default     | 5           | 10.157 μs | 0.1071 μs | 0.1052 μs |  6.56 |    0.07 | 0.0916 | 0.0763 |    2448 B |        4.94 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           |  4.212 μs | 0.0937 μs | 0.0963 μs |  2.72 |    0.06 | 0.0420 | 0.0381 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           |  4.261 μs | 0.3512 μs | 0.3450 μs |  2.75 |    0.22 | 0.0496 | 0.0458 |    1288 B |        2.60 |
|                               |            |                |             |             |           |           |           |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |  1.195 μs | 0.0506 μs | 0.0028 μs |  0.77 |    0.00 | 0.0229 |      - |     616 B |        1.24 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |  1.232 μs | 0.0382 μs | 0.0021 μs |  0.79 |    0.00 | 0.0229 |      - |     616 B |        1.24 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |  1.557 μs | 0.0795 μs | 0.0044 μs |  1.00 |    0.00 | 0.0191 |      - |     496 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |  1.176 μs | 0.1036 μs | 0.0057 μs |  0.75 |    0.00 | 0.0229 |      - |     616 B |        1.24 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |  1.570 μs | 0.0248 μs | 0.0014 μs |  1.01 |    0.00 | 0.0210 |      - |     560 B |        1.13 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           |  6.871 μs | 0.9318 μs | 0.0511 μs |  4.41 |    0.03 | 0.0687 | 0.0610 |    1792 B |        3.61 |
| GetOrSetAsync_WithTags        | ShortRun   | 3              | 1           | 3           | 10.428 μs | 1.0020 μs | 0.0549 μs |  6.70 |    0.03 | 0.1221 | 0.1068 |    3413 B |        6.88 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           |  3.994 μs | 0.1237 μs | 0.0068 μs |  2.56 |    0.01 | 0.0420 |      - |    1136 B |        2.29 |
| RemoveByTagAsync              | ShortRun   | 3              | 1           | 3           |  9.655 μs | 8.4894 μs | 0.4653 μs |  6.20 |    0.26 | 0.1068 | 0.0916 |    3042 B |        6.13 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           |  4.134 μs | 1.6112 μs | 0.0883 μs |  2.65 |    0.05 | 0.0420 | 0.0381 |    1064 B |        2.15 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           |  4.038 μs | 0.5333 μs | 0.0292 μs |  2.59 |    0.02 | 0.0420 | 0.0381 |    1072 B |        2.16 |
