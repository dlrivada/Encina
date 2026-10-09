```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                       | Job        | IterationCount | LaunchCount | Mean       | Error     | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------------- |----------- |--------------- |------------ |-----------:|----------:|---------:|------:|-------:|----------:|------------:|
| SimpleRoute_SingleCondition  | Job-YFEFPZ | 10             | Default     | 2,092.4 ns |  20.54 ns | 12.22 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | Job-YFEFPZ | 10             | Default     | 1,507.2 ns |   4.05 ns |  2.68 ns |  0.72 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | Job-YFEFPZ | 10             | Default     | 1,503.5 ns |   3.21 ns |  2.13 ns |  0.72 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | Job-YFEFPZ | 10             | Default     | 2,165.6 ns |   6.49 ns |  3.86 ns |  1.04 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | Job-YFEFPZ | 10             | Default     | 2,180.6 ns |   7.09 ns |  4.22 ns |  1.04 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | Job-YFEFPZ | 10             | Default     |   390.5 ns |  11.08 ns |  7.33 ns |  0.19 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | Job-YFEFPZ | 10             | Default     |   635.3 ns |   9.24 ns |  6.11 ns |  0.30 | 0.0782 |    1320 B |        1.34 |
|                              |            |                |             |            |           |          |       |        |           |             |
| SimpleRoute_SingleCondition  | ShortRun   | 3              | 1           | 2,073.2 ns | 185.28 ns | 10.16 ns |  1.00 | 0.0572 |     984 B |        1.00 |
| ComplexRoute_FirstMatch      | ShortRun   | 3              | 1           | 1,520.6 ns |  29.57 ns |  1.62 ns |  0.73 | 0.0343 |     584 B |        0.59 |
| ComplexRoute_DefaultFallback | ShortRun   | 3              | 1           | 1,511.2 ns | 227.56 ns | 12.47 ns |  0.73 | 0.0343 |     584 B |        0.59 |
| ManyRoutes_FirstMatch        | ShortRun   | 3              | 1           | 2,138.2 ns |  44.20 ns |  2.42 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| ManyRoutes_LateMatch         | ShortRun   | 3              | 1           | 2,138.7 ns | 349.50 ns | 19.16 ns |  1.03 | 0.0610 |    1024 B |        1.04 |
| BuildDefinition_Simple       | ShortRun   | 3              | 1           |   404.7 ns |   9.59 ns |  0.53 ns |  0.20 | 0.0348 |     584 B |        0.59 |
| BuildDefinition_Complex      | ShortRun   | 3              | 1           |   649.6 ns | 127.18 ns |  6.97 ns |  0.31 | 0.0782 |    1320 B |        1.34 |
