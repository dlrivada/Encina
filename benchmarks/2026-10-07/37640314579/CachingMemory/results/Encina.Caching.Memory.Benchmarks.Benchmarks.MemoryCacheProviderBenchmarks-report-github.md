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
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    43.62 ns |     0.057 ns |   0.058 ns |  0.65 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    48.91 ns |     0.067 ns |   0.077 ns |  0.72 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    67.61 ns |     1.256 ns |   1.396 ns |  1.00 |    0.03 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    43.02 ns |     0.154 ns |   0.151 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |   103.23 ns |     2.549 ns |   2.935 ns |  1.53 |    0.05 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,672.20 ns |   487.922 ns | 522.071 ns | 54.33 |    7.60 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 2,301.65 ns |    32.476 ns |  36.097 ns | 34.06 |    0.85 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 2,990.32 ns |   344.553 ns | 382.970 ns | 44.24 |    5.59 | 0.0076 | 0.0038 |     849 B |        5.90 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 3,066.26 ns |   472.938 ns | 506.038 ns | 45.37 |    7.34 | 0.0076 | 0.0038 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    42.34 ns |     0.711 ns |   0.039 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    47.63 ns |     0.104 ns |   0.006 ns |  0.71 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    66.63 ns |     3.415 ns |   0.187 ns |  1.00 |    0.00 | 0.0017 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    42.95 ns |     1.621 ns |   0.089 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |   101.11 ns |    13.645 ns |   0.748 ns |  1.52 |    0.01 | 0.0033 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,731.36 ns | 2,625.883 ns | 143.934 ns | 56.00 |    1.88 | 0.0114 | 0.0076 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 2,320.96 ns |   608.177 ns |  33.336 ns | 34.84 |    0.44 | 0.0076 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,544.47 ns | 7,935.258 ns | 434.958 ns | 38.19 |    5.65 | 0.0076 | 0.0038 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 3,091.83 ns | 2,324.192 ns | 127.397 ns | 46.41 |    1.66 | 0.0076 | 0.0038 |     712 B |        4.94 |
