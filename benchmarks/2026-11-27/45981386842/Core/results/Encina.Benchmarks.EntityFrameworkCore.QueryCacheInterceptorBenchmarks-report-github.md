```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.90GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  9,999.9 ns |   390.80 ns | 258.49 ns |  1.00 |    0.04 | 0.0763 | 0.0610 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 17,782.8 ns | 1,118.44 ns | 739.78 ns |  1.78 |    0.08 | 0.1526 | 0.1221 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 18,237.1 ns |   566.28 ns | 374.56 ns |  1.82 |    0.06 | 0.1831 | 0.1526 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 11,302.9 ns |   848.10 ns | 560.97 ns |  1.13 |    0.06 | 0.0916 | 0.0763 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    512.5 ns |     1.89 ns |   1.25 ns |  0.05 |    0.00 | 0.0010 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  5,163.5 ns |    22.78 ns |  11.91 ns |  0.52 |    0.01 | 0.0305 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 21,115.6 ns |    82.06 ns |  48.83 ns |  2.11 |    0.05 | 0.0916 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  9,437.5 ns | 4,042.31 ns | 221.57 ns |  1.00 |    0.03 | 0.0763 | 0.0610 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 16,677.0 ns | 2,193.70 ns | 120.24 ns |  1.77 |    0.04 | 0.1526 | 0.1221 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 16,910.7 ns | 5,230.06 ns | 286.68 ns |  1.79 |    0.05 | 0.1831 | 0.1526 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 10,362.3 ns | 1,678.49 ns |  92.00 ns |  1.10 |    0.02 | 0.0916 | 0.0763 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    513.4 ns |    30.27 ns |   1.66 ns |  0.05 |    0.00 | 0.0010 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  5,008.6 ns |   310.99 ns |  17.05 ns |  0.53 |    0.01 | 0.0305 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 20,871.3 ns |    77.03 ns |   4.22 ns |  2.21 |    0.05 | 0.0916 |      - |    8592 B |        1.13 |
