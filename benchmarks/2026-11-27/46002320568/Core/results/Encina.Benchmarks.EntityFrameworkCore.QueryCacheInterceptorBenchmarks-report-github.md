```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.47GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|-------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  7,683.4 ns |    232.36 ns | 153.69 ns |  1.00 |    0.03 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 14,242.9 ns |    478.22 ns | 316.31 ns |  1.85 |    0.05 | 0.8698 | 0.2899 |   16104 B |        1.87 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 12,314.0 ns |    728.70 ns | 481.99 ns |  1.60 |    0.07 | 0.9155 | 0.2289 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  8,308.0 ns |    343.99 ns | 227.53 ns |  1.08 |    0.03 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    661.7 ns |     12.63 ns |   8.35 ns |  0.09 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  4,412.3 ns |     89.59 ns |  59.26 ns |  0.57 |    0.01 | 0.1602 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 18,966.3 ns |    420.78 ns | 250.40 ns |  2.47 |    0.06 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  8,876.1 ns | 11,781.67 ns | 645.79 ns |  1.00 |    0.09 | 0.4425 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 14,173.7 ns |    574.70 ns |  31.50 ns |  1.60 |    0.10 | 0.8698 | 0.2899 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 12,241.5 ns | 11,469.33 ns | 628.67 ns |  1.38 |    0.11 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           |  8,157.3 ns |  5,927.47 ns | 324.90 ns |  0.92 |    0.07 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    648.3 ns |     78.96 ns |   4.33 ns |  0.07 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  4,388.9 ns |  1,943.67 ns | 106.54 ns |  0.50 |    0.03 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 19,094.4 ns | 17,375.37 ns | 952.40 ns |  2.16 |    0.17 | 0.4883 |      - |    8592 B |        1.13 |
