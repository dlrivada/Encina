```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean        | Error     | StdDev    | Median      | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |------------:|----------:|----------:|------------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 3           | 1,293.72 ns | 17.763 ns | 11.749 ns | 1,293.25 ns |  0.76 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | 3           |    62.22 ns |  2.191 ns |  1.450 ns |    62.03 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | 3           |   170.64 ns | 11.427 ns |  7.558 ns |   170.81 ns |  0.10 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3           | 2,238.01 ns | 57.990 ns | 38.357 ns | 2,237.88 ns |  1.32 |    0.02 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3           | 1,698.55 ns | 27.149 ns | 16.156 ns | 1,696.92 ns |  1.00 |    0.01 | 0.0591 |    1000 B |        1.00 |
|                              |            |                |             |             |             |           |           |             |       |         |        |           |             |
| GenerateKey_WithTemplate     | MediumRun  | 15             | 2           | 10          | 1,276.13 ns | 10.575 ns | 14.825 ns | 1,270.67 ns |  0.76 |    0.01 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | MediumRun  | 15             | 2           | 10          |    63.93 ns |  1.021 ns |  1.464 ns |    63.98 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | MediumRun  | 15             | 2           | 10          |   160.20 ns |  2.785 ns |  4.082 ns |   161.07 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | MediumRun  | 15             | 2           | 10          | 2,155.29 ns | 23.495 ns | 32.161 ns | 2,134.87 ns |  1.28 |    0.02 | 0.0954 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | MediumRun  | 15             | 2           | 10          | 1,688.68 ns | 12.678 ns | 17.353 ns | 1,691.85 ns |  1.00 |    0.01 | 0.0591 |    1000 B |        1.00 |
