```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 13,683.8 ns |    175.85 ns |   116.31 ns |  1.00 |    0.01 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 22,946.7 ns |    494.79 ns |   327.27 ns |  1.68 |    0.03 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 26,954.7 ns |    460.15 ns |   304.36 ns |  1.97 |    0.03 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 14,117.7 ns |    303.00 ns |   200.42 ns |  1.03 |    0.02 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    834.7 ns |      2.21 ns |     1.32 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  8,063.6 ns |     21.97 ns |    13.08 ns |  0.59 |    0.00 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 34,031.5 ns |    123.30 ns |    81.55 ns |  2.49 |    0.02 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 14,158.4 ns |  6,701.83 ns |   367.35 ns |  1.00 |    0.03 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 24,260.9 ns |  4,974.93 ns |   272.69 ns |  1.71 |    0.04 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 27,877.1 ns |  2,708.81 ns |   148.48 ns |  1.97 |    0.05 | 0.9155 | 0.2441 |   15473 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 14,056.8 ns |  5,191.02 ns |   284.54 ns |  0.99 |    0.03 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    834.8 ns |     23.36 ns |     1.28 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  8,084.9 ns |    610.56 ns |    33.47 ns |  0.57 |    0.01 | 0.1526 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 35,151.5 ns | 21,316.94 ns | 1,168.45 ns |  2.48 |    0.09 | 0.4883 |      - |    8592 B |        1.13 |
