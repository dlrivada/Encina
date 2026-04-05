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
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         86.10 ns |     0.328 ns |   0.217 ns |  1.00 |    0.00 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         53.37 ns |     0.066 ns |   0.043 ns |  0.62 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,729.47 ns |   395.593 ns | 235.411 ns | 43.32 |    2.59 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         60.38 ns |     0.121 ns |   0.080 ns |  0.70 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         54.14 ns |     0.115 ns |   0.069 ns |  0.63 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        132.67 ns |     0.728 ns |   0.433 ns |  1.54 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      5,035.44 ns |   414.975 ns | 246.945 ns | 58.49 |    2.72 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,750.63 ns |    16.856 ns |  11.150 ns | 31.95 |    0.15 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,713.51 ns | 1,059.768 ns | 630.651 ns | 43.13 |    6.95 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |             |                  |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,984,093.00 ns |           NA |   0.000 ns |  1.00 |    0.00 |      - |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,036,430.00 ns |           NA |   0.000 ns |  1.01 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,298,615.00 ns |           NA |   0.000 ns |  0.83 |    0.00 |      - |      - |     712 B |        4.94 |
| ExistsAsync_True              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,410,153.00 ns |           NA |   0.000 ns |  0.86 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,412,157.00 ns |           NA |   0.000 ns |  0.86 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  6,255,234.00 ns |           NA |   0.000 ns |  1.57 |    0.00 |      - |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 11,836,542.00 ns |           NA |   0.000 ns |  2.97 |    0.00 |      - |      - |    1080 B |        7.50 |
| RemoveAsync                   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  8,004,482.00 ns |           NA |   0.000 ns |  2.01 |    0.00 |      - |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,146,756.00 ns |           NA |   0.000 ns |  1.04 |    0.00 |      - |      - |     720 B |        5.00 |
