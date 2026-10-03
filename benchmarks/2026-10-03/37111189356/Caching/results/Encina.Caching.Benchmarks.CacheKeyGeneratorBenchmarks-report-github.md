```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|------------:|---------:|------:|--------:|-------:|----------:|------------:|
| GenerateKey_WithTemplate     | Job-YFEFPZ | 10             | Default     | 2,469.6 ns |    11.42 ns |  5.97 ns |  0.66 |    0.00 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | Job-YFEFPZ | 10             | Default     |   136.9 ns |     0.90 ns |  0.54 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | Job-YFEFPZ | 10             | Default     |   348.2 ns |     3.92 ns |  2.59 ns |  0.09 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | Job-YFEFPZ | 10             | Default     | 4,745.4 ns |    18.09 ns | 11.96 ns |  1.27 |    0.00 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | Job-YFEFPZ | 10             | Default     | 3,741.5 ns |     3.71 ns |  1.94 ns |  1.00 |    0.00 | 0.0572 |    1000 B |        1.00 |
|                              |            |                |             |            |             |          |       |         |        |           |             |
| GenerateKey_WithTemplate     | ShortRun   | 3              | 1           | 2,479.4 ns |   159.97 ns |  8.77 ns |  0.67 |    0.00 | 0.0801 |    1360 B |        1.36 |
| GeneratePattern              | ShortRun   | 3              | 1           |   137.6 ns |    14.41 ns |  0.79 ns |  0.04 |    0.00 | 0.0153 |     256 B |        0.26 |
| GeneratePattern_WithTemplate | ShortRun   | 3              | 1           |   360.0 ns |    44.95 ns |  2.46 ns |  0.10 |    0.00 | 0.0534 |     896 B |        0.90 |
| GenerateKey_ComplexQuery     | ShortRun   | 3              | 1           | 4,731.6 ns | 1,341.00 ns | 73.50 ns |  1.29 |    0.02 | 0.0916 |    1648 B |        1.65 |
| GenerateKey_SimpleQuery      | ShortRun   | 3              | 1           | 3,681.9 ns |    92.41 ns |  5.07 ns |  1.00 |    0.00 | 0.0572 |    1000 B |        1.00 |
