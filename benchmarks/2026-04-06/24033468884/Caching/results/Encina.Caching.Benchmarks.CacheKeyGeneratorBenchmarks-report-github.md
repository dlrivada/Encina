```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3           | 3,550.5 ns | 15.76 ns | 10.42 ns |  1.00 |    0.00 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3           | 4,631.7 ns | 16.06 ns | 10.62 ns |  1.30 |    0.00 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 3           | 2,424.5 ns | 44.76 ns | 26.64 ns |  0.68 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | 3           |   127.7 ns |  1.28 ns |  0.76 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | 3           |   310.5 ns |  2.51 ns |  1.66 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
|                              |            |                |             |             |            |          |          |       |         |        |           |             |
| GenerateKey_SimpleQuery      | MediumRun  | 15             | 2           | 10          | 3,566.3 ns | 31.46 ns | 47.09 ns |  1.00 |    0.02 | 0.0572 |    1000 B |        1.00 |
| GenerateKey_ComplexQuery     | MediumRun  | 15             | 2           | 10          | 4,512.4 ns |  8.70 ns | 12.48 ns |  1.27 |    0.02 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_WithTemplate     | MediumRun  | 15             | 2           | 10          | 2,405.0 ns |  6.34 ns |  9.30 ns |  0.67 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | MediumRun  | 15             | 2           | 10          |   121.5 ns |  1.41 ns |  2.03 ns |  0.03 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | MediumRun  | 15             | 2           | 10          |   328.4 ns |  1.82 ns |  2.62 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
