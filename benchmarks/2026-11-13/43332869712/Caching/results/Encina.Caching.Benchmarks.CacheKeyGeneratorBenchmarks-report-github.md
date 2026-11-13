```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,202.9 ns |  23.29 ns | 15.40 ns |  0.64 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   161.6 ns |   0.81 ns |  0.48 ns |  0.05 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   377.5 ns |   2.09 ns |  1.24 ns |  0.11 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,407.6 ns |  33.02 ns | 19.65 ns |  1.28 | 0.0153 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,456.4 ns |  28.71 ns | 18.99 ns |  1.00 | 0.0114 |    1000 B |        1.00 |
|                              |            |                |             |            |           |          |       |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,073.3 ns | 261.42 ns | 14.33 ns |  0.60 | 0.0153 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   159.7 ns |   5.68 ns |  0.31 ns |  0.05 | 0.0029 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   380.1 ns |  22.10 ns |  1.21 ns |  0.11 | 0.0105 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,480.3 ns | 954.13 ns | 52.30 ns |  1.30 | 0.0153 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,450.7 ns | 127.95 ns |  7.01 ns |  1.00 | 0.0114 |    1000 B |        1.00 |
