```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     |  8,858.5 ns | 1,703.36 ns | 1,126.67 ns |  1.02 |    0.18 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 12,820.8 ns | 2,691.71 ns | 1,780.40 ns |  1.47 |    0.27 | 0.8698 | 0.2899 |   16104 B |        1.87 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 10,583.6 ns |   343.33 ns |   227.09 ns |  1.21 |    0.15 | 0.9155 | 0.2289 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     |  7,273.9 ns |   396.36 ns |   262.17 ns |  0.83 |    0.11 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    547.5 ns |    10.42 ns |     6.90 ns |  0.06 |    0.01 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  4,283.3 ns |    40.22 ns |    26.60 ns |  0.49 |    0.06 | 0.1602 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 19,045.3 ns |   715.68 ns |   374.32 ns |  2.18 |    0.28 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           |  7,008.6 ns |   844.41 ns |    46.29 ns |  1.00 |    0.01 | 0.4501 | 0.1526 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 12,056.0 ns | 1,542.37 ns |    84.54 ns |  1.72 |    0.01 | 0.8698 | 0.2899 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 11,053.6 ns |   851.65 ns |    46.68 ns |  1.58 |    0.01 | 0.9155 | 0.2289 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           |  8,943.3 ns | 3,825.12 ns |   209.67 ns |  1.28 |    0.03 | 0.4578 | 0.1526 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    617.9 ns |   518.32 ns |    28.41 ns |  0.09 |    0.00 | 0.0067 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  4,705.5 ns |   730.06 ns |    40.02 ns |  0.67 |    0.01 | 0.1602 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 20,031.1 ns |   574.80 ns |    31.51 ns |  2.86 |    0.02 | 0.4883 |      - |    8592 B |        1.13 |
