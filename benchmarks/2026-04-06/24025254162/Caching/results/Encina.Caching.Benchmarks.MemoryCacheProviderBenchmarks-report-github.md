```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                        | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------ |----------- |--------------- |------------ |------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| GetAsync_CacheHit             | Job-YFEFPZ | 10             | Default     |    82.52 ns |     0.528 ns |   0.314 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | Job-YFEFPZ | 10             | Default     |    55.11 ns |     0.031 ns |   0.020 ns |  0.67 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | Job-YFEFPZ | 10             | Default     | 3,609.44 ns |   467.522 ns | 278.215 ns | 43.74 |    3.20 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | Job-YFEFPZ | 10             | Default     |    60.44 ns |     0.079 ns |   0.052 ns |  0.73 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | Job-YFEFPZ | 10             | Default     |    52.46 ns |     0.052 ns |   0.035 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | Job-YFEFPZ | 10             | Default     |   128.00 ns |     1.377 ns |   0.819 ns |  1.55 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | Job-YFEFPZ | 10             | Default     | 4,891.95 ns |   614.568 ns | 365.719 ns | 59.29 |    4.21 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | Job-YFEFPZ | 10             | Default     | 2,712.73 ns |    20.005 ns |  13.232 ns | 32.88 |    0.19 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | Job-YFEFPZ | 10             | Default     | 3,856.49 ns | 1,189.989 ns | 708.144 ns | 46.74 |    8.14 | 0.0420 | 0.0381 |     720 B |        5.00 |
|                               |            |                |             |             |              |            |       |         |        |        |           |             |
| GetAsync_CacheHit             | ShortRun   | 3              | 1           |    82.62 ns |     6.298 ns |   0.345 ns |  1.00 |    0.01 | 0.0086 |      - |     144 B |        1.00 |
| GetAsync_CacheMiss            | ShortRun   | 3              | 1           |    53.25 ns |     1.009 ns |   0.055 ns |  0.64 |    0.00 |      - |      - |         - |        0.00 |
| SetAsync                      | ShortRun   | 3              | 1           | 3,451.75 ns | 2,127.775 ns | 116.631 ns | 41.78 |    1.23 | 0.0420 | 0.0381 |     712 B |        4.94 |
| ExistsAsync_True              | ShortRun   | 3              | 1           |    60.35 ns |     1.810 ns |   0.099 ns |  0.73 |    0.00 |      - |      - |         - |        0.00 |
| ExistsAsync_False             | ShortRun   | 3              | 1           |    56.46 ns |     0.188 ns |   0.010 ns |  0.68 |    0.00 |      - |      - |         - |        0.00 |
| GetOrSetAsync_CacheHit        | ShortRun   | 3              | 1           |   126.40 ns |    23.793 ns |   1.304 ns |  1.53 |    0.01 | 0.0167 |      - |     280 B |        1.94 |
| GetOrSetAsync_CacheMiss       | ShortRun   | 3              | 1           | 4,675.08 ns | 2,808.198 ns | 153.927 ns | 56.59 |    1.63 | 0.0610 | 0.0572 |    1080 B |        7.50 |
| RemoveAsync                   | ShortRun   | 3              | 1           | 2,704.12 ns |   456.951 ns |  25.047 ns | 32.73 |    0.29 | 0.0458 |      - |     784 B |        5.44 |
| SetWithSlidingExpirationAsync | ShortRun   | 3              | 1           | 3,450.80 ns | 2,989.793 ns | 163.881 ns | 41.77 |    1.72 | 0.0420 | 0.0381 |     720 B |        5.00 |
