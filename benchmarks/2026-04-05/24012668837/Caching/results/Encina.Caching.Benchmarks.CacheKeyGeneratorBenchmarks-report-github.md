```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,717.2 ns | 10.18 ns |  6.74 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,610.2 ns | 29.44 ns | 17.52 ns |  1.24 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,415.1 ns | 13.82 ns |  7.23 ns |  0.65 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        138.5 ns |  1.56 ns |  1.03 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        333.1 ns | 13.38 ns |  8.85 ns |  0.09 | 0.0534 |     896 B |        0.90 |
|                              |            |                |             |             |              |             |                 |          |          |       |        |           |             |
| GenerateKey_SimpleQuery      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 37,230,689.0 ns |       NA |  0.00 ns |  1.00 |      - |    2496 B |        1.00 |
| GenerateKey_ComplexQuery     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 58,873,348.0 ns |       NA |  0.00 ns |  1.58 |      - |    3144 B |        1.26 |
| GenerateKey_WithTemplate     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  4,137,028.0 ns |       NA |  0.00 ns |  0.11 |      - |    5576 B |        2.23 |
| GeneratePattern              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,157,612.0 ns |       NA |  0.00 ns |  0.03 |      - |     472 B |        0.19 |
| GeneratePattern_WithTemplate | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,993,332.0 ns |       NA |  0.00 ns |  0.08 |      - |    2656 B |        1.06 |
