```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  8,800.1 ns |    669.33 ns |   442.72 ns |  1.00 |    0.07 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 15,009.6 ns |  1,865.63 ns | 1,234.00 ns |  1.71 |    0.16 | 0.8698 | 0.2899 |   16104 B |        1.87 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 13,873.3 ns |  1,035.88 ns |   685.17 ns |  1.58 |    0.11 | 0.9155 | 0.2289 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  9,148.0 ns |    473.70 ns |   313.32 ns |  1.04 |    0.06 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    667.6 ns |     14.59 ns |     8.68 ns |  0.08 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  4,346.0 ns |    106.66 ns |    55.78 ns |  0.49 |    0.02 | 0.1602 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 19,294.9 ns |    624.84 ns |   371.83 ns |  2.20 |    0.11 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  9,216.9 ns |  7,351.79 ns |   402.98 ns |  1.00 |    0.05 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 16,528.1 ns | 28,236.84 ns | 1,547.76 ns |  1.80 |    0.16 | 0.8545 | 0.2747 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 13,054.5 ns | 12,383.99 ns |   678.81 ns |  1.42 |    0.08 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           |  9,212.9 ns |  6,223.71 ns |   341.14 ns |  1.00 |    0.05 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    641.2 ns |    109.00 ns |     5.97 ns |  0.07 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  4,572.4 ns |  2,223.22 ns |   121.86 ns |  0.50 |    0.02 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 18,791.1 ns |  1,158.31 ns |    63.49 ns |  2.04 |    0.08 | 0.4883 |      - |    8592 B |        1.13 |
