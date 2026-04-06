```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                        | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean             | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------ |------------- |------------ |-----------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         86.15 ns |     1.239 ns |   0.819 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         54.50 ns |     0.032 ns |   0.019 ns |  0.63 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,625.58 ns |   504.728 ns | 263.983 ns | 42.09 |    2.91 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         62.46 ns |     0.053 ns |   0.035 ns |  0.73 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         52.16 ns |     0.053 ns |   0.035 ns |  0.61 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        133.71 ns |     1.532 ns |   1.013 ns |  1.55 |    0.02 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,854.86 ns |   762.983 ns | 454.039 ns | 56.36 |    5.02 | 0.0610 | 0.0534 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,778.08 ns |    34.613 ns |  22.894 ns | 32.25 |    0.39 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,936.13 ns | 1,080.492 ns | 642.983 ns | 45.69 |    7.09 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |             |                  |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,054,146.00 ns |           NA |   0.000 ns |  1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,066,259.00 ns |           NA |   0.000 ns |  1.00 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,458,415.00 ns |           NA |   0.000 ns |  0.85 |    0.00 |      - |      - |     712 B |        4.94 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,468,153.00 ns |           NA |   0.000 ns |  0.86 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,468,383.00 ns |           NA |   0.000 ns |  0.86 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  6,362,034.00 ns |           NA |   0.000 ns |  1.57 |    0.00 |      - |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 11,989,948.00 ns |           NA |   0.000 ns |  2.96 |    0.00 |      - |      - |    1080 B |        7.50 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,368,939.00 ns |           NA |   0.000 ns |  2.06 |    0.00 |      - |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,352,283.00 ns |           NA |   0.000 ns |  1.07 |    0.00 |      - |      - |     720 B |        5.00 |
