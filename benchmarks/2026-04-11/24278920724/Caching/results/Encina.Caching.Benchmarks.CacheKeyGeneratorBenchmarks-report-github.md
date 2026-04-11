```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.69GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                       | Job        | IterationCount | LaunchCount | WarmupCount | Mean       | Error    | StdDev   | Median     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |------------ |-----------:|---------:|---------:|-----------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 3           | 2,501.4 ns |  4.98 ns |  2.96 ns | 2,500.7 ns |  0.59 |    0.00 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     | 3           |   125.2 ns |  1.07 ns |  0.71 ns |   125.3 ns |  0.03 |    0.00 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     | 3           |   333.6 ns |  7.10 ns |  4.69 ns |   332.8 ns |  0.08 |    0.00 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 3           | 5,489.0 ns | 27.50 ns | 16.37 ns | 5,492.8 ns |  1.30 |    0.00 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3           | 4,234.5 ns | 14.83 ns |  8.82 ns | 4,238.0 ns |  1.00 |    0.00 | 0.0381 |    1000 B |        1.00 |
|                              |            |                |             |             |            |          |          |            |       |         |        |           |             |
| GenerateKey_WithTemplate     | MediumRun  | 15             | 2           | 10          | 2,537.6 ns | 25.58 ns | 38.29 ns | 2,547.4 ns |  0.59 |    0.01 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | MediumRun  | 15             | 2           | 10          |   129.0 ns |  0.56 ns |  0.80 ns |   128.9 ns |  0.03 |    0.00 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | MediumRun  | 15             | 2           | 10          |   334.5 ns |  5.53 ns |  8.10 ns |   334.3 ns |  0.08 |    0.00 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | MediumRun  | 15             | 2           | 10          | 5,305.0 ns | 36.19 ns | 53.05 ns | 5,271.2 ns |  1.23 |    0.02 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | MediumRun  | 15             | 2           | 10          | 4,297.8 ns | 22.38 ns | 32.80 ns | 4,314.9 ns |  1.00 |    0.01 | 0.0381 |    1000 B |        1.00 |
