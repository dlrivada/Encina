```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                                | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean           | Error       | StdDev    | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|-------------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |---------------:|------------:|----------:|------:|--------:|-------:|-------:|----------:|------------:|
| &#39;Key generation (simple query)&#39;       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    13,822.9 ns | 1,234.79 ns | 816.74 ns |  1.00 |    0.08 | 0.4272 | 0.1526 |    8600 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    21,339.7 ns | 1,670.34 ns | 873.62 ns |  1.55 |    0.11 | 0.8545 | 0.2747 |   16360 B |        1.90 |
| &#39;Key generation with tenant&#39;          | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    25,491.3 ns |   494.68 ns | 258.72 ns |  1.85 |    0.11 | 0.9155 | 0.2441 |   16497 B |        1.92 |
| &#39;Cache hit (memory)&#39;                  | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    12,807.5 ns |   296.06 ns | 154.85 ns |  0.93 |    0.05 | 0.4578 | 0.1526 |    8744 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       834.3 ns |     2.00 ns |   1.32 ns |  0.06 |    0.00 | 0.0067 |      - |     120 B |        0.01 |
| &#39;CachedDataReader read (5 rows)&#39;      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     8,052.4 ns |    65.03 ns |  43.02 ns |  0.58 |    0.03 | 0.1526 |      - |    2712 B |        0.32 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |    33,376.9 ns |   202.29 ns | 133.80 ns |  2.42 |    0.14 | 0.4883 |      - |    8592 B |        1.00 |
|                                       |            |                |             |             |              |             |                |             |           |       |         |        |        |           |             |
| &#39;Key generation (simple query)&#39;       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   391,281.0 ns |          NA |   0.00 ns |  1.00 |    0.00 |      - |      - |    7576 B |        1.00 |
| &#39;Key generation (complex JOIN query)&#39; | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 2,764,155.0 ns |          NA |   0.00 ns |  7.06 |    0.00 |      - |      - |   14568 B |        1.92 |
| &#39;Key generation with tenant&#39;          | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,659,704.0 ns |          NA |   0.00 ns | 14.46 |    0.00 |      - |      - |   17096 B |        2.26 |
| &#39;Cache hit (memory)&#39;                  | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,148,992.0 ns |          NA |   0.00 ns | 10.60 |    0.00 |      - |      - |    7720 B |        1.02 |
| &#39;Cache miss (memory)&#39;                 | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,235,644.0 ns |          NA |   0.00 ns | 10.83 |    0.00 |      - |      - |     120 B |        0.02 |
| &#39;CachedDataReader read (5 rows)&#39;      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,354,053.0 ns |          NA |   0.00 ns | 13.68 |    0.00 |      - |      - |    2712 B |        0.36 |
| &#39;CachedDataReader read (1000 rows)&#39;   | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 5,120,327.0 ns |          NA |   0.00 ns | 13.09 |    0.00 |      - |      - |    8592 B |        1.13 |
