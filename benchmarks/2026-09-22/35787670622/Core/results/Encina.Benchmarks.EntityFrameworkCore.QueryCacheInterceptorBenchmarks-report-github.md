```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 14,567.2 ns |   365.20 ns | 241.56 ns |  1.00 |    0.02 | 0.2747 | 0.1221 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 24,788.6 ns |   421.74 ns | 250.97 ns |  1.70 |    0.03 | 0.5798 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 27,472.4 ns |   260.63 ns | 172.39 ns |  1.89 |    0.03 | 0.6104 | 0.2136 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 15,545.2 ns |   194.04 ns | 115.47 ns |  1.07 |    0.02 | 0.3052 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    590.7 ns |     1.93 ns |   1.28 ns |  0.04 |    0.00 | 0.0048 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  7,320.9 ns |     7.97 ns |   4.75 ns |  0.50 |    0.01 | 0.1068 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 30,692.6 ns |    57.03 ns |  29.83 ns |  2.11 |    0.03 | 0.3052 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 14,803.1 ns | 1,665.00 ns |  91.26 ns |  1.00 |    0.01 | 0.2899 | 0.1373 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 24,751.1 ns | 7,059.63 ns | 386.96 ns |  1.67 |    0.02 | 0.5798 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 27,582.5 ns | 1,056.70 ns |  57.92 ns |  1.86 |    0.01 | 0.6104 | 0.2136 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 15,421.4 ns | 4,584.66 ns | 251.30 ns |  1.04 |    0.02 | 0.3052 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    593.5 ns |    18.98 ns |   1.04 ns |  0.04 |    0.00 | 0.0048 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  7,368.9 ns |   510.67 ns |  27.99 ns |  0.50 |    0.00 | 0.1068 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 31,599.6 ns | 1,064.49 ns |  58.35 ns |  2.13 |    0.01 | 0.3052 |      - |    8592 B |        1.13 |
