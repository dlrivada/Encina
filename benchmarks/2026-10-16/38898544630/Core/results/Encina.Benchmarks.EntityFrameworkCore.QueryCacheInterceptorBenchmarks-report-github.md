```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 10,352.1 ns |    347.88 ns |   230.10 ns |  1.00 |    0.03 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 19,133.1 ns |  1,579.29 ns | 1,044.60 ns |  1.85 |    0.10 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 13,533.0 ns |    820.27 ns |   542.56 ns |  1.31 |    0.06 | 0.9155 | 0.2289 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  9,135.1 ns |    548.07 ns |   362.51 ns |  0.88 |    0.04 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    572.9 ns |      4.77 ns |     2.84 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  4,541.8 ns |     48.19 ns |    31.88 ns |  0.44 |    0.01 | 0.1602 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 19,813.4 ns |    308.60 ns |   204.12 ns |  1.91 |    0.04 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  9,126.3 ns |  7,401.36 ns |   405.69 ns |  1.00 |    0.05 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 14,740.1 ns | 11,694.94 ns |   641.04 ns |  1.62 |    0.09 | 0.8698 | 0.2899 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 12,243.6 ns |  7,648.74 ns |   419.25 ns |  1.34 |    0.07 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           |  8,647.4 ns |  2,485.94 ns |   136.26 ns |  0.95 |    0.04 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    591.5 ns |    141.94 ns |     7.78 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  4,539.4 ns |    462.58 ns |    25.36 ns |  0.50 |    0.02 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 19,547.5 ns |  1,296.86 ns |    71.09 ns |  2.14 |    0.08 | 0.4883 |      - |    8592 B |        1.13 |
