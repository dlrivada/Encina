```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                                | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | 3           | 10,732.7 ns | 292.03 ns | 173.78 ns | 10,703.0 ns |  1.00 |    0.02 | 0.0763 | 0.0610 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | 3           | 18,255.4 ns | 579.72 ns | 344.98 ns | 18,455.4 ns |  1.70 |    0.04 | 0.1526 | 0.1221 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | 3           | 18,149.4 ns | 364.79 ns | 217.08 ns | 18,214.7 ns |  1.69 |    0.03 | 0.1831 | 0.1526 |   16496 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | 3           | 11,090.8 ns | 283.57 ns | 168.75 ns | 11,015.5 ns |  1.03 |    0.02 | 0.0916 | 0.0763 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | 3           |    520.9 ns |   1.52 ns |   0.91 ns |    520.9 ns |  0.05 |    0.00 | 0.0010 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | 3           |  5,546.2 ns |  57.51 ns |  38.04 ns |  5,547.2 ns |  0.52 |    0.01 | 0.0305 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | 3           | 23,971.2 ns | 141.92 ns |  93.87 ns | 24,002.5 ns |  2.23 |    0.04 | 0.0916 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |             |           |           |             |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | MediumRun  | 15             | 2           | 10          | 10,359.0 ns | 205.75 ns | 307.96 ns | 10,335.7 ns |  1.00 |    0.04 | 0.0763 | 0.0610 |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | MediumRun  | 15             | 2           | 10          | 18,087.5 ns | 187.29 ns | 274.52 ns | 18,111.8 ns |  1.75 |    0.06 | 0.1526 | 0.1221 |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | MediumRun  | 15             | 2           | 10          | 18,077.4 ns | 123.80 ns | 181.47 ns | 18,032.3 ns |  1.75 |    0.06 | 0.1831 | 0.1526 |   15472 B |        2.04 |
| &#39;Cache hit (memory)&#39;                  | MediumRun  | 15             | 2           | 10          | 10,913.3 ns |  58.62 ns |  80.24 ns | 10,898.9 ns |  1.05 |    0.03 | 0.0916 | 0.0763 |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | MediumRun  | 15             | 2           | 10          |    512.4 ns |   8.76 ns |  12.83 ns |    519.5 ns |  0.05 |    0.00 | 0.0010 |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | MediumRun  | 15             | 2           | 10          |  5,561.6 ns |   9.46 ns |  13.57 ns |  5,564.7 ns |  0.54 |    0.02 | 0.0305 |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | MediumRun  | 15             | 2           | 10          | 24,143.4 ns |  26.79 ns |  37.55 ns | 24,149.4 ns |  2.33 |    0.07 | 0.0916 |      - |    8592 B |        1.13 |
