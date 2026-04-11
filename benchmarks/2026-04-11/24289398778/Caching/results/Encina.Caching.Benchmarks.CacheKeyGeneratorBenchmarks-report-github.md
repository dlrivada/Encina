```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 3           | 2,420.7 ns | 16.22 ns | 10.73 ns |  0.66 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | 3           |   131.5 ns |  1.56 ns |  1.03 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | 3           |   325.7 ns |  6.45 ns |  4.27 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3           | 4,672.4 ns | 22.37 ns | 14.80 ns |  1.26 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3           | 3,694.8 ns | 22.92 ns | 15.16 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |             |            |          |          |       |        |           |             |
| GenerateKey_WithTemplate     | MediumRun  | 15             | 2           | 10          | 2,434.4 ns | 11.06 ns | 16.22 ns |  0.68 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | MediumRun  | 15             | 2           | 10          |   131.0 ns |  1.37 ns |  2.01 ns |  0.04 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | MediumRun  | 15             | 2           | 10          |   340.8 ns |  4.43 ns |  6.63 ns |  0.09 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | MediumRun  | 15             | 2           | 10          | 4,625.4 ns | 14.07 ns | 19.72 ns |  1.29 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | MediumRun  | 15             | 2           | 10          | 3,594.7 ns | 21.79 ns | 31.25 ns |  1.00 | 0.0572 |    1000 B |        1.00 |
