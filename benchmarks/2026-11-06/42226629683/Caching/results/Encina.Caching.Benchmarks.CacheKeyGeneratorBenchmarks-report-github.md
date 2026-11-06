```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,609.6 ns |  32.25 ns | 21.33 ns |  0.62 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   162.6 ns |   2.32 ns |  1.53 ns |  0.04 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   399.5 ns |   2.52 ns |  1.67 ns |  0.09 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 5,261.7 ns |  29.00 ns | 15.17 ns |  1.24 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 4,229.2 ns |  22.54 ns | 13.41 ns |  1.00 | 0.0381 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,641.5 ns | 108.17 ns |  5.93 ns |  0.65 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   156.2 ns |  10.16 ns |  0.56 ns |  0.04 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   411.3 ns |  26.33 ns |  1.44 ns |  0.10 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 5,168.9 ns | 125.64 ns |  6.89 ns |  1.27 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 4,077.3 ns |  67.68 ns |  3.71 ns |  1.00 | 0.0381 |    1000 B |        1.00 |
