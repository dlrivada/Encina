```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    49.22 ns |     0.047 ns |   0.048 ns |  0.61 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    55.27 ns |     0.036 ns |   0.040 ns |  0.69 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    80.23 ns |     0.278 ns |   0.320 ns |  1.00 |    0.01 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    49.77 ns |     0.038 ns |   0.039 ns |  0.62 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   124.70 ns |     0.838 ns |   0.965 ns |  1.55 |    0.01 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 4,455.82 ns |   758.425 ns | 811.506 ns | 55.54 |    9.85 | 0.0114 | 0.0076 |    1109 B |        7.70 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,517.51 ns |    19.342 ns |  22.274 ns | 31.38 |    0.30 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 3,655.74 ns |   440.738 ns | 489.879 ns | 45.57 |    5.95 | 0.0076 | 0.0038 |     827 B |        5.74 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,326.08 ns |   531.627 ns | 568.835 ns | 41.46 |    6.90 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    55.19 ns |     3.295 ns |   0.181 ns |  0.69 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    55.78 ns |     1.347 ns |   0.074 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    80.23 ns |     3.001 ns |   0.164 ns |  1.00 |    0.00 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    49.75 ns |     0.218 ns |   0.012 ns |  0.62 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   123.57 ns |    20.892 ns |   1.145 ns |  1.54 |    0.01 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,794.08 ns | 8,218.121 ns | 450.463 ns | 47.29 |    4.86 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,562.71 ns |   373.922 ns |  20.496 ns | 31.94 |    0.23 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,564.60 ns | 4,304.719 ns | 235.956 ns | 31.97 |    2.55 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,017.48 ns | 3,035.313 ns | 166.376 ns | 37.61 |    1.80 | 0.0076 | 0.0038 |     712 B |        4.94 |
