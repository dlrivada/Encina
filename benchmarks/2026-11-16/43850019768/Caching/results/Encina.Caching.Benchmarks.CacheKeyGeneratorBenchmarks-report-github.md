```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,574.3 ns |  13.30 ns |  7.92 ns |  0.63 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   155.9 ns |   2.14 ns |  1.28 ns |  0.04 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   369.5 ns |   4.51 ns |  2.98 ns |  0.09 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 5,472.2 ns |  21.16 ns | 12.59 ns |  1.34 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 4,078.3 ns |  15.52 ns |  8.12 ns |  1.00 | 0.0381 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,768.4 ns | 250.21 ns | 13.71 ns |  0.68 | 0.0534 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   154.2 ns |  13.72 ns |  0.75 ns |  0.04 | 0.0100 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   374.8 ns |  51.59 ns |  2.83 ns |  0.09 | 0.0353 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 5,385.7 ns |  75.42 ns |  4.13 ns |  1.32 | 0.0610 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 4,087.4 ns | 322.99 ns | 17.70 ns |  1.00 | 0.0381 |    1000 B |        1.00 |
