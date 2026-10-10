```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  7,074.9 ns |   189.58 ns | 125.40 ns |  1.00 |    0.02 | 0.4501 | 0.1526 |    8088 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 11,965.3 ns |   228.14 ns | 135.77 ns |  1.69 |    0.03 | 0.8698 | 0.2899 |   16104 B |        1.99 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 10,781.5 ns |   293.48 ns | 194.12 ns |  1.52 |    0.04 | 0.9155 | 0.2289 |   16496 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  7,140.6 ns |    67.49 ns |  44.64 ns |  1.01 |    0.02 | 0.4578 | 0.1526 |    8744 B |        1.08 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    554.0 ns |     5.09 ns |   3.03 ns |  0.08 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  4,226.9 ns |    45.87 ns |  27.30 ns |  0.60 |    0.01 | 0.1602 |      - |    2712 B |        0.34 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 18,529.7 ns |   136.44 ns |  71.36 ns |  2.62 |    0.04 | 0.4883 |      - |    8592 B |        1.06 |
|                                       |            |                |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  7,183.4 ns |   192.50 ns |  10.55 ns |  1.00 |    0.00 | 0.4501 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 14,115.1 ns | 7,564.64 ns | 414.64 ns |  1.96 |    0.05 | 0.8698 | 0.2899 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 11,392.5 ns |   590.46 ns |  32.37 ns |  1.59 |    0.00 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 10,145.8 ns | 3,252.43 ns | 178.28 ns |  1.41 |    0.02 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    600.5 ns |    26.32 ns |   1.44 ns |  0.08 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  4,584.1 ns | 1,107.40 ns |  60.70 ns |  0.64 |    0.01 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 19,760.2 ns |   478.44 ns |  26.23 ns |  2.75 |    0.00 | 0.4883 |      - |    8592 B |        1.13 |
