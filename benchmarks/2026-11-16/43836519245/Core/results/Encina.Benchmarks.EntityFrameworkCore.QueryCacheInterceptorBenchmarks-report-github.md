```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.41GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  7,752.7 ns |    453.13 ns | 299.72 ns |  1.00 |    0.05 | 0.4501 | 0.1526 |    8088 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 13,956.5 ns |  1,333.93 ns | 882.31 ns |  1.80 |    0.13 | 0.8698 | 0.2899 |   16104 B |        1.99 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 13,016.0 ns |    742.35 ns | 441.76 ns |  1.68 |    0.08 | 0.9155 | 0.2289 |   16496 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  8,825.0 ns |    594.11 ns | 392.97 ns |  1.14 |    0.06 | 0.4578 | 0.1526 |    8744 B |        1.08 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    649.7 ns |      6.35 ns |   3.78 ns |  0.08 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  4,376.0 ns |    101.07 ns |  60.15 ns |  0.57 |    0.02 | 0.1602 |      - |    2712 B |        0.34 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 19,078.2 ns |    631.01 ns | 417.37 ns |  2.46 |    0.10 | 0.4883 |      - |    8592 B |        1.06 |
|                                       |            |                |             |             |              |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  7,794.2 ns |    435.48 ns |  23.87 ns |  1.00 |    0.00 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 15,529.3 ns | 12,767.58 ns | 699.83 ns |  1.99 |    0.08 | 0.8698 | 0.2899 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 11,425.7 ns |  2,788.04 ns | 152.82 ns |  1.47 |    0.02 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           |  8,425.3 ns |  5,963.04 ns | 326.85 ns |  1.08 |    0.04 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    646.4 ns |    167.27 ns |   9.17 ns |  0.08 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  4,405.1 ns |  1,158.11 ns |  63.48 ns |  0.57 |    0.01 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 19,142.2 ns |  1,779.98 ns |  97.57 ns |  2.46 |    0.01 | 0.4883 |      - |    8592 B |        1.13 |
