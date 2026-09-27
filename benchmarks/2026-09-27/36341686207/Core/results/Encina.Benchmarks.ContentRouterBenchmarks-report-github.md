```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 1,722.3 ns |   7.16 ns |  4.74 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,087.8 ns |  21.72 ns | 14.37 ns |  0.63 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,070.9 ns |   8.27 ns |  5.47 ns |  0.62 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 1,809.2 ns |   5.73 ns |  3.79 ns |  1.05 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 1,812.1 ns |   4.89 ns |  2.91 ns |  1.05 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   490.6 ns |   3.31 ns |  1.97 ns |  0.28 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   752.3 ns |   5.33 ns |  3.53 ns |  0.44 | 0.0525 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 1,703.0 ns |  21.16 ns |  1.16 ns |  1.00 | 0.0381 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,085.5 ns |  28.68 ns |  1.57 ns |  0.64 | 0.0229 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,093.2 ns |  60.57 ns |  3.32 ns |  0.64 | 0.0229 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 1,768.4 ns | 162.91 ns |  8.93 ns |  1.04 | 0.0401 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 1,768.5 ns |  47.91 ns |  2.63 ns |  1.04 | 0.0401 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   502.0 ns |  36.69 ns |  2.01 ns |  0.29 | 0.0229 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   721.0 ns |  26.96 ns |  1.48 ns |  0.42 | 0.0525 |    1320 B |        1.34 |
