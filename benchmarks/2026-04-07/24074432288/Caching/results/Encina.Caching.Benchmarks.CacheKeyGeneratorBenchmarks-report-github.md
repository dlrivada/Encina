```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3           | 3,546.9 ns | 11.69 ns |  6.96 ns | 3,547.5 ns |  1.00 |    0.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3           | 4,657.2 ns | 24.48 ns | 16.20 ns | 4,664.3 ns |  1.31 |    0.00 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 3           | 2,407.3 ns | 12.85 ns |  8.50 ns | 2,405.7 ns |  0.68 |    0.00 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | 3           |   125.5 ns |  1.41 ns |  0.93 ns |   125.5 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | 3           |   310.3 ns |  4.17 ns |  2.76 ns |   310.4 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
|                              |            |                |             |             |            |          |          |            |       |         |        |           |             |
| GenerateKey_SimpleQuery      | MediumRun  | 15             | 2           | 10          | 3,617.0 ns | 47.21 ns | 67.71 ns | 3,668.6 ns |  1.00 |    0.03 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | MediumRun  | 15             | 2           | 10          | 4,731.2 ns | 20.53 ns | 28.78 ns | 4,737.4 ns |  1.31 |    0.03 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | MediumRun  | 15             | 2           | 10          | 2,391.1 ns | 11.00 ns | 15.41 ns | 2,393.6 ns |  0.66 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | MediumRun  | 15             | 2           | 10          |   131.3 ns |  0.60 ns |  0.89 ns |   131.2 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | MediumRun  | 15             | 2           | 10          |   334.4 ns |  3.03 ns |  4.54 ns |   332.7 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
