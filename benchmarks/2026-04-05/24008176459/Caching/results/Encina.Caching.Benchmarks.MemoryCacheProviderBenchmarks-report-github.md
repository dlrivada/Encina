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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         83.18 ns |     1.535 ns |   1.015 ns |  1.00 |    0.02 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         53.57 ns |     0.151 ns |   0.079 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,725.98 ns |   732.763 ns | 436.056 ns | 44.80 |    5.00 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         60.46 ns |     0.113 ns |   0.067 ns |  0.73 |    0.01 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         53.22 ns |     0.116 ns |   0.069 ns |  0.64 |    0.01 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        127.24 ns |     5.016 ns |   3.318 ns |  1.53 |    0.04 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5,044.20 ns |   657.531 ns | 391.286 ns | 60.65 |    4.52 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,788.67 ns |    38.366 ns |  25.377 ns | 33.53 |    0.49 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,925.96 ns | 1,410.877 ns | 839.590 ns | 47.20 |    9.59 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |             |                  |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,014,809.00 ns |           NA |   0.000 ns |  1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,054,975.00 ns |           NA |   0.000 ns |  1.01 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,401,340.00 ns |           NA |   0.000 ns |  0.85 |    0.00 |      - |      - |     712 B |        4.94 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,628,306.00 ns |           NA |   0.000 ns |  0.90 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,557,193.00 ns |           NA |   0.000 ns |  0.89 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  6,374,017.00 ns |           NA |   0.000 ns |  1.59 |    0.00 |      - |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 12,050,815.00 ns |           NA |   0.000 ns |  3.00 |    0.00 |      - |      - |    1080 B |        7.50 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,350,409.00 ns |           NA |   0.000 ns |  2.08 |    0.00 |      - |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,250,917.00 ns |           NA |   0.000 ns |  1.06 |    0.00 |      - |      - |     720 B |        5.00 |
