```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,590.9 ns |  11.96 ns |  7.12 ns |  0.65 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   148.2 ns |   1.11 ns |  0.66 ns |  0.04 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   346.6 ns |   5.52 ns |  3.28 ns |  0.09 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 5,451.1 ns |  42.87 ns | 25.51 ns |  1.37 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,983.2 ns |  19.41 ns | 12.84 ns |  1.00 | 0.0381 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,678.4 ns |  52.57 ns |  2.88 ns |  0.67 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   149.9 ns |  40.62 ns |  2.23 ns |  0.04 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   347.1 ns |  31.59 ns |  1.73 ns |  0.09 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 5,115.6 ns | 261.42 ns | 14.33 ns |  1.28 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,984.1 ns | 190.18 ns | 10.42 ns |  1.00 | 0.0381 |    1000 B |        1.00 |
