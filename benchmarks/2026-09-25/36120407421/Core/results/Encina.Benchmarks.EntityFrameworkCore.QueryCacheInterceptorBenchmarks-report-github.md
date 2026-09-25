```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                                | Job        | IterationCount | LaunchCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 14,882.7 ns |   495.34 ns | 327.64 ns |  1.00 |    0.03 | 0.0763 | 0.0610 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 26,247.7 ns |   836.75 ns | 553.46 ns |  1.76 |    0.05 | 0.1526 | 0.1221 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 24,509.8 ns |   267.13 ns | 176.69 ns |  1.65 |    0.04 | 0.1831 | 0.1526 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 15,963.7 ns |   116.35 ns |  76.96 ns |  1.07 |    0.02 | 0.0916 | 0.0610 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     |    623.6 ns |     2.83 ns |   1.68 ns |  0.04 |    0.00 | 0.0010 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     |  6,703.4 ns |    46.64 ns |  30.85 ns |  0.45 |    0.01 | 0.0305 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 28,840.8 ns |    75.08 ns |  49.66 ns |  1.94 |    0.04 | 0.0916 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | ShortRun   | 3              | 1           | 14,851.8 ns | 7,365.80 ns | 403.74 ns |  1.00 |    0.03 | 0.0763 | 0.0610 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | ShortRun   | 3              | 1           | 25,603.4 ns |    68.94 ns |   3.78 ns |  1.72 |    0.04 | 0.1526 | 0.1221 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | ShortRun   | 3              | 1           | 24,620.9 ns | 4,828.57 ns | 264.67 ns |  1.66 |    0.04 | 0.1831 | 0.1526 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | ShortRun   | 3              | 1           | 15,986.6 ns |   938.05 ns |  51.42 ns |  1.08 |    0.03 | 0.0916 | 0.0610 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | ShortRun   | 3              | 1           |    616.7 ns |     8.75 ns |   0.48 ns |  0.04 |    0.00 | 0.0010 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | ShortRun   | 3              | 1           |  6,723.2 ns |   633.44 ns |  34.72 ns |  0.45 |    0.01 | 0.0305 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | ShortRun   | 3              | 1           | 28,883.0 ns |   687.71 ns |  37.70 ns |  1.95 |    0.05 | 0.0916 |      - |    8592 B |        1.13 |
