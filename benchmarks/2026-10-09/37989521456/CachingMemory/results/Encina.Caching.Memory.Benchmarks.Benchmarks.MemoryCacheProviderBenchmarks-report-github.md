```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-NUBXJZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                        | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| ExistsAsync_False             | Job-NUBXJZ | 20             | Default     | 5           |    34.31 ns |     0.174 ns |   0.186 ns |  0.74 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | Job-NUBXJZ | 20             | Default     | 5           |    38.32 ns |     0.335 ns |   0.373 ns |  0.83 |    0.01 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | Job-NUBXJZ | 20             | Default     | 5           |    46.37 ns |     0.384 ns |   0.442 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-NUBXJZ | 20             | Default     | 5           |    35.21 ns |     0.272 ns |   0.279 ns |  0.76 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-NUBXJZ | 20             | Default     | 5           |    65.52 ns |     0.307 ns |   0.301 ns |  1.41 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-NUBXJZ | 20             | Default     | 5           | 3,935.41 ns |   245.647 ns | 262.840 ns | 84.87 |    5.57 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-NUBXJZ | 20             | Default     | 5           | 1,549.70 ns |     7.312 ns |   8.421 ns | 33.42 |    0.36 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-NUBXJZ | 20             | Default     | 5           | 2,997.37 ns |   319.122 ns | 354.704 ns | 64.64 |    7.48 | 0.0496 | 0.0458 |     850 B |        5.90 |
| SetAsync                      | Job-NUBXJZ | 20             | Default     | 5           | 2,845.75 ns |   197.905 ns | 203.234 ns | 61.37 |    4.30 | 0.0420 | 0.0381 |     712 B |        4.94 |
|                               |            |                |             |             |             |              |            |       |         |        |        |           |             |
| ExistsAsync_False             | ShortRun   | 3              | 1           | 3           |    34.37 ns |     1.088 ns |   0.060 ns |  0.73 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_True              | ShortRun   | 3              | 1           | 3           |    38.11 ns |     0.287 ns |   0.016 ns |  0.81 |    0.00 |      - |      - |         - |        0.00 |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           | 3           |    46.77 ns |     4.494 ns |   0.246 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           | 3           |    35.28 ns |     6.181 ns |   0.339 ns |  0.75 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           | 3           |    65.97 ns |     8.099 ns |   0.444 ns |  1.41 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 3           | 3,334.85 ns | 1,845.002 ns | 101.131 ns | 71.30 |    1.90 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 3           | 1,538.46 ns |   160.150 ns |   8.778 ns | 32.89 |    0.22 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3           | 2,429.88 ns | 3,844.066 ns | 210.706 ns | 51.95 |    3.91 | 0.0420 | 0.0381 |     720 B |        5.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3           | 2,504.58 ns | 2,134.585 ns | 117.004 ns | 53.55 |    2.18 | 0.0420 | 0.0381 |     712 B |        4.94 |
