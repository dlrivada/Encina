```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,555.5 ns |  14.38 ns |  7.52 ns |  0.58 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   153.0 ns |   0.54 ns |  0.32 ns |  0.03 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   390.6 ns |   3.63 ns |  2.40 ns |  0.09 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 5,467.7 ns |  34.10 ns | 22.55 ns |  1.24 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 4,410.4 ns |  18.33 ns | 12.13 ns |  1.00 | 0.0381 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,568.4 ns |  95.06 ns |  5.21 ns |  0.60 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   152.4 ns |   2.80 ns |  0.15 ns |  0.04 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   391.8 ns |  53.99 ns |  2.96 ns |  0.09 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 5,365.3 ns | 107.44 ns |  5.89 ns |  1.25 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 4,279.3 ns | 246.72 ns | 13.52 ns |  1.00 | 0.0381 |    1000 B |        1.00 |
