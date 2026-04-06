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
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,558.9 ns | 18.85 ns | 12.47 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,585.8 ns | 12.22 ns |  8.08 ns |  1.29 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,393.2 ns | 10.11 ns |  6.69 ns |  0.67 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        123.8 ns |  2.59 ns |  1.71 ns |  0.03 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        318.4 ns |  6.05 ns |  4.00 ns |  0.09 | 0.0534 |     896 B |        0.90 |
|                              |            |                |             |             |              |             |                 |          |          |       |        |           |             |
| GenerateKey_SimpleQuery      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 37,305,131.0 ns |       NA |  0.00 ns |  1.00 |      - |    2496 B |        1.00 |
| GenerateKey_ComplexQuery     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 60,106,212.0 ns |       NA |  0.00 ns |  1.61 |      - |    3144 B |        1.26 |
| GenerateKey_WithTemplate     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,992,374.0 ns |       NA |  0.00 ns |  0.11 |      - |    5576 B |        2.23 |
| GeneratePattern              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,145,930.0 ns |       NA |  0.00 ns |  0.03 |      - |     472 B |        0.19 |
| GeneratePattern_WithTemplate | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,121,618.0 ns |       NA |  0.00 ns |  0.08 |      - |    2656 B |        1.06 |
