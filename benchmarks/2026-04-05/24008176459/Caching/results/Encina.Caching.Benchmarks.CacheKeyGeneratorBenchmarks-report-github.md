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
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      3,644.1 ns | 16.85 ns | 11.14 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      4,782.0 ns | 26.95 ns | 17.82 ns |  1.31 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      2,440.2 ns | 37.81 ns | 25.01 ns |  0.67 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        131.7 ns |  3.62 ns |  2.39 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        316.1 ns | 10.79 ns |  7.14 ns |  0.09 | 0.0534 |     896 B |        0.90 |
|                              |            |                |             |             |              |             |                 |          |          |       |        |           |             |
| GenerateKey_SimpleQuery      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 36,085,673.0 ns |       NA |  0.00 ns |  1.00 |      - |    2496 B |        1.00 |
| GenerateKey_ComplexQuery     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 58,578,211.0 ns |       NA |  0.00 ns |  1.62 |      - |    3144 B |        1.26 |
| GenerateKey_WithTemplate     | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  3,912,359.0 ns |       NA |  0.00 ns |  0.11 |      - |    5576 B |        2.23 |
| GeneratePattern              | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  1,125,598.0 ns |       NA |  0.00 ns |  0.03 |      - |     472 B |        0.19 |
| GeneratePattern_WithTemplate | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  2,985,924.0 ns |       NA |  0.00 ns |  0.08 |      - |    2656 B |        1.06 |
