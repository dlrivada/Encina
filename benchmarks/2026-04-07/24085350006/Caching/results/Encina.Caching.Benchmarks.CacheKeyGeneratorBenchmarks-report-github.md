```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,425.6 ns |    18.65 ns | 12.33 ns |  0.73 |    0.00 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   122.1 ns |     3.39 ns |  2.24 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   288.0 ns |     7.76 ns |  4.62 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,109.0 ns |    14.00 ns |  9.26 ns |  1.23 |    0.00 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,336.6 ns |     5.49 ns |  3.63 ns |  1.00 |    0.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |            |             |          |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,439.6 ns |   192.63 ns | 10.56 ns |  0.72 |    0.00 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   124.4 ns |    28.80 ns |  1.58 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   291.0 ns |    27.09 ns |  1.48 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,333.9 ns | 1,627.19 ns | 89.19 ns |  1.28 |    0.02 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,373.7 ns |    27.64 ns |  1.51 ns |  1.00 |    0.00 | 0.0572 |    1000 B |        1.00 |
