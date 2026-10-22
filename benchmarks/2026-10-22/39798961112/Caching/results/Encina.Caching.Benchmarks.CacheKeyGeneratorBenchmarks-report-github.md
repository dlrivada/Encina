```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 3           | 2,437.2 ns | 22.98 ns | 15.20 ns |  0.68 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | 3           |   141.1 ns |  2.39 ns |  1.58 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | 3           |   344.9 ns | 11.44 ns |  7.57 ns |  0.10 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3           | 4,718.1 ns |  6.40 ns |  4.24 ns |  1.31 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3           | 3,597.8 ns | 10.26 ns |  6.78 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |             |            |          |          |       |        |           |             |
| GenerateKey_WithTemplate     | MediumRun  | 15             | 2           | 10          | 2,468.4 ns | 17.57 ns | 26.30 ns |  0.67 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | MediumRun  | 15             | 2           | 10          |   142.8 ns |  0.81 ns |  1.21 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | MediumRun  | 15             | 2           | 10          |   343.2 ns |  5.83 ns |  8.72 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | MediumRun  | 15             | 2           | 10          | 4,646.4 ns | 12.97 ns | 18.18 ns |  1.26 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | MediumRun  | 15             | 2           | 10          | 3,700.1 ns | 14.67 ns | 21.51 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
