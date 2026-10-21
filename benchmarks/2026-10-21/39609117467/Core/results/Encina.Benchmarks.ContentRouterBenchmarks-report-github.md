```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.78GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,682.3 ns |   1.97 ns |  1.17 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,204.8 ns |   1.16 ns |  0.69 ns |  0.72 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,173.3 ns |   5.45 ns |  3.61 ns |  0.70 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,811.5 ns |   3.00 ns |  1.99 ns |  1.08 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,811.0 ns |   5.49 ns |  3.27 ns |  1.08 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   473.9 ns |   4.11 ns |  2.72 ns |  0.28 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   692.9 ns |  20.81 ns | 13.77 ns |  0.41 | 0.0525 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,680.7 ns | 209.13 ns | 11.46 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,188.2 ns |  54.78 ns |  3.00 ns |  0.71 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,195.5 ns | 126.06 ns |  6.91 ns |  0.71 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,756.5 ns |  83.81 ns |  4.59 ns |  1.05 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,740.9 ns |  42.09 ns |  2.31 ns |  1.04 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   478.7 ns |  19.69 ns |  1.08 ns |  0.28 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   685.8 ns | 314.98 ns | 17.27 ns |  0.41 | 0.0525 |    1320 B |        1.34 |
