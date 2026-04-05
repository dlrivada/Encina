```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean           | Error     | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |---------------:|----------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    13,318.5 ns | 313.90 ns | 207.62 ns |  1.00 |    0.02 | 0.4425 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    21,682.3 ns | 521.68 ns | 345.06 ns |  1.63 |    0.03 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    26,434.8 ns | 575.96 ns | 380.96 ns |  1.99 |    0.04 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    13,513.6 ns | 382.75 ns | 253.17 ns |  1.01 |    0.02 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       859.8 ns |   3.44 ns |   2.28 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     7,975.1 ns |  42.28 ns |  27.97 ns |  0.60 |    0.01 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    33,405.1 ns | 145.32 ns |  86.48 ns |  2.51 |    0.04 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |                |           |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   485,402.0 ns |        NA |   0.00 ns |  1.00 |    0.00 |      - |      - |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,815,861.0 ns |        NA |   0.00 ns |  5.80 |    0.00 |      - |      - |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,885,680.0 ns |        NA |   0.00 ns | 12.13 |    0.00 |      - |      - |   17096 B |        2.26 |
| &#39;Cache hit (memory)&#39;                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,308,166.0 ns |        NA |   0.00 ns |  6.82 |    0.00 |      - |      - |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,356,105.0 ns |        NA |   0.00 ns |  8.97 |    0.00 |      - |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,344,363.0 ns |        NA |   0.00 ns | 11.01 |    0.00 |      - |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,260,625.0 ns |        NA |   0.00 ns | 10.84 |    0.00 |      - |      - |    8592 B |        1.13 |
